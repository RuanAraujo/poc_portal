# Especificação — Documentation Agent

## 1. Objetivo e responsabilidade

`Documentation.Agent` responde, em português, perguntas sobre integração de
recursos do Documentation Portal. Ele combina um LLM compatível com a API da
OpenAI, busca semântica sobre documentações já indexadas e um fluxo supervisor /
especialista. O serviço expõe a resposta como texto em streaming.

O agente somente consulta dados. Publicação, versionamento e indexação pertencem,
respectivamente, à `Documentation.Api` e à `Documentation.Ingestion.Worker`.

## 2. Arquitetura real

O código Python separa quatro áreas:

| Área | Responsabilidade |
| --- | --- |
| `domain` | Modelo imutável `KnowledgeChunk`. |
| `application` | Portas e casos de uso de chat, saúde e busca. Não importa FastAPI, gRPC, psycopg ou LangChain. |
| `infrastructure` | Configuração, LLM/LangChain, ferramentas, gRPC de embeddings e consulta pgvector. |
| `interface_adapters` | Rotas FastAPI, CORS, correlação e logs. |

`app.py` é a raiz de composição:

```text
HTTP -> ChatUseCase -> LangChainAgentGateway
                              |
                              v
                supervisor -> especialista -> search_system_knowledge
                                                    |
                                                    v
                         Embeddings gRPC -> busca pgvector no PostgreSQL
```

No ciclo de vida da aplicação, o canal gRPC é criado no startup e fechado no
shutdown. O supervisor e seus modelos são construídos sob demanda na primeira
conversa e reutilizados pelo processo. O container executa um único worker do
Uvicorn, na porta interna `8080`, sem access log padrão.

## 3. Contrato HTTP

### `GET /health`

Verifica em paralelo:

- conexão PostgreSQL, com `SELECT 1` e timeout de conexão de 5 segundos;
- prontidão do canal gRPC de embeddings, com timeout de 5 segundos.

Respostas:

| Status | Corpo |
| --- | --- |
| `200` | `{"status":"healthy"}` |
| `503` | `{"detail":"PostgreSQL is unavailable."}` |
| `503` | `{"detail":"Embedding service is unavailable."}` |

O health não valida o LLM nem executa inferência de embedding. Portanto, serviço
saudável significa apenas que PostgreSQL e canal gRPC estão alcançáveis.

### `POST /api/agents/chat`

Requisição JSON:

```json
{ "message": "Como integrar uma nova operação?" }
```

`message` é obrigatório, tem espaços externos removidos e deve conter entre 1 e
10.000 caracteres. Violações retornam o `422` padrão do FastAPI/Pydantic.

A resposta `200` é `text/plain; charset=utf-8`, enviada incrementalmente. O
serviço aguarda o primeiro fragmento antes de confirmar o sucesso: exceção ou
stream vazio antes desse ponto resulta em `502` com
`{"detail":"Agent invocation failed."}`. Falha depois do início da resposta é
registrada e interrompe o stream; já não pode ser convertida com segurança em um
novo status HTTP.

FastAPI também disponibiliza OpenAPI e Swagger nos caminhos padrão (`/openapi.json`
e `/docs`).

## 4. Fluxo da conversa

1. A rota valida a pergunta, registra o início e chama `ChatUseCase`.
2. O gateway inicializa dois `ChatOpenAI`: modelo principal e fallback, ambos com
   streaming, `reasoning_effort="none"` e o mesmo limite de tokens.
3. O supervisor recebe a pergunta. Seu prompt restringe o tema a integração de
   recursos do Portal e ordena delegação a `consult_feature_integration_specialist`.
4. A ferramenta invoca o agente especialista.
5. O especialista deve chamar `search_system_knowledge`, não inventar contratos e
   citar `api_id`, versão e ambiente dos chunks usados.
6. A busca gera um vetor via gRPC e consulta os três chunks disponíveis mais
   próximos no PostgreSQL.
7. A resposta final do especialista volta ao supervisor, que a transmite ao
   cliente. Tokens internos do especialista e chamadas de ferramenta não são
   enviados ao navegador.

`ModelFallbackMiddleware` tenta o modelo secundário quando o primário falha, tanto
no supervisor quanto no especialista. Qualquer falha não absorvida é convertida
em `AgentInvocationFailed`.

O filtro de saída remove blocos `<think>...</think>`, inclusive quando as tags
chegam fragmentadas. Conteúdo de raciocínio estruturado também é ignorado; somente
blocos textuais são considerados. Se `<think>` não for fechado, seu restante é
descartado.

## 5. Busca de conhecimento e contratos integrados

### Embeddings

O cliente chama `EmbeddingService/EmbedQuery` com:

- `text`: pergunta de busca;
- metadata gRPC `x-correlation-id`;
- deadline de 100 segundos.

A resposta deve conter exatamente 768 números finitos. Erro gRPC, dimensão errada
ou valor não finito se torna `EmbeddingUnavailable`. O canal é inseguro
(`grpc.aio.insecure_channel`) na rede interna da POC.

### PostgreSQL / pgvector

A consulta lê `ingestion.document_chunks` e associa os schemas de documentação.
Somente versões com status `Available` participam. A ordenação usa distância
cosseno (`<=>`) e retorna no máximo três chunks, cada conteúdo truncado em 4.000
caracteres. Não há score mínimo nem filtro prévio por API, versão, ambiente ou tipo
de chunk.

Cada resultado entregue ao LLM contém:

| Campo | Significado |
| --- | --- |
| `api_id`, `api_name` | Identidade legível da API. |
| `version`, `environment` | Contexto da versão indexada. |
| `chunk_type`, `metadata`, `content` | Trecho recuperado e sua classificação. |
| `document_id`, `version_id` | IDs persistidos, serializados como texto. |
| `score` | Similaridade calculada como `1 - distância`. |

A ferramenta retorna JSON UTF-8. Indisponibilidade de embeddings ou banco é
retornada ao agente como objeto `error`, em vez de abortar imediatamente a
conversa, permitindo que o LLM explique a indisponibilidade sem inventar dados.

## 6. Configuração

| Variável | Padrão no código | Uso |
| --- | --- | --- |
| `EMBEDDING_GRPC_ADDRESS` | `localhost:8080` | Serviço gRPC de embeddings; no Compose, `documentation-embeddings:8080`. |
| `DATABASE_DSN` | PostgreSQL local com usuário e senha `postgres` | Conexão psycopg; o Compose injeta as credenciais locais. |
| `AGENT_MODEL` | `nvidia/nemotron-3-ultra-550b-a55b` | Modelo principal. |
| `AGENT_FALLBACK_MODEL` | `nvidia/nemotron-3-super-120b-a12b` | Modelo alternativo. |
| `LLM_MAX_TOKENS` | `512` | Máximo de tokens de saída por chamada de modelo. |
| `LLM_BASE_URL` | `https://integrate.api.nvidia.com/v1` | Endpoint OpenAI-compatible. |
| `LLM_API_KEY` | vazio | Credencial do provedor; necessária para o chat real. |
| `PORTAL_ORIGIN` | `http://localhost:3000` | Única origem aceita por CORS. |

`LLM_MAX_TOKENS` é convertido para inteiro no startup; valor inválido impede a
aplicação de iniciar. As demais configurações não têm validação antecipada.

## 7. Validação e tratamento de erros

- O limite HTTP protege apenas a mensagem externa. As entradas internas das
  ferramentas não têm limites próprios.
- O repositório encapsula erros `psycopg` como `KnowledgeBaseUnavailable`.
- O gateway de embeddings encapsula falhas gRPC e respostas inválidas.
- O endpoint de chat não expõe exceções internas nem prompts na resposta.
- Falhas da ferramenta de busca viram JSON estável em inglês para consumo do LLM.
- Não há timeout explícito para LLM, busca SQL completa ou duração total do chat.
- Não há cancelamento propagado do cliente para LangChain, gRPC ou PostgreSQL.

## 8. Segurança

Comportamento atual:

- não há autenticação, autorização, rate limiting ou quota;
- CORS permite somente a origem configurada, método `POST` e quaisquer headers;
- `X-Correlation-ID` somente é aceito se houver um valor com 1–128 caracteres no
  padrão alfanumérico seguido de alfanuméricos, `.`, `_`, `:` ou `-`;
- header ausente, duplicado ou inválido é substituído por UUID hexadecimal;
- logs de falha registram tipos, contagens, modelos e durações, mas não pergunta,
  resposta, prompt, chave ou mensagem da exceção;
- a chave do LLM deve ser fornecida por segredo de ambiente e nunca versionada;
- conexões gRPC e PostgreSQL da POC não configuram TLS no cliente.

A implementação definitiva deve autenticar o endpoint antes de exposição, aplicar
autorização/limites por consumidor, proteger tráfego e segredos e definir política
de retenção. Instruções encontradas nos documentos recuperados devem ser tratadas
como dados não confiáveis para reduzir prompt injection.

## 9. Observabilidade

Os logs são texto estruturado por pares `chave=valor`, com `CorrelationId`, `Step`,
`Outcome` e, quando aplicável, `ElapsedMs`. Eventos cobrem ciclo de vida, requisição
HTTP, inicialização do agente, invocação, delegação, embedding e busca. A correlação
entra por HTTP, volta na resposta e segue para o gRPC de embeddings.

Health `200` é omitido do log HTTP para reduzir ruído; falhas de health são
registradas. Não existem métricas, tracing distribuído, exportador, dashboard nem
alertas na POC.

## 10. Testes existentes

`python -m unittest test_app.py` cobre:

- contrato gRPC, deadline, correlação, dimensão e ciclo de vida do canal;
- literal pgvector;
- configuração dos modelos e fallback;
- delegação/streaming e remoção de raciocínio, inclusive tags fragmentadas;
- erros estáveis das ferramentas;
- `200`, `422`, `502` e `503` dos endpoints;
- CORS e correlação;
- ausência de conteúdo secreto nos logs de falha;
- independência das camadas `domain` e `application` de frameworks.

Não há teste real com PostgreSQL, serviço de embeddings ou provedor LLM, teste de
carga, contrato de OpenAPI ou avaliação de qualidade/factualidade das respostas.

## 11. Decisões para a implementação definitiva

### Preservar

- busca restrita a versões `Available`;
- rastreabilidade de API, versão e ambiente na evidência entregue ao agente;
- validação da dimensão e finitude dos embeddings;
- resposta em streaming, com falhas anteriores ao primeiro fragmento mapeadas;
- fallback de modelo e separação supervisor / especialista enquanto avaliações
  demonstrarem ganho real;
- ocultação de raciocínio interno, correlação ponta a ponta e logs sem conteúdo
  sensível;
- núcleo de aplicação independente de FastAPI, LangChain e drivers.

### Reavaliar antes de produção

- autenticação, autorização, rate limit, TLS e gestão de segredos;
- provedor/modelos, limite de tokens, prompts e necessidade de dois agentes;
- top 3 global, truncamento em 4.000 caracteres e ausência de score mínimo/filtros;
- políticas contra prompt injection, citações verificáveis e resposta sem evidência;
- timeouts, retries com backoff, cancelamento, circuit breaker e limites de
  concorrência;
- readiness que inclua ou não LLM e inferência real;
- persistência de conversas, contexto multi-turno e feedback, ausentes na POC;
- métricas, traces, avaliações automáticas e testes integrados.
