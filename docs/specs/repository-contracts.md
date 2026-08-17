# Especificação dos contratos compartilhados do repositório

## 1. Propósito

Este documento define os contratos que conectam os projetos da POC e as regras
que devem orientar a implementação do sistema definitivo. Ele distingue:

- contratos compartilhados em código por `Documentation.Contracts`;
- contratos de integração mantidos estruturalmente entre serviços;
- ports internos da camada `Documentation.Application`;
- convenções de compatibilidade que hoje são implícitas.

O estado é *as built*: somente evento e nomes RabbitMQ estão centralizados em
um assembly compartilhado. Os DTOs HTTP internos ainda são duplicados entre
produtor e consumidor.

## 2. Plataforma e organização

| Item | Valor atual |
| --- | --- |
| Solução | `DocumentationPortal.sln` |
| SDK usado pelo repositório | .NET SDK 10.0.302 |
| Target dos projetos deste contrato | `net10.0` |
| Nullable/implicit usings em `Documentation.Contracts` | Habilitados |
| Serialização interoperável | JSON com nomes `camelCase` |
| Identificadores | UUID / `Guid` |
| Timestamps de integração | `DateTimeOffset`, UTC na produção dos eventos |

Dependências de compilação do núcleo de documentação:

```text
Documentation.Api ----------> Documentation.Application
       |                                 |
       +--> Documentation.Infrastructure +--> Documentation.Domain
       |                                 +--> Documentation.Contracts
       +--> Documentation.Contracts

Documentation.Infrastructure --> Documentation.Application
                             --> Documentation.Domain
                             --> Documentation.Contracts

Documentation.Ingestion.Worker --------> Documentation.Contracts
Documentation.Ingestion.Infrastructure -> Documentation.Contracts
```

`Documentation.Contracts` não referencia outros projetos nem pacotes de
infraestrutura. Essa direção deve ser preservada.

## 3. Contrato compartilhado em código

O assembly contém apenas:

| Símbolo | Responsabilidade |
| --- | --- |
| `DocumentationPublished` | Envelope tipado da publicação de uma versão. |
| `RabbitMqTopology` | Nomes canônicos de exchange, routing key e filas. |

Ele não contém DTOs HTTP, entidades, opções de infraestrutura, lógica de
retry ou abstrações de aplicação.

## 4. Evento `DocumentationPublished`

### 4.1 Semântica

O evento afirma que uma versão já foi persistida pela API e está pronta para
ser buscada e indexada. Ele referencia uma versão imutável; não carrega o
conteúdo. Cada criação ou republicação gera um novo `eventId`.

Nome lógico obrigatório: `DocumentationPublished`.

### 4.2 Payload canônico

| Campo JSON | Tipo | Origem e uso |
| --- | --- | --- |
| `eventId` | UUID | Novo por tentativa; chave de idempotência do consumidor. |
| `eventType` | string | Valor exato `DocumentationPublished`; validado pelo worker. |
| `documentId` | UUID | Identidade interna do agregado na API. |
| `versionId` | UUID | Identidade interna da versão e chave de acesso ao conteúdo. |
| `apiId` | string | Identidade externa da API; metadado no consumidor atual. |
| `version` | string | Versão informada pelo cliente; metadado no consumidor atual. |
| `environment` | string | Ambiente informado pelo cliente; metadado no consumidor atual. |
| `occurredAt` | timestamp ISO-8601 | Instante UTC da criação da tentativa de publicação. |

Exemplo:

```json
{
  "eventId": "52e833f8-d8f0-4b44-a52f-463cf64242be",
  "eventType": "DocumentationPublished",
  "documentId": "a53d17ec-c8b6-4d96-b7fc-cce9bc029c10",
  "versionId": "2b653cbd-fbd8-4295-9e55-62eb655a0d46",
  "apiId": "payments-api",
  "version": "1.0.0",
  "environment": "production",
  "occurredAt": "2026-08-16T15:00:01+00:00"
}
```

O publisher usa `JsonSerializerDefaults.Web`; o consumidor também. O formato
canônico é camelCase, embora o desserializador web seja tolerante a caixa e a
campos JSON adicionais.

### 4.3 Propriedades AMQP

| Propriedade | Valor produzido pela API |
| --- | --- |
| `ContentType` | `application/json` |
| `Type` | `DocumentationPublished` |
| `MessageId` | `eventId.ToString()` |
| `CorrelationId` | correlação HTTP efetiva ou `eventId` sem hífens |
| `Timestamp` | `occurredAt` convertido em segundos Unix |
| Persistência | habilitada |

O consumidor preserva corpo, `MessageId` e `CorrelationId` ao copiar mensagens
para retry ou DLQ, e acrescenta/atualiza o header `x-retry-count`. A cópia não
preserva explicitamente `Type` nem `Timestamp`; o processamento depende do
payload, não dessas propriedades.

### 4.4 Validação atual do consumidor

O worker:

1. desserializa o corpo como `DocumentationPublished`;
2. exige objeto não nulo;
3. exige `eventType` exatamente igual a `DocumentationPublished`.

Ele não valida UUIDs vazios, strings vazias ou coerência dos metadados com o
conteúdo recuperado. Falhas posteriores de busca ou parsing cobrem parte desses
casos, mas isso não substitui validação explícita na borda.

## 5. Topologia RabbitMQ

Constantes canônicas:

| Item | Valor |
| --- | --- |
| Exchange | `documentation.events` |
| Tipo do exchange | `topic` |
| Routing key de publicação | `documentation.published.v1` |
| Fila principal | `documentation.ingestion.v1` |
| Fila de retry | `documentation.ingestion.retry.v1` |
| Dead-letter queue | `documentation.ingestion.dlq.v1` |

Todas as entidades são duráveis, não exclusivas e não auto-delete.

Responsabilidade atual de declaração:

| Componente | Declara |
| --- | --- |
| Publisher da API | Exchange, fila principal e binding principal. |
| Worker de ingestão | Exchange, fila principal/binding, retry com TTL/dead-letter e DLQ. |

A fila de retry usa:

- `x-message-ttl = RabbitMq:RetryDelaySeconds * 1000`;
- `x-dead-letter-exchange = documentation.events`;
- `x-dead-letter-routing-key = documentation.published.v1`.

Retry e DLQ recebem cópias pelo default exchange, usando o próprio nome da fila
como routing key. Publicação inicial e republicações do worker usam publisher
confirms com prazo de cinco segundos. A publicação inicial usa
`mandatory = false`; cópias para retry/DLQ usam `mandatory = true`.

Alterações de nome ou argumentos de uma fila existente podem causar erro de
precondition no RabbitMQ. Topologia deve ser alterada de forma coordenada ou
por uma estratégia de provisionamento/versionamento.

## 6. Contrato HTTP interno API-ingestão

Este contrato é interoperável, mas não vive em `Documentation.Contracts`.

### 6.1 Obter conteúdo

```http
GET /internal/documentations/{documentId}/versions/{versionId}/content
```

Resposta canônica da API em `200 OK`:

| Campo | Tipo | Consumido hoje pelo worker |
| --- | --- | --- |
| `documentId` | UUID | Não; ignorado. |
| `versionId` | UUID | Não; ignorado. |
| `format` | string | Sim. |
| `content` | string | Sim; vazio é falha permanente. |
| `apiId` | string | Não; ignorado. |
| `version` | string | Não; ignorado. |
| `environment` | string | Não; ignorado. |

`404` significa que o par documento/versão não existe. Para a ingestão, todo
erro desta chamada, inclusive 5xx, é tratado atualmente como permanente; a
única exceção é `429 Too Many Requests`, classificado como transitório.

### 6.2 Atualizar estado

```http
PUT /internal/documentations/{documentId}/versions/{versionId}/indexing-status
Content-Type: application/json
```

Body canônico:

```json
{
  "status": "indexingFailed",
  "error": "descrição opcional"
}
```

Estados aceitos: `indexing`, `available` e `indexingFailed`.

| Resposta | Semântica |
| --- | --- |
| `204 No Content` | Estado persistido. |
| `400 Bad Request` | Estado não permitido. |
| `404 Not Found` | Par documento/versão ausente. |

O cliente da ingestão possui seu próprio enum com os mesmos três nomes e envia
somente `status`; portanto `error` existe no contrato da API, mas não é
propagado pelo worker atual. No cliente, 4xx exceto 429 são permanentes; 429 e
5xx são transitórios.

### 6.3 Correlação

O worker encaminha `X-Correlation-ID` obtido do baggage da atividade. A API
valida e devolve o valor efetivo no mesmo header. O contrato admite 1 a 128
caracteres, iniciando por ASCII alfanumérico e continuando com ASCII
alfanumérico, `.`, `_`, `:` ou `-`.

### 6.4 Risco de duplicação estrutural

Hoje existem modelos distintos:

- API: `Documentation.Application.Models.DocumentationContent`, com sete campos;
- worker: `Documentation.Ingestion.Application.Models.DocumentationContent`,
  com `Format` e `Content`;
- API: `DocumentationVersionStatus`;
- worker: `DocumentationIndexingStatus`;
- requests de status privadas e públicas diferentes.

A tolerância do `System.Text.Json` torna a resposta maior compatível com o
cliente menor. Renomear `format`, `content` ou os nomes dos estados, porém,
quebrará a integração sem erro de compilação. O sistema definitivo deve manter
testes de contrato/OpenAPI ou gerar o cliente; mover DTOs HTTP para o assembly
de eventos não é obrigatório e misturaria responsabilidades.

## 7. Ports internos de `Documentation.Application`

Estes contratos organizam as camadas da API; não são contratos entre serviços.

### 7.1 Persistência

| Interface | Operação | Semântica requerida pela aplicação |
| --- | --- | --- |
| `IApiDocumentationRepository` | `GetByApiIdAsync(apiId)` | Documento com suas versões ou nulo. |
|  | `GetByIdAsync(id)` | Documento com suas versões ou nulo. |
|  | `ListAsync()` | Todos os documentos com versões, ordenados por nome na implementação. |
|  | `Add(documentation)` | Adiciona ao unit of work, sem salvar imediatamente. |
| `IDocumentationVersionRepository` | `GetByIdAsync(documentationId, versionId)` | Versão somente se pertencer ao documento. |
|  | `ExistsAsync(documentationId, version, environment)` | Pré-checagem de duplicidade. |
|  | `Add(version)` | Adiciona ao unit of work, sem salvar imediatamente. |
| `IUnitOfWork` | `SaveChangesAsync()` | Persiste mudanças rastreadas e retorna a contagem do EF Core. |

As implementações são scoped e compartilham o mesmo
`DocumentationDbContext`, que também implementa `IUnitOfWork`. `Add` não deve
abrir transação ou executar commit próprio.

### 7.2 Mensageria

```csharp
Task PublishAsync(
    DocumentationPublished integrationEvent,
    CancellationToken cancellationToken = default);
```

`IDocumentationEventPublisher` pertence à Application e sua implementação à
Infrastructure. Sucesso significa confirmação do broker; falha deve aparecer
como exceção para que o serviço marque `PublishFailed`.

## 8. Contratos de resposta da API

Embora não compartilhados em assembly, estes formatos são consumidos pelo
portal e devem ser tratados como API pública:

| Contrato | Campos |
| --- | --- |
| `PublishDocumentationResponse` | `documentId`, `versionId`, `status`, `error?` |
| `DocumentationSummary` | `id`, `apiId`, `name`, `createdAtUtc`, `versions[]` |
| `DocumentationVersionSummary` | `id`, `version`, `environment`, `format`, `status`, `lastError?`, `createdAtUtc`, `publishedAtUtc?`, `indexingUpdatedAtUtc?` |

O conteúdo integral não faz parte de summaries.

## 9. Regras de compatibilidade para evolução

Enquanto houver produtores e consumidores implantados separadamente:

- manter os nomes atuais de exchange, routing key, filas e campos obrigatórios;
- manter `eventType = DocumentationPublished` e a semântica de um novo
  `eventId` por tentativa;
- tratar adição de campo opcional como evolução compatível;
- não remover, renomear ou mudar o tipo de campos existentes na mesma versão;
- criar nova routing key/versionamento para mudança incompatível de evento;
- manter o conteúdo fora do evento e acessível pelo par documento/versão;
- preservar strings camelCase dos estados HTTP;
- consumidores devem ignorar campos JSON desconhecidos;
- produtores não devem depender dessa tolerância para omitir campos canônicos;
- documentar prazo de retenção do conteúdo maior que a janela máxima de retry;
- manter idempotência por `eventId` e substituição dos chunks por versão;
- propagar correlação sem usá-la como identidade de negócio.

## 10. Requisitos derivados para o sistema definitivo

- RC-01: `Documentation.Contracts` deve continuar pequeno e independente de
  infraestrutura.
- RC-02: evento, propriedades AMQP e topologia devem possuir testes de contrato
  entre publisher e consumidor.
- RC-03: o contrato HTTP interno deve ser publicado em OpenAPI e validado no CI
  ou gerar o cliente da ingestão.
- RC-04: mudanças incompatíveis devem criar versão nova e permitir migração
  coordenada.
- RC-05: callbacks internos devem ser autenticados e autorizados.
- RC-06: falhas transitórias de leitura de conteúdo não devem ir diretamente à
  DLQ sem política intencional.
- RC-07: falhas definitivas devem transportar causa sanitizada até o estado
  `indexingFailed` quando isso for útil operacionalmente.
- RC-08: validar campos essenciais do evento antes de executar I/O.
- RC-09: definir ownership único da topologia ou provisioná-la fora dos
  processos para evitar declarações divergentes.
- RC-10: segredos e endpoints devem vir de configuração segura, nunca dos
  defaults de desenvolvimento.

## 11. Limites da POC

- nenhum schema registry ou versionamento formal de payload;
- nenhum teste automatizado de compatibilidade entre projetos;
- DTOs HTTP e enums duplicados;
- somente `eventType` é validado na entrada AMQP;
- publicação não usa outbox;
- API e worker declaram partes sobrepostas da topologia;
- defaults locais incluem credenciais conhecidas;
- callback interno não possui autenticação;
- publisher inicial usa `mandatory = false`;
- causa de `IndexingFailed` não é enviada pelo worker.

## 12. Decisões a preservar e reavaliar

### Preservar

- contrato de evento separado de Domain/Application/Infrastructure;
- evento referencial e pequeno, sem documentação integral;
- `eventId` distinto dos IDs de documento e versão;
- routing key explicitamente versionada com `.v1`;
- mensagens e filas duráveis com publisher confirms;
- correlação preservada em HTTP e AMQP;
- idempotência do consumidor por evento.

### Reavaliar

- estratégia de versionamento e compatibilidade de eventos;
- classificação de todo erro ao buscar conteúdo como permanente;
- duplicação dos contratos HTTP sem teste ou geração de cliente;
- ausência do erro detalhado/sanitizado no callback de falha;
- declaração da topologia dentro de publisher e worker;
- `mandatory = false` na publicação inicial;
- outbox e garantias de entrega;
- validação mínima do payload recebido;
- autenticação e política de rede do contrato interno.
