# Especificação — Documentation Frontend

## 1. Objetivo e responsabilidade

`Documentation.Frontend` é o portal web da POC. Ele oferece, em uma única página:

- chat com o `Documentation.Agent`;
- publicação de especificações OpenAPI na `Documentation.Api`;
- consulta do estado das versões;
- republicação manual de versões que falharam.

O frontend não contém regras de domínio nem persiste estado. É HTML, CSS e
JavaScript nativos servidos pelo Nginx, sem framework, bundler ou dependência de
runtime no navegador.

## 2. Arquitetura e entrega

```text
Navegador
  |-- GET arquivos estáticos ----------------------> Nginx :80
  |-- GET/POST /api/documentations ----------------> Documentation.Api
  `-- POST /api/agents/chat (texto em streaming) --> Documentation.Agent
```

O container usa `nginx:alpine`. No startup, o mecanismo `envsubst` da imagem gera
`config.js` a partir de `config.js.template`. Esse arquivo não recebe cache
(`Cache-Control: no-store`), permitindo trocar URLs sem reconstruir os assets.
Não existe proxy reverso: as URLs configuradas são públicas e acessadas diretamente
pelo navegador, por isso nomes internos do Compose não funcionam nelas.

A porta interna é `80`; o Compose publica `3000` por padrão.

## 3. Estrutura da interface

A página `pt-BR` possui navegação por âncoras e duas seções.

### Chat

- histórico apenas da sessão atual da página;
- textarea obrigatória, máximo de 10.000 caracteres;
- botão de envio e indicador visual “Digitando”;
- resposta incremental em texto puro durante o stream;
- renderização Markdown segura somente após o término;
- duração total apresentada em milissegundos ou segundos.

### Administração

O formulário solicita:

| Campo | Regra do HTML | Contrato enviado |
| --- | --- | --- |
| `apiId` | obrigatório, até 200 caracteres | string |
| `name` | obrigatório, até 300 caracteres | string |
| `version` | obrigatório, até 100 caracteres | string |
| `environment` | obrigatório, até 100 caracteres | string |
| arquivo | opcional; `.json`, `.yaml`, `.yml` e MIME types correspondentes | seu texto preenche `content` |
| `content` | obrigatório, sem limite explícito | string sem espaços externos |
| `format` | inferido | `json` ou `yaml` |

A lista exibe nome, `apiId` e, por versão: versão, ambiente, status, última data
relevante, último erro e ação. A data escolhida é, nesta ordem,
`indexingUpdatedAtUtc`, `publishedAtUtc`, `createdAtUtc`. O botão “Tentar novamente”
aparece somente para `publishFailed` e `indexingFailed`.

O layout passa de duas colunas para uma abaixo de `40rem`; tabelas ganham rolagem
horizontal. A animação de digitação é desativada quando o usuário prefere movimento
reduzido.

## 4. Configuração

`window.PORTAL_CONFIG` contém:

| Campo JavaScript | Variável no container | Fallback no navegador |
| --- | --- | --- |
| `apiUrl` | `PORTAL_API_URL` | `http://localhost:8080` |
| `agentUrl` | `PORTAL_AGENT_URL` | `http://localhost:8090` |

No Compose, essas variáveis recebem `DOCUMENTATION_API_URL` e
`DOCUMENTATION_AGENT_URL`, respectivamente. Uma única `/` final é removida antes
da composição das rotas. Valores de configuração são embutidos em JavaScript; não
devem conter segredos.

## 5. Contratos HTTP consumidos

Todas as chamadas usam `fetch` e enviam `Content-Type: application/json`. Em erro,
o frontend lê o corpo como texto e o apresenta no aviso global. `204` é aceito
como resposta vazia; as demais respostas não brutas são interpretadas como JSON.

### `GET {apiUrl}/api/documentations`

Executado ao carregar a página e ao clicar em “Atualizar lista”. Espera um array:

```json
[
  {
    "id": "uuid",
    "apiId": "payments-api",
    "name": "Payments API",
    "createdAtUtc": "2026-01-01T00:00:00Z",
    "versions": [
      {
        "id": "uuid",
        "version": "1.0.0",
        "environment": "sandbox",
        "format": "yaml",
        "status": "available",
        "lastError": null,
        "createdAtUtc": "2026-01-01T00:00:00Z",
        "publishedAtUtc": "2026-01-01T00:00:01Z",
        "indexingUpdatedAtUtc": "2026-01-01T00:00:02Z"
      }
    ]
  }
]
```

O JSON da API é `camelCase`; enums de status também são strings `camelCase`.

### `POST {apiUrl}/api/documentations`

Envia `apiId`, `name`, `version`, `environment`, `content` e `format`. Sucesso é o
`202 Accepted` da API. Conflitos `409`, falhas de publicação `503` e validações
`400` são tratados genericamente pelo texto da resposta.

Ao publicar com sucesso, o formulário é limpo e a lista é recarregada. O frontend
não aguarda a indexação nem faz polling automático.

### `POST {apiUrl}/api/documentations/{documentId}/versions/{versionId}/republish`

É chamado com os IDs presentes na listagem. Em sucesso, a lista é recarregada. O
botão fica desabilitado durante a chamada.

### `POST {agentUrl}/api/agents/chat`

Envia `{"message":"..."}` e espera `text/plain` em streaming. Se
`ReadableStream` estiver disponível, lê bytes com `TextDecoder` incremental e
atualiza a mensagem. Sem `response.body`, usa `response.text()`.

Resposta vazia é rejeitada no cliente, mesmo com status HTTP de sucesso.

## 6. Fluxos funcionais

### Perguntar ao agente

1. O usuário envia texto não vazio; a mensagem entra no histórico e o campo é
   limpo.
2. O botão é desabilitado e surge o indicador de digitação.
3. Fragmentos recebidos são mostrados como texto, impedindo HTML parcial ativo.
4. Ao final, o texto completo é convertido pelo renderizador Markdown seguro e a
   duração é anexada.
5. Em falha, a mensagem do agente vira aviso genérico e o detalhe HTTP/rede aparece
   na região global de status.

Não há reenvio automático, botão cancelar, timeout, histórico persistido ou
contexto multi-turno: cada chamada contém somente a pergunta atual.

### Publicar documentação

1. Selecionar arquivo valida apenas a extensão e copia seu texto para o textarea.
2. Sem arquivo reconhecido, o formato é inferido pelo primeiro caractere: `{` ou
   `[` significa JSON; qualquer outro conteúdo significa YAML.
3. O navegador aplica os requisitos e limites declarados no HTML.
4. O payload é publicado; em sucesso, a tela informa que a indexação é assíncrona
   e atualiza a listagem.

Não há parsing local de JSON/YAML, limite de tamanho, preview, confirmação ou
proteção contra perda do formulário.

### Acompanhar e tentar novamente

A lista é um snapshot obtido sob demanda. Status de falha habilitam republicação;
os demais são somente leitura. Não há atualização em tempo real nem paginação.

## 7. Renderização e segurança no navegador

Dados da API usados em templates são escapados antes de entrar em `innerHTML`,
inclusive status, erro e IDs visíveis. Mensagens do usuário usam `textContent`.

O renderizador Markdown próprio:

- escapa HTML primeiro;
- suporta títulos, parágrafos, listas simples, blockquotes, blocos e trechos de
  código, negrito, itálico e links;
- transforma apenas links `http://` e `https://`;
- abre links em nova aba com `rel="noopener noreferrer"`;
- não suporta HTML cru, tabelas, listas aninhadas nem a especificação CommonMark
  completa.

Esse comportamento é uma fronteira de segurança e deve permanecer testado. A
implementação definitiva também precisa definir CSP, headers de segurança,
proteção CSRF conforme o modelo de autenticação, dependência ou auditoria do
sanitizador Markdown e política para URLs externas.

A POC não possui autenticação/autorização e expõe ações administrativas a qualquer
usuário que alcance a página e as APIs. Não armazena tokens, cookies ou dados em
`localStorage`/`sessionStorage`.

## 8. Validações e erros

- extensões de arquivo são comparadas sem diferenciar maiúsculas/minúsculas;
- arquivo inválido é limpo e não substitui o conteúdo atual;
- conteúdo é aparado e vazio não é enviado;
- datas inválidas podem provocar erro de formatação no navegador;
- estrutura inesperada da resposta pode falhar durante renderização;
- falhas HTTP mostram o corpo bruto, que no ASP.NET pode ser JSON serializado em
  vez de mensagem amigável;
- erros de rede, CORS, parsing JSON e HTTP compartilham tratamento genérico;
- botões de submit são desabilitados durante chamadas, mas o refresh não possui
  bloqueio e não existe deduplicação de requisições.

O frontend depende da validação autoritativa dos serviços. Restrições HTML não
substituem validação da API.

## 9. Acessibilidade e observabilidade

Recursos atuais incluem idioma da página, labels, navegação nomeada, regiões
`aria-live`, `role=status`, indicação acessível de digitação, foco devolvido ao
campo e respeito a `prefers-reduced-motion`.

Pontos a validar na implementação definitiva: foco após avisos, anúncio de
atualizações extensas da lista/chat, cabeçalhos e legenda da tabela, contraste nos
dois esquemas de cor e navegação completa por teclado.

A UI mede apenas a duração do chat e a mostra ao usuário. Não envia
`X-Correlation-ID`, telemetria, métricas, traces ou erros para backend. O header de
correlação devolvido pelos serviços também não é exibido. Isso limita o diagnóstico
de falhas reportadas por usuários.

## 10. Testes existentes

`node test/helpers.test.js` usa somente `node:assert` e cobre:

- estados que permitem republicação;
- inferência de formato por nome e conteúdo;
- recursos básicos do Markdown;
- escape de HTML e recusa de link `javascript:`;
- formatação de duração em `pt-BR`.

Não existem testes do DOM, fluxos `fetch`, streaming multibyte, formulários,
acessibilidade, responsividade, Nginx/configuração ou integração real com API e
agente.

## 11. Decisões para a implementação definitiva

### Preservar

- configuração de URLs em runtime e ausência de segredos no bundle;
- feedback imediato durante streaming e conteúdo parcial tratado como texto;
- escape de todo dado externo e restrição de links do Markdown;
- republicação limitada aos estados recuperáveis definidos pelo backend;
- validações básicas alinhadas aos limites da API;
- fundamentos de acessibilidade e movimento reduzido;
- tecnologia estática sem framework enquanto os fluxos permanecerem deste porte.

### Reavaliar antes de produção

- separação entre experiência pública de consulta e administração autenticada;
- autenticação, autorização, CSRF/CSP e headers de segurança;
- proxy same-origin versus CORS e URLs públicas separadas;
- polling, eventos ou atualização manual para acompanhar indexação;
- paginação, busca e ordenação quando o catálogo crescer;
- parsing/validação e limite de tamanho do arquivo antes do upload;
- mensagens de erro tipadas, correlação visível e telemetria;
- cancelamento/timeout do chat, retry explícito e preservação do formulário;
- biblioteca Markdown auditada somente se a sintaxe exigida superar o parser
  mínimo atual;
- testes de browser e acessibilidade para os fluxos críticos.
