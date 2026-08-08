# Documentation API

## Responsabilidade

O serviço `Documentation.Api` recebe documentações OpenAPI versionadas, persiste o conteúdo original no PostgreSQL e publica uma referência para indexação assíncrona. Ele é o sistema de registro das versões e do respectivo estado de indexação.

O conteúdo OpenAPI não segue na mensagem RabbitMQ. O worker de ingestão o consulta pelo endpoint interno da API.

## Arquitetura

Os projetos respeitam as dependências da Clean Architecture:

```text
Documentation.Api -> Application -> Domain
                  -> Infrastructure -> Application -> Domain
```

- `Documentation.Domain`: entidades e enumeração de estados.
- `Documentation.Application`: casos de uso, DTOs e abstrações de repositório, unit of work e mensageria.
- `Documentation.Infrastructure`: EF Core/Npgsql, repositórios e publisher RabbitMQ.
- `Documentation.Api`: HTTP, Swagger, health check e configuração de dependências.

## Modelo de persistência

O schema PostgreSQL é `documentation`.

| Tabela | Finalidade |
| --- | --- |
| `api_documentations` | Agregado da documentação identificada por `api_id`. |
| `documentation_versions` | Conteúdo OpenAPI imutável, ambiente e estado da indexação. |

Uma versão é única em `(documentation_id, version, environment)`. O banco cria o schema e as tabelas automaticamente no startup da POC através de `EnsureCreated`.

Estados possíveis:

```text
Publishing -> PendingIndexing -> Indexing -> Available
Publishing -> PublishFailed
Indexing -> IndexingFailed
```

## Contrato HTTP

O JSON usa `camelCase`.

| Método | Rota | Finalidade |
| --- | --- | --- |
| `POST` | `/api/documentations` | Cria documento e versão, então publica o evento. |
| `GET` | `/api/documentations` | Lista documentos e versões sem o conteúdo integral. |
| `GET` | `/api/documentations/{documentId}` | Consulta documento e versões sem conteúdo integral. |
| `POST` | `/api/documentations/{documentId}/versions/{versionId}/republish` | Reenvia uma versão existente para indexação. |
| `GET` | `/internal/documentations/{documentId}/versions/{versionId}/content` | Fornece formato e conteúdo ao worker. |
| `PUT` | `/internal/documentations/{documentId}/versions/{versionId}/indexing-status` | Atualiza o estado retornado pelo worker. |

`POST /api/documentations` recebe `apiId`, `name`, `version`, `environment`, `format` e `content`. Se o publisher confirmar a mensagem, responde `202 Accepted` com `pendingIndexing`. Se a publicação falhar, a versão é mantida como `publishFailed` e a resposta é `503 Service Unavailable`, incluindo os IDs para posterior republicação. Uma versão repetida resulta em `409 Conflict`.

O endpoint de status aceita apenas `indexing`, `available` e `indexingFailed`. As rotas internas não possuem autenticação nesta POC e não devem ser expostas fora da rede local de containers.

## Mensageria

Ao publicar ou republicar uma versão, a API envia `DocumentationPublished` definido em `Documentation.Contracts`:

- Exchange topic: `documentation.events`.
- Routing key: `documentation.published.v1`.
- JSON persistente em `camelCase`.
- Publisher confirms obrigatórios.
- A mensagem contém `eventId`, IDs e metadados da versão, nunca `content`.

## Configuração

| Chave | Uso |
| --- | --- |
| `ConnectionStrings__DocumentationDb` | PostgreSQL da API. |
| `RabbitMq__HostName` | Host RabbitMQ. |
| `RabbitMq__Port` | Porta AMQP (padrão `5672`). |
| `RabbitMq__UserName` / `RabbitMq__Password` | Credenciais AMQP. |
| `ASPNETCORE_URLS` | Deve ser `http://+:8080` no container. |

Swagger fica em `/swagger` e o health check em `/health`.

## Limites da POC

- Sem autenticação, outbox, paginação ou validação profunda do OpenAPI.
- Não há garantia transacional entre banco e broker; a republicação recupera falhas de publicação.
- `EnsureCreated` é usado no lugar de migrations para reduzir a complexidade inicial.
