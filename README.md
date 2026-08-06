# Documentation Portal POC

Prova de conceito em .NET 10 para cadastro de especificações OpenAPI, publicação de eventos no RabbitMQ, ingestão assíncrona e persistência de embeddings no PostgreSQL com pgvector.

## Componentes

- `Documentation.Api`: cadastro e versionamento das documentações.
- `Documentation.Ingestion.Worker`: chunking e geração de embeddings.
- `Documentation.Contracts`: contrato compartilhado da mensageria.
- `compose.yaml`: ambiente local com API, worker, PostgreSQL/pgvector e RabbitMQ.

As instruções de execução serão consolidadas após a implementação dos serviços.
