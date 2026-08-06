# Repositório e contratos compartilhados

## Plataforma

- .NET SDK 10.0.302 e target framework `net10.0`.
- Solução `DocumentationPortal.sln`.
- PostgreSQL/pgvector e RabbitMQ executados pelo Docker Compose.
- JSON público e de mensageria em camelCase.

## Evento `DocumentationPublished`

O evento referencia uma versão imutável. O conteúdo OpenAPI não é transportado pelo RabbitMQ; o worker o obtém pelo endpoint interno da API.

| Campo | Tipo |
| --- | --- |
| `eventId` | UUID |
| `eventType` | `DocumentationPublished` |
| `documentId` | UUID |
| `versionId` | UUID |
| `apiId` | string |
| `version` | string |
| `environment` | string |
| `occurredAt` | timestamp ISO-8601 |

## RabbitMQ

- Exchange topic durável: `documentation.events`.
- Routing key: `documentation.published.v1`.
- Fila principal: `documentation.ingestion.v1`.
- Retry: `documentation.ingestion.retry.v1`.
- DLQ: `documentation.ingestion.dlq.v1`.
- Mensagens persistentes e publisher confirms.

## Ownership durante a implementação

- Coordenador: solution, contratos, arquivos raiz e integração.
- Agent API: `Documentation.Domain`, `Documentation.Application`, `Documentation.Infrastructure`, `Documentation.Api` e sua especificação.
- Agent Ingestion: projetos `Documentation.Ingestion.*` e sua especificação.
- Agent Infra: Compose, `.env.example`, configuração de containers e sua especificação.
