# Graph Report - poc_portal  (2026-08-16)

## Corpus Check
- 106 files · ~82,747 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 677 nodes · 1199 edges · 55 communities (38 shown, 17 thin omitted)
- Extraction: 92% EXTRACTED · 8% INFERRED · 0% AMBIGUOUS · INFERRED: 92 edges (avg confidence: 0.61)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `654f05d0`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .PublishVersionAsync
- .ExecuteInTransactionAsync
- IngestionDbContext
- Documentation.Ingestion.Infrastructure.csproj
- DocumentationPublished
- test_app.py
- OpenApiChunker
- IEmbeddingGenerator
- ApiDocumentation
- RabbitMqIngestionWorker
- DocumentationPortal.sln
- DatabaseInitializer
- .GetContent
- Documentation.Ingestion.Application.Models
- Documentation.Ingestion.Application.Exceptions
- EmbeddingEngine
- Documentation.Infrastructure.csproj
- Documentation.Api
- Especificação — serviço de ingestão de documentações
- .ProcessAsync
- Documentation API
- Documentation.Contracts.csproj
- smoke.sh
- Documentation.Embeddings.csproj
- requirements file
- Documentation Portal POC
- Application layer
- Ingestion worker
- AGENTS.md
- Documentation Agent service
- Documentation API service
- Documentation Ingestion service
- Hugging Face cache volume
- PostgreSQL service
- PostgreSQL data volume
- RabbitMQ service
- RabbitMQ data volume
- DocumentationPublished event
- Agent
- API Service
- Ingestion Worker
- PostgreSQL
- helpers.test.js
- Documentation.Ingestion.Application.csproj
- Documentation.Ingestion.Application.Abstractions
- Documentation.Ingestion.Infrastructure/DependencyInjection.cs
- Documentation.Ingestion.Worker.csproj
- documentation_agent/__init__.py
- .AddDocumentationIngestionInfrastructure
- DocumentationVersion
- embeddings.py

## God Nodes (most connected - your core abstractions)
1. `AgentTests` - 28 edges
2. `KnowledgeSearchUseCase` - 20 edges
3. `GrpcEmbeddingGateway` - 18 edges
4. `RabbitMqIngestionWorker` - 18 edges
5. `HealthUseCase` - 17 edges
6. `ChatUseCase` - 17 edges
7. `Settings` - 17 edges
8. `ApiDocumentation` - 17 edges
9. `EmbeddingUnavailable` - 16 edges
10. `KnowledgeBaseUnavailable` - 16 edges

## Surprising Connections (you probably didn't know these)
- `GrpcEmbeddingGateway` --uses--> `EmbeddingUnavailable`  [INFERRED]
  src/Documentation.Agent/documentation_agent/infrastructure/embeddings.py → src/Documentation.Agent/documentation_agent/application/errors.py
- `ChatRequest` --uses--> `EmbeddingUnavailable`  [INFERRED]
  src/Documentation.Agent/documentation_agent/interface_adapters/http.py → src/Documentation.Agent/documentation_agent/application/errors.py
- `AgentTests` --uses--> `EmbeddingUnavailable`  [INFERRED]
  src/Documentation.Agent/test_app.py → src/Documentation.Agent/documentation_agent/application/errors.py
- `FailingAgent` --uses--> `EmbeddingUnavailable`  [INFERRED]
  src/Documentation.Agent/test_app.py → src/Documentation.Agent/documentation_agent/application/errors.py
- `HealthyEmbeddings` --uses--> `EmbeddingUnavailable`  [INFERRED]
  src/Documentation.Agent/test_app.py → src/Documentation.Agent/documentation_agent/application/errors.py

## Import Cycles
- None detected.

## Communities (55 total, 17 thin omitted)

### Community 0 - ".PublishVersionAsync"
Cohesion: 0.13
Nodes (21): HttpPost, ActionResult, CancellationToken, Guid, HttpGet, IActionResult, IReadOnlyList, ProducesResponseType (+13 more)

### Community 1 - ".ExecuteInTransactionAsync"
Cohesion: 0.50
Nodes (3): CancellationToken, Func, Task

### Community 2 - "IngestionDbContext"
Cohesion: 0.08
Nodes (23): EntityTypeBuilder, CancellationToken, Guid, IReadOnlyCollection, Task, DateTimeOffset, Guid, DocumentChunk (+15 more)

### Community 3 - "Documentation.Ingestion.Infrastructure.csproj"
Cohesion: 0.13
Nodes (14): Grpc.Net.ClientFactory (2.80.0), Microsoft.EntityFrameworkCore (10.0.0), Microsoft.Extensions.Configuration.Binder (10.0.0), Microsoft.Extensions.Http (10.0.0), Microsoft.Extensions.Options.ConfigurationExtensions (10.0.0), Pgvector.EntityFrameworkCore (0.2.2), YamlDotNet (16.3.0), net10.0 (+6 more)

### Community 4 - "DocumentationPublished"
Cohesion: 0.07
Nodes (23): ConnectionFactory, Documentation.Infrastructure.Messaging, Documentation.Application.Abstractions.Messaging, Documentation.Contracts, CancellationToken, Task, IDocumentationEventPublisher, string (+15 more)

### Community 5 - "test_app.py"
Cohesion: 0.05
Nodes (48): APIRouter, BaseModel, ChatOpenAI, patch, Protocol, create_app(), FastAPI, AgentInvocationFailed (+40 more)

### Community 6 - "OpenApiChunker"
Cohesion: 0.30
Nodes (7): IDeserializer, JsonDocument, JsonElement, DocumentationContent, IReadOnlyList, string, OpenApiChunker

### Community 7 - "IEmbeddingGenerator"
Cohesion: 0.22
Nodes (6): Documentation.Ingestion.Infrastructure.Embeddings, IEmbeddingGenerator, CancellationToken, int, Task, EmbeddingGemmaEmbeddingGenerator

### Community 8 - "ApiDocumentation"
Cohesion: 0.15
Nodes (14): ICollection, CancellationToken, Guid, IReadOnlyList, Task, IApiDocumentationRepository, DateTimeOffset, Guid (+6 more)

### Community 9 - "RabbitMqIngestionWorker"
Cohesion: 0.11
Nodes (18): BackgroundService, BasicDeliverEventArgs, Documentation.Ingestion.Infrastructure.Options, IDictionary, IModel, string, DocumentationApiOptions, string (+10 more)

### Community 10 - "DocumentationPortal.sln"
Cohesion: 0.25
Nodes (5): net10.0, Microsoft.Extensions.Logging.Abstractions (10.0.0), Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk

### Community 11 - "DatabaseInitializer"
Cohesion: 0.16
Nodes (12): IHostedService, CancellationToken, ILogger, IServiceScopeFactory, string, Task, DatabaseInitializer, CancellationToken (+4 more)

### Community 12 - ".GetContent"
Cohesion: 0.23
Nodes (10): ControllerBase, HttpPut, ActionResult, CancellationToken, Guid, HttpGet, IActionResult, ProducesResponseType (+2 more)

### Community 13 - "Documentation.Ingestion.Application.Models"
Cohesion: 0.20
Nodes (5): Documentation.Ingestion.Domain.ValueObjects, Documentation.Ingestion.Application.Models, IReadOnlyList, IOpenApiChunker, DocumentChunkDraft

### Community 14 - "Documentation.Ingestion.Application.Exceptions"
Cohesion: 0.33
Nodes (4): Documentation.Ingestion.Infrastructure.Clients, Documentation.Ingestion.Application.Exceptions, Exception, PermanentIngestionException

### Community 15 - "EmbeddingEngine"
Cohesion: 0.09
Nodes (23): DenseTensor, EmbeddingServiceBase, EmbedRequest, EmbedResponse, HealthCheckContext, HealthCheckResult, IDisposable, IHealthCheck (+15 more)

### Community 16 - "Documentation.Infrastructure.csproj"
Cohesion: 0.25
Nodes (7): Microsoft.Extensions.Configuration.Abstractions (10.0.0), net10.0, Microsoft.Extensions.DependencyInjection.Abstractions (10.0.0), Microsoft.Extensions.Logging.Abstractions (10.0.0), Npgsql.EntityFrameworkCore.PostgreSQL (10.0.0), RabbitMQ.Client (6.8.1), Microsoft.NET.Sdk

### Community 17 - "Documentation.Api"
Cohesion: 0.18
Nodes (10): applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, launchUrl, ASPNETCORE_ENVIRONMENT, profiles (+2 more)

### Community 18 - "Especificação — serviço de ingestão de documentações"
Cohesion: 0.17
Nodes (11): Configuração, Contrato de mensageria, Contrato HTTP esperado da API, Embeddings, Especificação — serviço de ingestão de documentações, Fluxo de processamento, Inicialização local, Limitações deliberadas da POC (+3 more)

### Community 21 - ".ProcessAsync"
Cohesion: 0.07
Nodes (25): HttpRequestMessage, HttpResponseMessage, CancellationToken, Guid, Task, IDocumentationApiClient, CancellationToken, Task (+17 more)

### Community 22 - "Documentation API"
Cohesion: 0.22
Nodes (8): Arquitetura, Configuração, Contrato HTTP, Documentation API, Limites da POC, Mensageria, Modelo de persistência, Responsabilidade

### Community 23 - "Documentation.Contracts.csproj"
Cohesion: 0.29
Nodes (5): Swashbuckle.AspNetCore (9.0.6), net10.0, Microsoft.NET.Sdk.Web, net10.0, Microsoft.NET.Sdk

### Community 25 - "Documentation.Embeddings.csproj"
Cohesion: 0.25
Nodes (7): Grpc.AspNetCore (2.80.0), Microsoft.ML.OnnxRuntime (1.28.0), Microsoft.ML.Tokenizers (2.0.0), net10.0, Google.Protobuf (3.35.1), Grpc.Tools (2.83.0), Microsoft.NET.Sdk.Web

### Community 26 - "requirements file"
Cohesion: 0.25
Nodes (8): fastapi, langchain, langchain-openai, psycopg[binary], requirements file, sentence-transformers, torch, uvicorn

### Community 27 - "Documentation Portal POC"
Cohesion: 0.25
Nodes (7): Agente de integração, Componentes, Desenvolvimento, Documentation Portal POC, Embeddings, Executar localmente, Limites da POC

### Community 29 - "Application layer"
Cohesion: 0.67
Nodes (4): Application layer, Documentation.Api service, Domain layer, Infrastructure layer

### Community 30 - "Ingestion worker"
Cohesion: 0.67
Nodes (3): Embedding service, Ingestion worker, RabbitMQ

### Community 45 - "helpers.test.js"
Cohesion: 0.29
Nodes (10): escapeHtml(), formatDuration(), formatFromContent(), formatFromFile(), inlineMarkdown(), isRetryableStatus(), renderMarkdown(), assert (+2 more)

### Community 47 - "Documentation.Ingestion.Application.csproj"
Cohesion: 0.29
Nodes (5): net10.0, Microsoft.Extensions.Logging.Abstractions (10.0.0), Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk

### Community 48 - "Documentation.Ingestion.Application.Abstractions"
Cohesion: 0.15
Nodes (12): Documentation.Ingestion.Infrastructure.Persistence.Repositories, Documentation.Ingestion.Application.Abstractions, Documentation.Ingestion.Domain.Entities, IChunkRepository, IIngestionUnitOfWork, IProcessedIntegrationEventRepository, ILogger, int (+4 more)

### Community 49 - "Documentation.Ingestion.Infrastructure/DependencyInjection.cs"
Cohesion: 0.22
Nodes (5): Documentation.Ingestion.Infrastructure.OpenApi, Documentation.Ingestion.Infrastructure.Persistence, Documentation.Ingestion.Application.Services, Documentation.Ingestion.Infrastructure, Documentation.Ingestion.Worker

### Community 50 - "Documentation.Ingestion.Worker.csproj"
Cohesion: 0.40
Nodes (4): Microsoft.Extensions.Hosting (10.0.0), Microsoft.NET.Sdk.Worker, net10.0, RabbitMQ.Client (6.8.1)

### Community 55 - ".AddDocumentationIngestionInfrastructure"
Cohesion: 0.50
Nodes (3): IConfiguration, IServiceCollection, DependencyInjection

### Community 56 - "DocumentationVersion"
Cohesion: 0.05
Nodes (33): Documentation.Infrastructure, Documentation.Application.Abstractions.Persistence, Documentation.Application.Models, Documentation.Api.Controllers, Documentation.Infrastructure.Persistence, Documentation.Domain.Entities, Documentation.Api.Contracts, Documentation.Domain.Enums (+25 more)

## Knowledge Gaps
- **103 isolated node(s):** `smoke.sh script`, `PublishDocumentationResponse`, `net10.0`, `Swashbuckle.AspNetCore (9.0.6)`, `Microsoft.NET.Sdk.Web` (+98 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **17 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Documentation.Contracts` connect `DocumentationPublished` to `DocumentationVersion`, `Documentation.Ingestion.Infrastructure/DependencyInjection.cs`?**
  _High betweenness centrality (0.119) - this node is a cross-community bridge._
- **Why does `IngestionDbContext` connect `IngestionDbContext` to `DocumentationVersion`, `Documentation.Ingestion.Infrastructure/DependencyInjection.cs`, `Documentation.Ingestion.Application.Abstractions`, `DatabaseInitializer`?**
  _High betweenness centrality (0.072) - this node is a cross-community bridge._
- **Why does `DatabaseInitializationHostedService` connect `DatabaseInitializer` to `Documentation.Ingestion.Infrastructure/DependencyInjection.cs`?**
  _High betweenness centrality (0.071) - this node is a cross-community bridge._
- **Are the 9 inferred relationships involving `AgentTests` (e.g. with `AgentInvocationFailed` and `EmbeddingUnavailable`) actually correct?**
  _`AgentTests` has 9 INFERRED edges - model-reasoned connections that need verification._
- **Are the 10 inferred relationships involving `KnowledgeSearchUseCase` (e.g. with `AgentGateway` and `EmbeddingGateway`) actually correct?**
  _`KnowledgeSearchUseCase` has 10 INFERRED edges - model-reasoned connections that need verification._
- **Are the 6 inferred relationships involving `GrpcEmbeddingGateway` (e.g. with `EmbeddingUnavailable` and `AgentTests`) actually correct?**
  _`GrpcEmbeddingGateway` has 6 INFERRED edges - model-reasoned connections that need verification._
- **Are the 9 inferred relationships involving `HealthUseCase` (e.g. with `AgentGateway` and `EmbeddingGateway`) actually correct?**
  _`HealthUseCase` has 9 INFERRED edges - model-reasoned connections that need verification._