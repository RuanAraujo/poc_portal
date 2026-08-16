# Graph Report - poc_portal  (2026-08-16)

## Corpus Check
- 106 files · ~82,665 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 676 nodes · 1193 edges · 57 communities (40 shown, 17 thin omitted)
- Extraction: 92% EXTRACTED · 8% INFERRED · 0% AMBIGUOUS · INFERRED: 92 edges (avg confidence: 0.61)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `654f05d0`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- DocumentationVersion
- .ExecuteInTransactionAsync
- IngestionDbContext
- Documentation.Ingestion.Infrastructure.csproj
- DocumentationPublished
- test_app.py
- DocumentationApiClient
- IEmbeddingGenerator
- ApiDocumentation
- RabbitMqIngestionWorker
- DocumentationPortal.sln
- DatabaseInitializer
- .Create
- Documentation.Ingestion.Application.Abstractions
- IProcessedIntegrationEventRepository
- EmbeddingEngine
- Documentation.Infrastructure.csproj
- Documentation.Api
- Especificação — serviço de ingestão de documentações
- ProcessedIntegrationEventRepository
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
- Documentation.Ingestion.Domain.Entities
- RabbitMqIngestionWorker.cs
- Documentation.Ingestion.Worker.csproj
- documentation_agent/__init__.py
- .AddDocumentationIngestionInfrastructure
- Documentation.Application.Abstractions.Persistence
- embeddings_pb2_grpc.py
- .ReplaceForVersionAsync

## God Nodes (most connected - your core abstractions)
1. `AgentTests` - 27 edges
2. `KnowledgeSearchUseCase` - 20 edges
3. `GrpcEmbeddingGateway` - 18 edges
4. `RabbitMqIngestionWorker` - 18 edges
5. `HealthUseCase` - 17 edges
6. `ChatUseCase` - 17 edges
7. `ApiDocumentation` - 17 edges
8. `EmbeddingUnavailable` - 16 edges
9. `KnowledgeBaseUnavailable` - 16 edges
10. `Settings` - 16 edges

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

## Communities (57 total, 17 thin omitted)

### Community 0 - "DocumentationVersion"
Cohesion: 0.09
Nodes (23): CancellationToken, Guid, Task, IDocumentationVersionRepository, CreateDocumentationCommand, DocumentationContent, DocumentationSummary, DocumentationVersionSummary (+15 more)

### Community 1 - ".ExecuteInTransactionAsync"
Cohesion: 0.17
Nodes (8): CancellationToken, Func, Task, IIngestionUnitOfWork, CancellationToken, Func, Task, IngestionUnitOfWork

### Community 2 - "IngestionDbContext"
Cohesion: 0.18
Nodes (10): EntityTypeBuilder, DateTimeOffset, Guid, DocumentChunk, DateTimeOffset, Guid, ProcessedIntegrationEvent, DbSet (+2 more)

### Community 3 - "Documentation.Ingestion.Infrastructure.csproj"
Cohesion: 0.13
Nodes (14): Grpc.Net.ClientFactory (2.80.0), Microsoft.EntityFrameworkCore (10.0.0), Microsoft.Extensions.Configuration.Binder (10.0.0), Microsoft.Extensions.Http (10.0.0), Microsoft.Extensions.Options.ConfigurationExtensions (10.0.0), Pgvector.EntityFrameworkCore (0.2.2), YamlDotNet (16.3.0), net10.0 (+6 more)

### Community 4 - "DocumentationPublished"
Cohesion: 0.11
Nodes (16): ConnectionFactory, Documentation.Contracts, CancellationToken, Task, IDocumentationEventPublisher, string, DocumentationPublished, string (+8 more)

### Community 5 - "test_app.py"
Cohesion: 0.05
Nodes (48): APIRouter, BaseModel, ChatOpenAI, patch, Protocol, create_app(), FastAPI, AgentInvocationFailed (+40 more)

### Community 6 - "DocumentationApiClient"
Cohesion: 0.11
Nodes (19): HttpRequestMessage, HttpResponseMessage, IDeserializer, JsonDocument, JsonElement, IReadOnlyList, IOpenApiChunker, DocumentationContent (+11 more)

### Community 7 - "IEmbeddingGenerator"
Cohesion: 0.20
Nodes (7): CancellationToken, Task, IEmbeddingGenerator, CancellationToken, int, Task, EmbeddingGemmaEmbeddingGenerator

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

### Community 12 - ".Create"
Cohesion: 0.13
Nodes (20): ControllerBase, HttpPost, HttpPut, ActionResult, CancellationToken, Guid, HttpGet, IActionResult (+12 more)

### Community 13 - "Documentation.Ingestion.Application.Abstractions"
Cohesion: 0.16
Nodes (10): Documentation.Ingestion.Infrastructure.OpenApi, Documentation.Ingestion.Domain.ValueObjects, Documentation.Ingestion.Infrastructure.Clients, Documentation.Ingestion.Application.Models, Documentation.Ingestion.Application.Abstractions, Documentation.Ingestion.Infrastructure.Embeddings, Documentation.Ingestion.Application.Services, Documentation.Ingestion.Application.Exceptions (+2 more)

### Community 14 - "IProcessedIntegrationEventRepository"
Cohesion: 0.38
Nodes (4): CancellationToken, Guid, Task, IProcessedIntegrationEventRepository

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

### Community 20 - "ProcessedIntegrationEventRepository"
Cohesion: 0.38
Nodes (5): CancellationToken, Guid, IngestionDbContext, Task, ProcessedIntegrationEventRepository

### Community 21 - ".ProcessAsync"
Cohesion: 0.18
Nodes (11): CancellationToken, Guid, Task, IDocumentationApiClient, DocumentationIndexingStatus, IngestionOutcome, CancellationToken, ILogger (+3 more)

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

### Community 48 - "Documentation.Ingestion.Domain.Entities"
Cohesion: 0.16
Nodes (9): Documentation.Ingestion.Infrastructure.Persistence.Repositories, Documentation.Ingestion.Domain.Entities, IChunkRepository, CancellationToken, Guid, IngestionDbContext, IReadOnlyCollection, Task (+1 more)

### Community 49 - "RabbitMqIngestionWorker.cs"
Cohesion: 0.29
Nodes (3): Documentation.Ingestion.Infrastructure.Persistence, Documentation.Ingestion.Infrastructure, Documentation.Ingestion.Worker

### Community 50 - "Documentation.Ingestion.Worker.csproj"
Cohesion: 0.40
Nodes (4): Microsoft.Extensions.Hosting (10.0.0), Microsoft.NET.Sdk.Worker, net10.0, RabbitMQ.Client (6.8.1)

### Community 55 - ".AddDocumentationIngestionInfrastructure"
Cohesion: 0.50
Nodes (3): IConfiguration, IServiceCollection, DependencyInjection

### Community 56 - "Documentation.Application.Abstractions.Persistence"
Cohesion: 0.06
Nodes (28): Documentation.Infrastructure.Messaging, Documentation.Infrastructure, Documentation.Application.Abstractions.Messaging, Documentation.Application.Abstractions.Persistence, Documentation.Application.Models, Documentation.Api.Controllers, Documentation.Infrastructure.Persistence, Documentation.Domain.Entities (+20 more)

### Community 59 - ".ReplaceForVersionAsync"
Cohesion: 0.40
Nodes (4): CancellationToken, Guid, IReadOnlyCollection, Task

## Knowledge Gaps
- **103 isolated node(s):** `smoke.sh script`, `PublishDocumentationResponse`, `net10.0`, `Swashbuckle.AspNetCore (9.0.6)`, `Microsoft.NET.Sdk.Web` (+98 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **17 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Documentation.Contracts` connect `DocumentationPublished` to `Documentation.Application.Abstractions.Persistence`, `RabbitMqIngestionWorker.cs`, `Documentation.Ingestion.Application.Abstractions`?**
  _High betweenness centrality (0.119) - this node is a cross-community bridge._
- **Why does `IngestionDbContext` connect `IngestionDbContext` to `Documentation.Application.Abstractions.Persistence`, `RabbitMqIngestionWorker.cs`, `DatabaseInitializer`, `.ExecuteInTransactionAsync`?**
  _High betweenness centrality (0.072) - this node is a cross-community bridge._
- **Why does `DatabaseInitializationHostedService` connect `DatabaseInitializer` to `RabbitMqIngestionWorker.cs`?**
  _High betweenness centrality (0.071) - this node is a cross-community bridge._
- **Are the 9 inferred relationships involving `AgentTests` (e.g. with `AgentInvocationFailed` and `EmbeddingUnavailable`) actually correct?**
  _`AgentTests` has 9 INFERRED edges - model-reasoned connections that need verification._
- **Are the 10 inferred relationships involving `KnowledgeSearchUseCase` (e.g. with `AgentGateway` and `EmbeddingGateway`) actually correct?**
  _`KnowledgeSearchUseCase` has 10 INFERRED edges - model-reasoned connections that need verification._
- **Are the 6 inferred relationships involving `GrpcEmbeddingGateway` (e.g. with `EmbeddingUnavailable` and `AgentTests`) actually correct?**
  _`GrpcEmbeddingGateway` has 6 INFERRED edges - model-reasoned connections that need verification._
- **Are the 9 inferred relationships involving `HealthUseCase` (e.g. with `AgentGateway` and `EmbeddingGateway`) actually correct?**
  _`HealthUseCase` has 9 INFERRED edges - model-reasoned connections that need verification._