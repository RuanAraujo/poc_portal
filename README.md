# Documentation Portal POC

Prova de conceito em .NET 10 e Python para cadastro de especificações OpenAPI, ingestão assíncrona, busca semântica no pgvector e orientação de integração por agentes LangChain.

## Componentes

- `Documentation.Api`: cadastro e versionamento das documentações.
- `Documentation.Ingestion.Worker`: chunking e geração de embeddings.
- `Documentation.Embeddings`: EmbeddingGemma local via gRPC.
- `Documentation.Agent`: busca pgvector e agentes LangChain/FastAPI.
- NVIDIA NIM: modelos de chat OpenAI-compatible usados pelos agentes.
- `Documentation.Contracts`: contrato compartilhado da mensageria.
- PostgreSQL 17 com pgvector e RabbitMQ Management via Docker Compose.

As decisões de cada sistema estão em [`docs/specs`](docs/specs).

## Executar localmente

Pré-requisitos: Docker Desktop, Docker Compose e uma chave NVIDIA NIM. O serviço
baixa a versão ONNX fp32 do
[`google/embeddinggemma-300m`](https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX)
no primeiro startup; observe os termos da licença Gemma.

```bash
cp .env.example .env
# preencha LLM_API_KEY com a chave NVIDIA
docker compose up --build -d
docker compose ps
```

Serviços:

- Swagger: <http://localhost:8080/swagger>
- Health da API: <http://localhost:8080/health>
- Swagger do agente: <http://localhost:8090/docs>
- Health do agente: <http://localhost:8090/health>
- RabbitMQ Management: <http://localhost:15672>
- PostgreSQL: `localhost:5432`

Credenciais locais padrão: `documentation_user` / `documentation_password`.

Execute `./scripts/smoke.sh` para cadastrar uma OpenAPI pequena, aguardar `available`, validar os vetores de 768 dimensões e consultar o Agent.

Para conferir os vetores:

```bash
docker compose exec -T postgres psql \
  -U documentation_user \
  -d documentation_portal \
  -c "SELECT count(*) AS chunks, min(vector_dims(embedding)) AS dimensions FROM ingestion.document_chunks;"
```

## Embeddings

O `google/embeddinggemma-300m` roda via ONNX Runtime, em CPU, no serviço de
embeddings exposto internamente por gRPC. Ele gera vetores normalizados de 768
dimensões sem API paga; o volume `huggingface-cache` preserva os pesos.

Se uma instalação existente ainda tiver `vector(1024)`, o worker remove os
vetores incompatíveis, altera a coluna para `vector(768)` e marca as versões
afetadas como `indexingFailed`. Republique essas versões pela API para regenerar
os vetores.

## Agente de integração

O chat é stateless e usa os modelos NVIDIA NIM configurados em `AGENT_MODEL` e
`AGENT_FALLBACK_MODEL` para o supervisor e o especialista. Informe
`LLM_API_KEY` antes de iniciar o chat.

```bash
curl --fail-with-body http://localhost:8090/api/agents/chat \
  -H 'Content-Type: application/json' \
  --data '{"message":"Como integrar uma nova operacao nesta API?"}'
```

## Desenvolvimento

```bash
dotnet restore DocumentationPortal.sln
dotnet build DocumentationPortal.sln --no-restore
```

Para encerrar preservando os volumes:

```bash
docker compose down
```

`docker compose down -v` também apaga o cache do EmbeddingGemma e força um novo
download na próxima inicialização.
