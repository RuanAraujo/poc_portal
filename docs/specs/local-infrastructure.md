# Especificação: infraestrutura local

## Objetivo

Disponibilizar o ambiente local da POC por meio de Docker Compose, com PostgreSQL
17 + pgvector, RabbitMQ Management, a API de documentação, o serviço de embeddings,
o agente Python, o portal web e o worker de ingestão. O ambiente deve funcionar em máquinas ARM64 e AMD64 que suportem as
imagens oficiais utilizadas.

## Serviços

| Serviço | Imagem/build | Porta publicada | Responsabilidade |
| --- | --- | --- | --- |
| `postgres` | `pgvector/pgvector:pg17` | `5432` | Banco `documentation_portal`, extensão `vector` e schemas da POC. |
| `rabbitmq` | `rabbitmq:4-management` | `5672`, `15672` | Broker AMQP e painel de gerenciamento local. |
| `documentation-api` | `src/Documentation.Api/Dockerfile` | `8080` | API HTTP e Swagger local. |
| `documentation-frontend` | `src/Documentation.Frontend/Dockerfile` | `3000` | Portal web local. |
| `documentation-embeddings` | `src/Documentation.Embeddings/Dockerfile` | nenhuma | EmbeddingGemma gRPC interno (`8080`) e health (`8081`). |
| `documentation-agent` | `src/Documentation.Agent/Dockerfile` | `8090` | Busca pgvector e agentes LangChain. |
| `documentation-ingestion` | `src/Documentation.Ingestion.Worker/Dockerfile` | nenhuma | Consumo de eventos e persistência dos vetores. |

## Estado e inicialização

- Os dados do PostgreSQL ficam no volume nomeado `postgres-data`.
- Os dados do RabbitMQ ficam no volume nomeado `rabbitmq-data`.
- Os pesos do EmbeddingGemma ficam no volume nomeado `huggingface-cache`.
- Os modelos de chat são acessados no NVIDIA NIM e não ocupam volumes Docker.
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
| `Embeddings__BaseUrl` | URL gRPC interna: `http://documentation-embeddings:8080`. |
| `EMBEDDING_DIMENSIONS` | Dimensão fixa dos vetores persistidos: `768`. |
| `MODEL_DIR` | Cache do modelo ONNX no serviço de embeddings: `/models/huggingface`. |
| `LLM_API_KEY`, `LLM_BASE_URL`, `AGENT_MODEL`, `AGENT_FALLBACK_MODEL`, `LLM_MAX_TOKENS` | NVIDIA NIM OpenAI-compatible, chave, modelo principal, fallback e limite da resposta. |
| `EMBEDDING_GRPC_ADDRESS` | Destino gRPC usado pelo agente: `documentation-embeddings:8080`. |
| `FRONTEND_PORT` | Porta publicada pelo portal web (padrão `3000`). |
| `PORTAL_ORIGIN` | Origem exata do portal, repassada à API e ao agente para CORS (padrão `http://localhost:3000`). |
| `DOCUMENTATION_API_URL`, `DOCUMENTATION_AGENT_URL` | URLs públicas que o navegador usa para acessar API e agente; não use nomes de serviço do Compose. |

## Saúde e ordem de inicialização

- PostgreSQL: `pg_isready` contra o banco configurado.
- RabbitMQ: `rabbitmq-diagnostics -q ping`.
- API: healthcheck HTTP em `/health` executado dentro do container.
- A API inicia após PostgreSQL e RabbitMQ ficarem saudáveis.
- O serviço de embeddings fica saudável após carregar o EmbeddingGemma.
- O agente inicia após PostgreSQL e o serviço de embeddings ficarem saudáveis.
- O portal verifica HTTP em sua raiz; ele inicia independentemente dos serviços de backend.
- O worker inicia após PostgreSQL, RabbitMQ, API e embeddings ficarem saudáveis.

O endpoint `/health` da API é parte do contrato de inicialização: se ele não
responder com sucesso, o Compose não inicia o worker.

## Operação local

```bash
cp .env.example .env
docker compose up --build
```

URLs locais:

- Portal: `http://localhost:3000`
- Swagger: `http://localhost:8080/swagger`
- Swagger do agente: `http://localhost:8090/docs`
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
- O portal é somente para uso local da POC; não há autenticação ou autorização.
- Usuários e senhas locais são configuráveis por `.env`; não use estes valores
  fora do ambiente de desenvolvimento.
- O primeiro startup do serviço de embeddings baixa o artefato ONNX fixado no
  código a partir do Hugging Face. A inferência posterior é local e sem API paga.
- O uso do modelo deve respeitar a licença Gemma.
- O chat requer `LLM_API_KEY`; ela não deve ser versionada.

## Redes e exposição

O Compose usa a rede padrão do projeto. PostgreSQL, RabbitMQ, API, Agent e
Frontend publicam portas no host para facilitar diagnóstico local. Embeddings
expõe gRPC `8080` e health HTTP `8081` somente para a rede interna; o worker não
publica porta.

`DOCUMENTATION_API_URL` e `DOCUMENTATION_AGENT_URL` são injetadas pelo template
do Nginx em `config.js` e precisam ser URLs alcançáveis pelo navegador.
`DocumentationApi__BaseUrl` e `EMBEDDING_GRPC_ADDRESS`, por outro lado, usam os
nomes DNS internos do Compose.

## Requisitos para ambientes definitivos

- construir e identificar imagens imutáveis por versão/commit, sem depender de
  `latest` implícito;
- separar redes e permitir somente os fluxos descritos na especificação geral;
- armazenar credenciais e a chave do LLM em um gerenciador de segredos;
- remover credenciais padrão e `Include Error Detail=true` fora do
  desenvolvimento;
- publicar apenas os endpoints necessários atrás de TLS e controle de acesso;
- separar liveness de readiness e incluir health do worker/consumidor;
- executar migrations como etapa controlada de deploy;
- definir requests/limits de CPU e memória, especialmente para embeddings;
- monitorar espaço dos volumes, fila/DLQ, latência, erros e disponibilidade;
- definir backup e teste periódico de restauração do PostgreSQL e a política de
  retenção do RabbitMQ;
- preservar o cache do modelo ou empacotar artefatos validados para evitar
  download externo durante o startup;
- fixar e verificar checksum/licença dos artefatos de modelo.

O Compose continua útil como ambiente de desenvolvimento e teste de integração;
ele não constitui por si só uma especificação de produção ou alta
disponibilidade.
