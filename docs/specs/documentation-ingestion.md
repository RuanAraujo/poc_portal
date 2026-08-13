# Especificação — serviço de ingestão de documentações

## Objetivo

O `Documentation.Ingestion.Worker` transforma uma versão OpenAPI publicada pela
API em chunks vetorizados persistidos no PostgreSQL. Ele é um consumidor
assíncrono: a mensagem de integração contém somente referências, e o conteúdo
imutável é recuperado da API interna.

## Limites e responsabilidades

- Consome exclusivamente `DocumentationPublished` de `Documentation.Contracts`.
- Não cria, altera ou remove versões de documentação na API.
- Não expõe endpoint HTTP; saúde e progresso são registrados nos logs do
  `BackgroundService`.
- Persiste seus dados no schema PostgreSQL `ingestion`.

## Contrato de mensageria

| Item | Valor |
| --- | --- |
| Exchange | `documentation.events` (topic, durável) |
| Routing key | `documentation.published.v1` |
| Fila principal | `documentation.ingestion.v1` |
| Fila de retry | `documentation.ingestion.retry.v1` |
| Fila DLQ | `documentation.ingestion.dlq.v1` |
| ACK | Manual, somente depois de processar ou republicar com confirmação |
| Retry | 10 segundos, até 3 novas tentativas |

A fila de retry possui TTL de 10 segundos e devolve a mensagem à fila principal
via dead-letter. A contagem fica no header `x-retry-count`. Em uma falha
definitiva ou na quarta tentativa total, o worker publica a cópia persistente na
DLQ e confirma a mensagem original.

## Fluxo de processamento

1. Desserializar o evento JSON camelCase e validar `EventType`.
2. Consultar `processed_integration_events` pelo `EventId`.
3. Se já estiver processado, chamar o callback `Available` na API e confirmar
   a mensagem sem criar chunks novos.
4. Buscar `GET /internal/documentations/{documentId}/versions/{versionId}/content`.
5. Fazer `PUT .../indexing-status` com `Indexing`.
6. Criar um chunk geral e um chunk para cada operação HTTP encontrada nos paths
   do OpenAPI JSON ou YAML.
7. Solicitar ao serviço local EmbeddingGemma, via gRPC `EmbedDocument`, embeddings normalizados de 768
   dimensões e substituir os chunks da versão numa transação, junto do registro
   de idempotência.
8. Fazer callback `Available` e confirmar a mensagem.

Caso o callback final falhe depois da transação, a nova entrega encontra o
`EventId` já processado e refaz apenas o callback `Available`.

## Contrato HTTP esperado da API

O endpoint de conteúdo retorna JSON camelCase com ao menos:

```json
{
  "format": "json",
  "content": "{ ... OpenAPI ... }"
}
```

O endpoint de status aceita:

```json
{ "status": "Indexing" }
```

Os status usados pelo worker são `Indexing`, `Available` e `IndexingFailed`.
Respostas 404 ou outros 4xx (exceto 429) ao buscar conteúdo são permanentes;
erros de rede, 5xx e 429 são tratados como transitórios.

## Persistência

Tabela `ingestion.document_chunks`:

- `id` UUID;
- `document_id` UUID e `version_id` UUID;
- `chunk_index`, `chunk_type`, `content`, `content_hash`;
- `metadata` como `jsonb`;
- `embedding` como `vector(768)`;
- `created_at_utc`.

Há restrição única em `(version_id, chunk_index)` e índice HNSW usando
`vector_cosine_ops` em `embedding`.

Tabela `ingestion.processed_integration_events`:

- `event_id` UUID como chave primária;
- `processed_at_utc`.

A substituição dos chunks da versão e o registro do evento são atômicos. Assim,
uma republicação de outra mensagem para a mesma versão não duplica vetores.

## Embeddings

`IEmbeddingGenerator` define 768 dimensões. Sua implementação chama o serviço
gRPC `EmbeddingService/EmbedDocument` no `Documentation.Embeddings`, que executa
`google/embeddinggemma-300m` localmente com normalização. `InvalidArgument` é
permanente; os demais erros gRPC são transitórios e seguem o retry RabbitMQ.
Não há provider externo ou cobrança por embedding.

## Configuração

| Chave | Padrão | Descrição |
| --- | --- | --- |
| `ConnectionStrings:Postgres` | — | PostgreSQL com extensão pgvector |
| `RabbitMq:Host` | `localhost` | Host RabbitMQ |
| `RabbitMq:Port` | `5672` | Porta AMQP |
| `RabbitMq:UserName` | `guest` | Usuário RabbitMQ |
| `RabbitMq:Password` | `guest` | Senha RabbitMQ |
| `RabbitMq:VirtualHost` | `/` | Virtual host RabbitMQ |
| `DocumentationApi:BaseUrl` | `http://localhost:8080` | Base da API interna |
| `Embeddings:BaseUrl` | `http://localhost:8080` | Serviço gRPC local EmbeddingGemma |
| `Embeddings:Dimensions` | `768` | Deve permanecer 768 nesta POC |

## Inicialização local

Na partida, o worker instala `vector`, cria o schema/tabelas/índice quando ainda
não existem e declara a topologia RabbitMQ. Se encontrar uma coluna com dimensão
diferente de 768, remove os chunks e eventos processados incompatíveis, migra a
coluna e marca as versões afetadas para republicação. O `Dockerfile` do worker
recebe a solução inteira como contexto de build.

## Limitações deliberadas da POC

Não há autenticação entre worker e API, outbox, telemetria distribuída,
retentativas HTTP sofisticadas ou validação completa da especificação OpenAPI.
O parser procura `paths` e verbos HTTP conhecidos; se não encontrar operações,
persiste ao menos o chunk geral.
