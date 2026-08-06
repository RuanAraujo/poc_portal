# Documentation Portal POC

Prova de conceito em .NET 10 para cadastro de especificações OpenAPI, publicação de eventos no RabbitMQ, ingestão assíncrona e persistência de embeddings no PostgreSQL com pgvector.

## Componentes

- `Documentation.Api`: cadastro e versionamento das documentações.
- `Documentation.Ingestion.Worker`: chunking e geração de embeddings.
- `Documentation.Contracts`: contrato compartilhado da mensageria.
- PostgreSQL 17 com pgvector e RabbitMQ Management via Docker Compose.

As decisões de cada sistema estão em [`docs/specs`](docs/specs).

## Executar localmente

Pré-requisitos: Docker Desktop e Docker Compose.

```bash
cp .env.example .env
docker compose up --build -d
docker compose ps
```

Serviços:

- Swagger: <http://localhost:8080/swagger>
- Health da API: <http://localhost:8080/health>
- RabbitMQ Management: <http://localhost:15672>
- PostgreSQL: `localhost:5432`

Credenciais locais padrão: `documentation_user` / `documentation_password`.

Execute `./scripts/smoke.sh` para cadastrar uma OpenAPI pequena e consultar o status. O processamento normalmente muda de `pendingIndexing` para `available` em poucos segundos.

Para conferir os vetores:

```bash
docker compose exec -T postgres psql \
  -U documentation_user \
  -d documentation_portal \
  -c "SELECT count(*) AS chunks, min(vector_dims(embedding)) AS dimensions FROM ingestion.document_chunks;"
```

## Embeddings

O provider padrão é `Fake`, determinístico e com 1024 dimensões. Para usar Amazon Bedrock, configure `EMBEDDINGS_PROVIDER=Bedrock`, credenciais AWS válidas, região e o modelo `amazon.titan-embed-text-v2:0` antes de recriar o worker.

## Desenvolvimento

```bash
dotnet restore DocumentationPortal.sln
dotnet build DocumentationPortal.sln --no-restore
```

Para encerrar preservando os volumes:

```bash
docker compose down
```
