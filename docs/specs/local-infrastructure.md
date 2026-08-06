# Especificação: infraestrutura local

## Objetivo

Disponibilizar o ambiente local da POC por meio de Docker Compose, com PostgreSQL
17 + pgvector, RabbitMQ Management, a API de documentação e o worker de
ingestão. O ambiente deve funcionar em máquinas ARM64 e AMD64 que suportem as
imagens oficiais utilizadas.

## Serviços

| Serviço | Imagem/build | Porta publicada | Responsabilidade |
| --- | --- | --- | --- |
| `postgres` | `pgvector/pgvector:pg17` | `5432` | Banco `documentation_portal`, extensão `vector` e schemas da POC. |
| `rabbitmq` | `rabbitmq:4-management` | `5672`, `15672` | Broker AMQP e painel de gerenciamento local. |
| `documentation-api` | `src/Documentation.Api/Dockerfile` | `8080` | API HTTP e Swagger local. |
| `documentation-ingestion` | `src/Documentation.Ingestion.Worker/Dockerfile` | nenhuma | Consumo de eventos e persistência dos vetores. |

## Estado e inicialização

- Os dados do PostgreSQL ficam no volume nomeado `postgres-data`.
- Os dados do RabbitMQ ficam no volume nomeado `rabbitmq-data`.
- Na primeira criação do volume do PostgreSQL, `infra/postgres/init.sql` habilita
  a extensão `vector` e cria os schemas `documentation` e `ingestion`.
- O script é idempotente e pode ser executado manualmente em um banco já criado.
- As migrations ou `EnsureCreated` dos serviços continuam responsáveis pelas
  tabelas; este repositório de infraestrutura não pressupõe o ORM adotado.

## Configuração por ambiente

O arquivo `.env` (copiado de `.env.example`) é carregado automaticamente pelo
Compose. Ele contém somente valores locais de POC e placeholders, nunca
credenciais reais.

Variáveis compartilhadas que os containers recebem:

| Variável | Uso |
| --- | --- |
| `ConnectionStrings__Postgres`, `ConnectionStrings__DocumentationDb` | String de conexão Npgsql para `documentation_portal`; o segundo nome é o alias adotado pela API. |
| `RabbitMq__Host`, `RabbitMq__HostName`, `RabbitMq__Port`, `RabbitMq__UserName`, `RabbitMq__Password`, `RabbitMq__VirtualHost` | Conexão com o RabbitMQ; `HostName` é o alias adotado pela API. |
| `DocumentationApi__BaseUrl` | URL interna usada pelo worker: `http://documentation-api:8080`. |
| `Embeddings__Provider` | `Fake` por padrão local. |
| `Embeddings__Dimensions` | Dimensão fixa dos embeddings: `1024`. |
| `Embeddings__BedrockRegion`, `Embeddings__BedrockModelId` | Região e modelo (`amazon.titan-embed-text-v2:0`) do provider Bedrock. |
| `AWS__Region`, `AWS_REGION`, `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_SESSION_TOKEN` | Placeholders opcionais para o provider Bedrock; não necessários no provider Fake. |

## Saúde e ordem de inicialização

- PostgreSQL: `pg_isready` contra o banco configurado.
- RabbitMQ: `rabbitmq-diagnostics -q ping`.
- API: healthcheck HTTP em `/health` executado dentro do container.
- A API inicia após PostgreSQL e RabbitMQ ficarem saudáveis.
- O worker inicia após PostgreSQL, RabbitMQ e API ficarem saudáveis.

O endpoint `/health` deve ser provido pelo projeto da API. Enquanto ele não
existir, o healthcheck da API naturalmente impedirá o worker de iniciar, o que
torna explícito o contrato de integração.

## Operação local

```bash
cp .env.example .env
docker compose up --build
```

URLs locais:

- Swagger: `http://localhost:8080/swagger`
- RabbitMQ Management: `http://localhost:15672`
- PostgreSQL: `localhost:5432`

Para encerrar preservando dados:

```bash
docker compose down
```

Para recriar os volumes da POC deliberadamente:

```bash
docker compose down -v
```

## Limites da POC

- Não há TLS, autenticação externa, secrets manager, monitoramento ou backup.
- Usuários e senhas locais são configuráveis por `.env`; não use estes valores
  fora do ambiente de desenvolvimento.
- O provider padrão de embeddings é o determinístico `Fake`; Bedrock requer
  credenciais AWS válidas fornecidas externamente.
