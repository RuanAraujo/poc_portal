# Graph Report - poc_portal  (2026-08-16)

## Corpus Check
- 106 files · ~82,152 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 667 nodes · 1169 edges · 63 communities (45 shown, 18 thin omitted)
- Extraction: 92% EXTRACTED · 8% INFERRED · 0% AMBIGUOUS · INFERRED: 89 edges (avg confidence: 0.61)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `3ca7ad79`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .PublishVersionAsync
- DocumentationVersion
- IngestionDbContext
- Documentation.Ingestion.Infrastructure.csproj
- IDocumentationVersionRepository
- test_app.py
- DocumentationApiClient
- Documentation.Domain.Enums
- ApiDocumentation
- RabbitMqIngestionWorker
- DocumentationPortal.sln
- DatabaseInitializer
- .Create
- Documentation.Ingestion.Application.Abstractions
- Documentation.Application.Abstractions.Persistence
- EmbeddingEngine
- Documentation.Infrastructure.csproj
- Documentation.Api
- Especificação — serviço de ingestão de documentações
- DocumentationPublished
- ProcessedIntegrationEventRepository
- .ProcessAsync
- Documentation API
- Documentation.Contracts.csproj
- smoke.sh
- Documentation.Embeddings.csproj
- requirements file
- Documentation Portal POC
- DocumentationVersionRepository
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
- Documentation.Ingestion.Domain.Entities
- Documentation.Ingestion.Infrastructure.Persistence
- Documentation.Ingestion.Worker.csproj
- documentation_agent/__init__.py
- .AddDocumentationIngestionInfrastructure
- Documentation.Infrastructure/DependencyInjection.cs
- DocumentationDbContext
- embeddings.py
- .ReplaceForVersionAsync
- ProcessedIntegrationEvent
- .ReplaceForVersionAsync
- PermanentIngestionException.cs

## God Nodes (most connected - your core abstractions)
1. `AgentTests` - 22 edges
2. `KnowledgeSearchUseCase` - 19 edges
3. `HealthUseCase` - 18 edges
4. `ChatUseCase` - 18 edges
5. `GrpcEmbeddingGateway` - 18 edges
6. `RabbitMqIngestionWorker` - 18 edges
7. `EmbeddingUnavailable` - 17 edges
8. `KnowledgeBaseUnavailable` - 17 edges
9. `ApiDocumentation` - 17 edges
10. `DocumentationVersion` - 16 edges

## Surprising Connections (you probably didn't know these)
- `GrpcEmbeddingGateway` --uses--> `EmbeddingUnavailable`  [INFERRED]
  src/Documentation.Agent/documentation_agent/infrastructure/embeddings.py → src/Documentation.Agent/documentation_agent/application/errors.py
- `ChatRequest` --uses--> `EmbeddingUnavailable`  [INFERRED]
  src/Documentation.Agent/documentation_agent/interface_adapters/http.py → src/Documentation.Agent/documentation_agent/application/errors.py
- `ChatResponse` --uses--> `EmbeddingUnavailable`  [INFERRED]
  src/Documentation.Agent/documentation_agent/interface_adapters/http.py → src/Documentation.Agent/documentation_agent/application/errors.py
- `AgentTests` --uses--> `EmbeddingUnavailable`  [INFERRED]
  src/Documentation.Agent/test_app.py → src/Documentation.Agent/documentation_agent/application/errors.py
- `FailingAgent` --uses--> `EmbeddingUnavailable`  [INFERRED]
  src/Documentation.Agent/test_app.py → src/Documentation.Agent/documentation_agent/application/errors.py

## Import Cycles
- None detected.

## Communities (63 total, 18 thin omitted)

### Community 0 - ".PublishVersionAsync"
Cohesion: 0.23
Nodes (11): CreateDocumentationCommand, DocumentationContent, DocumentationSummary, DocumentationVersionSummary, PublishAttemptResult, PublishAttemptResultKind, CancellationToken, Guid (+3 more)

### Community 1 - "DocumentationVersion"
Cohesion: 0.24
Nodes (4): DateTimeOffset, Guid, DocumentationVersion, DocumentationVersionStatus

### Community 2 - "IngestionDbContext"
Cohesion: 0.27
Nodes (7): EntityTypeBuilder, DateTimeOffset, Guid, DocumentChunk, DbSet, ModelBuilder, IngestionDbContext

### Community 3 - "Documentation.Ingestion.Infrastructure.csproj"
Cohesion: 0.13
Nodes (14): Grpc.Net.ClientFactory (2.80.0), Microsoft.EntityFrameworkCore (10.0.0), Microsoft.Extensions.Configuration.Binder (10.0.0), Microsoft.Extensions.Http (10.0.0), Microsoft.Extensions.Options.ConfigurationExtensions (10.0.0), Pgvector.EntityFrameworkCore (0.2.2), YamlDotNet (16.3.0), net10.0 (+6 more)

### Community 4 - "IDocumentationVersionRepository"
Cohesion: 0.43
Nodes (4): CancellationToken, Guid, Task, IDocumentationVersionRepository

### Community 5 - "test_app.py"
Cohesion: 0.06
Nodes (46): APIRouter, BaseModel, ChatOpenAI, patch, Protocol, create_app(), FastAPI, AgentInvocationFailed (+38 more)

### Community 6 - "DocumentationApiClient"
Cohesion: 0.09
Nodes (20): HttpRequestMessage, HttpResponseMessage, IDeserializer, JsonDocument, JsonElement, IReadOnlyList, IOpenApiChunker, DocumentationContent (+12 more)

### Community 7 - "Documentation.Domain.Enums"
Cohesion: 0.33
Nodes (6): Documentation.Application.Models, Documentation.Api.Controllers, Documentation.Api.Contracts, Documentation.Domain.Enums, Documentation.Application.Services, PublishDocumentationResponse

### Community 8 - "ApiDocumentation"
Cohesion: 0.14
Nodes (14): ICollection, CancellationToken, Guid, IReadOnlyList, Task, IApiDocumentationRepository, DateTimeOffset, Guid (+6 more)

### Community 9 - "RabbitMqIngestionWorker"
Cohesion: 0.11
Nodes (18): BackgroundService, BasicDeliverEventArgs, Documentation.Ingestion.Infrastructure.Options, IDictionary, IModel, string, DocumentationApiOptions, string (+10 more)

### Community 10 - "DocumentationPortal.sln"
Cohesion: 0.25
Nodes (5): net10.0, Microsoft.Extensions.Logging.Abstractions (10.0.0), Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk

### Community 11 - "DatabaseInitializer"
Cohesion: 0.17
Nodes (12): IHostedService, CancellationToken, ILogger, IServiceScopeFactory, string, Task, DatabaseInitializer, CancellationToken (+4 more)

### Community 12 - ".Create"
Cohesion: 0.12
Nodes (22): ControllerBase, HttpPost, HttpPut, CreateDocumentationRequest, UpdateIndexingStatusRequest, ActionResult, CancellationToken, Guid (+14 more)

### Community 13 - "Documentation.Ingestion.Application.Abstractions"
Cohesion: 0.23
Nodes (8): Documentation.Ingestion.Infrastructure.OpenApi, Documentation.Ingestion.Domain.ValueObjects, Documentation.Ingestion.Infrastructure.Clients, Documentation.Ingestion.Application.Models, Documentation.Ingestion.Application.Abstractions, Documentation.Ingestion.Infrastructure.Embeddings, Documentation.Ingestion.Application.Services, Documentation.Ingestion.Application.Exceptions

### Community 14 - "Documentation.Application.Abstractions.Persistence"
Cohesion: 0.21
Nodes (6): Documentation.Application.Abstractions.Persistence, Documentation.Domain.Entities, Documentation.Infrastructure.Persistence.Repositories, CancellationToken, Task, IUnitOfWork

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

### Community 19 - "DocumentationPublished"
Cohesion: 0.11
Nodes (16): ConnectionFactory, Documentation.Contracts, CancellationToken, Task, IDocumentationEventPublisher, string, DocumentationPublished, string (+8 more)

### Community 20 - "ProcessedIntegrationEventRepository"
Cohesion: 0.38
Nodes (5): CancellationToken, Guid, IngestionDbContext, Task, ProcessedIntegrationEventRepository

### Community 21 - ".ProcessAsync"
Cohesion: 0.07
Nodes (25): CancellationToken, Guid, Task, IDocumentationApiClient, CancellationToken, Task, IEmbeddingGenerator, CancellationToken (+17 more)

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

### Community 28 - "DocumentationVersionRepository"
Cohesion: 0.53
Nodes (4): CancellationToken, Guid, Task, DocumentationVersionRepository

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

### Community 48 - "Documentation.Ingestion.Domain.Entities"
Cohesion: 0.28
Nodes (5): Documentation.Ingestion.Infrastructure.Persistence.Repositories, Documentation.Ingestion.Domain.Entities, IChunkRepository, IngestionDbContext, ChunkRepository

### Community 49 - "Documentation.Ingestion.Infrastructure.Persistence"
Cohesion: 0.15
Nodes (7): Documentation.Ingestion.Infrastructure.Persistence, Documentation.Ingestion.Infrastructure, Documentation.Ingestion.Worker, CancellationToken, Func, Task, IngestionUnitOfWork

### Community 50 - "Documentation.Ingestion.Worker.csproj"
Cohesion: 0.40
Nodes (4): Microsoft.Extensions.Hosting (10.0.0), Microsoft.NET.Sdk.Worker, net10.0, RabbitMQ.Client (6.8.1)

### Community 55 - ".AddDocumentationIngestionInfrastructure"
Cohesion: 0.50
Nodes (3): IConfiguration, IServiceCollection, DependencyInjection

### Community 56 - "Documentation.Infrastructure/DependencyInjection.cs"
Cohesion: 0.15
Nodes (10): Documentation.Infrastructure.Messaging, Documentation.Infrastructure, Documentation.Application.Abstractions.Messaging, Documentation.Infrastructure.Persistence, Program, IConfiguration, IServiceCollection, DependencyInjection (+2 more)

### Community 57 - "DocumentationDbContext"
Cohesion: 0.40
Nodes (4): DbContext, DbSet, ModelBuilder, DocumentationDbContext

### Community 59 - ".ReplaceForVersionAsync"
Cohesion: 0.40
Nodes (4): CancellationToken, Guid, IReadOnlyCollection, Task

### Community 60 - "ProcessedIntegrationEvent"
Cohesion: 0.50
Nodes (3): DateTimeOffset, Guid, ProcessedIntegrationEvent

### Community 61 - ".ReplaceForVersionAsync"
Cohesion: 0.40
Nodes (4): CancellationToken, Guid, IReadOnlyCollection, Task

## Knowledge Gaps
- **103 isolated node(s):** `smoke.sh script`, `PublishDocumentationResponse`, `net10.0`, `Swashbuckle.AspNetCore (9.0.6)`, `Microsoft.NET.Sdk.Web` (+98 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **18 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Documentation.Contracts` connect `DocumentationPublished` to `Documentation.Infrastructure/DependencyInjection.cs`, `Documentation.Ingestion.Application.Abstractions`, `Documentation.Domain.Enums`?**
  _High betweenness centrality (0.122) - this node is a cross-community bridge._
- **Why does `IngestionDbContext` connect `IngestionDbContext` to `Documentation.Ingestion.Infrastructure.Persistence`, `DatabaseInitializer`, `ProcessedIntegrationEvent`, `DocumentationDbContext`?**
  _High betweenness centrality (0.074) - this node is a cross-community bridge._
- **Why does `DatabaseInitializationHostedService` connect `DatabaseInitializer` to `Documentation.Ingestion.Infrastructure.Persistence`?**
  _High betweenness centrality (0.073) - this node is a cross-community bridge._
- **Are the 8 inferred relationships involving `AgentTests` (e.g. with `AgentInvocationFailed` and `EmbeddingUnavailable`) actually correct?**
  _`AgentTests` has 8 INFERRED edges - model-reasoned connections that need verification._
- **Are the 9 inferred relationships involving `KnowledgeSearchUseCase` (e.g. with `AgentGateway` and `EmbeddingGateway`) actually correct?**
  _`KnowledgeSearchUseCase` has 9 INFERRED edges - model-reasoned connections that need verification._
- **Are the 10 inferred relationships involving `HealthUseCase` (e.g. with `AgentGateway` and `EmbeddingGateway`) actually correct?**
  _`HealthUseCase` has 10 INFERRED edges - model-reasoned connections that need verification._
- **Are the 10 inferred relationships involving `ChatUseCase` (e.g. with `AgentGateway` and `EmbeddingGateway`) actually correct?**
  _`ChatUseCase` has 10 INFERRED edges - model-reasoned connections that need verification._