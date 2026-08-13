# Graph Report - poc_portal  (2026-08-10)

## Corpus Check
- 82 files · ~78,004 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 574 nodes · 886 edges · 52 communities (37 shown, 15 thin omitted)
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 28 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `a463f1a2`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Documentation.Application.Abstractions.Persistence
- .ProcessAsync
- DocumentChunk
- Documentation.Ingestion.Infrastructure.csproj
- .PublishVersionAsync
- app.py
- OpenApiChunker
- DocumentationVersion
- ApiDocumentation
- RabbitMqIngestionWorker
- AgentTests
- DatabaseInitializer
- .Create
- Documentation.Ingestion.Infrastructure/DependencyInjection.cs
- Documentation.Ingestion.Infrastructure.Persistence
- EmbeddingEngine
- Documentation.Ingestion.Application.Abstractions
- Documentation.Api
- Especificação — serviço de ingestão de documentações
- Documentation.Infrastructure.csproj
- .GetContent
- Documentation.Contracts.csproj
- Documentation API
- DocumentationPortal.sln
- smoke.sh
- Documentation.Embeddings.csproj
- requirements file
- Documentation Portal POC
- Documentation.Ingestion.Worker.csproj
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
- IEmbeddingGenerator
- IngestionDbContext
- .ExecuteInTransactionAsync
- ProcessedIntegrationEventRepository
- IProcessedIntegrationEventRepository
- ProcessedIntegrationEvent
- .ReplaceForVersionAsync

## God Nodes (most connected - your core abstractions)
1. `ApiDocumentation` - 17 edges
2. `DocumentationVersion` - 16 edges
3. `RabbitMqIngestionWorker` - 16 edges
4. `EmbeddingEngine` - 15 edges
5. `Documentation.Ingestion.Application.Abstractions` - 14 edges
6. `AgentTests` - 11 edges
7. `DocumentationIngestionService` - 11 edges
8. `Especificação — serviço de ingestão de documentações` - 11 edges
9. `IngestionDbContext` - 10 edges
10. `DocumentationApplicationService` - 9 edges

## Surprising Connections (you probably didn't know these)
- `ApiDocumentation` --references--> `DocumentationVersion`  [EXTRACTED]
  src/Documentation.Domain/Entities/ApiDocumentation.cs → src/Documentation.Domain/Entities/DocumentationVersion.cs
- `DocumentationDbContext` --references--> `ApiDocumentation`  [EXTRACTED]
  src/Documentation.Infrastructure/Persistence/DocumentationDbContext.cs → src/Documentation.Domain/Entities/ApiDocumentation.cs
- `DocumentationIngestionService` --references--> `IChunkRepository`  [EXTRACTED]
  src/Documentation.Ingestion.Application/Services/DocumentationIngestionService.cs → src/Documentation.Ingestion.Application/Abstractions/IChunkRepository.cs
- `DocumentationApiClient` --implements--> `IDocumentationApiClient`  [EXTRACTED]
  src/Documentation.Ingestion.Infrastructure/Clients/DocumentationApiClient.cs → src/Documentation.Ingestion.Application/Abstractions/IDocumentationApiClient.cs
- `DocumentationIngestionService` --references--> `IEmbeddingGenerator`  [EXTRACTED]
  src/Documentation.Ingestion.Application/Services/DocumentationIngestionService.cs → src/Documentation.Ingestion.Application/Abstractions/IEmbeddingGenerator.cs

## Import Cycles
- None detected.

## Communities (52 total, 15 thin omitted)

### Community 0 - "Documentation.Application.Abstractions.Persistence"
Cohesion: 0.08
Nodes (21): Documentation.Infrastructure.Messaging, Documentation.Infrastructure, Documentation.Application.Abstractions.Messaging, Documentation.Application.Abstractions.Persistence, Documentation.Application.Models, Documentation.Api.Controllers, Documentation.Infrastructure.Persistence, Documentation.Domain.Entities (+13 more)

### Community 1 - ".ProcessAsync"
Cohesion: 0.06
Nodes (28): ConnectionFactory, Documentation.Ingestion.Application.Models, Documentation.Contracts, Documentation.Ingestion.Application.Services, CancellationToken, Task, IDocumentationEventPublisher, string (+20 more)

### Community 2 - "DocumentChunk"
Cohesion: 0.22
Nodes (7): CancellationToken, Guid, IReadOnlyCollection, Task, DateTimeOffset, Guid, DocumentChunk

### Community 3 - "Documentation.Ingestion.Infrastructure.csproj"
Cohesion: 0.13
Nodes (14): Grpc.Net.ClientFactory (2.80.0), Microsoft.EntityFrameworkCore (10.0.0), Microsoft.Extensions.Configuration.Binder (10.0.0), Microsoft.Extensions.Http (10.0.0), Microsoft.Extensions.Logging.Abstractions (10.0.0), Microsoft.Extensions.Options.ConfigurationExtensions (10.0.0), Pgvector.EntityFrameworkCore (0.2.2), YamlDotNet (16.3.0) (+6 more)

### Community 4 - ".PublishVersionAsync"
Cohesion: 0.23
Nodes (11): CreateDocumentationCommand, DocumentationContent, DocumentationSummary, DocumentationVersionSummary, PublishAttemptResult, PublishAttemptResultKind, CancellationToken, Guid (+3 more)

### Community 5 - "app.py"
Cohesion: 0.10
Nodes (24): Any, BaseModel, ChatOpenAI, FastAPI, get, post, chat(), ChatRequest (+16 more)

### Community 6 - "OpenApiChunker"
Cohesion: 0.16
Nodes (12): Documentation.Ingestion.Infrastructure.OpenApi, Documentation.Ingestion.Domain.ValueObjects, IDeserializer, JsonDocument, JsonElement, IReadOnlyList, IOpenApiChunker, DocumentationContent (+4 more)

### Community 7 - "DocumentationVersion"
Cohesion: 0.09
Nodes (19): DbContext, CancellationToken, Guid, Task, IDocumentationVersionRepository, CancellationToken, Task, IUnitOfWork (+11 more)

### Community 8 - "ApiDocumentation"
Cohesion: 0.14
Nodes (14): ICollection, CancellationToken, Guid, IReadOnlyList, Task, IApiDocumentationRepository, DateTimeOffset, Guid (+6 more)

### Community 9 - "RabbitMqIngestionWorker"
Cohesion: 0.22
Nodes (11): BackgroundService, BasicDeliverEventArgs, IDictionary, IModel, CancellationToken, ILogger, IServiceScopeFactory, JsonSerializerOptions (+3 more)

### Community 10 - "AgentTests"
Cohesion: 0.15
Nodes (3): object, AgentTests, FailingSupervisor

### Community 11 - "DatabaseInitializer"
Cohesion: 0.13
Nodes (14): Documentation.Ingestion.Infrastructure, Documentation.Ingestion.Worker, IHostedService, CancellationToken, ILogger, IServiceScopeFactory, string, Task (+6 more)

### Community 12 - ".Create"
Cohesion: 0.30
Nodes (10): HttpPost, ActionResult, CancellationToken, Guid, HttpGet, IActionResult, IReadOnlyList, ProducesResponseType (+2 more)

### Community 13 - "Documentation.Ingestion.Infrastructure/DependencyInjection.cs"
Cohesion: 0.07
Nodes (23): Documentation.Ingestion.Infrastructure.Options, Documentation.Ingestion.Infrastructure.Clients, Documentation.Ingestion.Infrastructure.Embeddings, Documentation.Ingestion.Application.Exceptions, Exception, HttpResponseMessage, PermanentIngestionException, CancellationToken (+15 more)

### Community 14 - "Documentation.Ingestion.Infrastructure.Persistence"
Cohesion: 0.22
Nodes (5): Documentation.Ingestion.Infrastructure.Persistence, CancellationToken, Func, Task, IngestionUnitOfWork

### Community 15 - "EmbeddingEngine"
Cohesion: 0.09
Nodes (23): DenseTensor, EmbeddingServiceBase, EmbedRequest, EmbedResponse, HealthCheckContext, HealthCheckResult, IDisposable, IHealthCheck (+15 more)

### Community 16 - "Documentation.Ingestion.Application.Abstractions"
Cohesion: 0.31
Nodes (6): Documentation.Ingestion.Infrastructure.Persistence.Repositories, Documentation.Ingestion.Application.Abstractions, Documentation.Ingestion.Domain.Entities, IChunkRepository, IngestionDbContext, ChunkRepository

### Community 17 - "Documentation.Api"
Cohesion: 0.18
Nodes (10): applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, launchUrl, ASPNETCORE_ENVIRONMENT, profiles (+2 more)

### Community 18 - "Especificação — serviço de ingestão de documentações"
Cohesion: 0.17
Nodes (11): Configuração, Contrato de mensageria, Contrato HTTP esperado da API, Embeddings, Especificação — serviço de ingestão de documentações, Fluxo de processamento, Inicialização local, Limitações deliberadas da POC (+3 more)

### Community 19 - "Documentation.Infrastructure.csproj"
Cohesion: 0.20
Nodes (8): Microsoft.Extensions.Configuration.Abstractions (10.0.0), net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.Extensions.DependencyInjection.Abstractions (10.0.0), Npgsql.EntityFrameworkCore.PostgreSQL (10.0.0), RabbitMQ.Client (6.8.1), Microsoft.NET.Sdk

### Community 20 - ".GetContent"
Cohesion: 0.23
Nodes (10): ControllerBase, HttpPut, ActionResult, CancellationToken, Guid, HttpGet, IActionResult, ProducesResponseType (+2 more)

### Community 21 - "Documentation.Contracts.csproj"
Cohesion: 0.22
Nodes (7): Swashbuckle.AspNetCore (9.0.6), net10.0, Microsoft.NET.Sdk.Web, net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk

### Community 22 - "Documentation API"
Cohesion: 0.22
Nodes (8): Arquitetura, Configuração, Contrato HTTP, Documentation API, Limites da POC, Mensageria, Modelo de persistência, Responsabilidade

### Community 23 - "DocumentationPortal.sln"
Cohesion: 0.29
Nodes (4): net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk

### Community 25 - "Documentation.Embeddings.csproj"
Cohesion: 0.25
Nodes (7): Grpc.AspNetCore (2.80.0), Microsoft.ML.OnnxRuntime (1.28.0), Microsoft.ML.Tokenizers (2.0.0), net10.0, Google.Protobuf (3.35.1), Grpc.Tools (2.83.0), Microsoft.NET.Sdk.Web

### Community 26 - "requirements file"
Cohesion: 0.25
Nodes (8): fastapi, langchain, langchain-openai, psycopg[binary], requirements file, sentence-transformers, torch, uvicorn

### Community 27 - "Documentation Portal POC"
Cohesion: 0.29
Nodes (6): Agente de integração, Componentes, Desenvolvimento, Documentation Portal POC, Embeddings, Executar localmente

### Community 28 - "Documentation.Ingestion.Worker.csproj"
Cohesion: 0.40
Nodes (4): Microsoft.Extensions.Hosting (10.0.0), Microsoft.NET.Sdk.Worker, net10.0, RabbitMQ.Client (6.8.1)

### Community 29 - "Application layer"
Cohesion: 0.67
Nodes (4): Application layer, Documentation.Api service, Domain layer, Infrastructure layer

### Community 30 - "Ingestion worker"
Cohesion: 0.67
Nodes (3): Embedding service, Ingestion worker, RabbitMQ

### Community 45 - "IEmbeddingGenerator"
Cohesion: 0.20
Nodes (7): CancellationToken, Task, IEmbeddingGenerator, CancellationToken, int, Task, EmbeddingGemmaEmbeddingGenerator

### Community 46 - "IngestionDbContext"
Cohesion: 0.43
Nodes (4): EntityTypeBuilder, DbSet, ModelBuilder, IngestionDbContext

### Community 47 - ".ExecuteInTransactionAsync"
Cohesion: 0.33
Nodes (4): CancellationToken, Func, Task, IIngestionUnitOfWork

### Community 48 - "ProcessedIntegrationEventRepository"
Cohesion: 0.38
Nodes (5): CancellationToken, Guid, IngestionDbContext, Task, ProcessedIntegrationEventRepository

### Community 49 - "IProcessedIntegrationEventRepository"
Cohesion: 0.47
Nodes (4): CancellationToken, Guid, Task, IProcessedIntegrationEventRepository

### Community 50 - "ProcessedIntegrationEvent"
Cohesion: 0.50
Nodes (3): DateTimeOffset, Guid, ProcessedIntegrationEvent

### Community 51 - ".ReplaceForVersionAsync"
Cohesion: 0.40
Nodes (4): CancellationToken, Guid, IReadOnlyCollection, Task

## Knowledge Gaps
- **96 isolated node(s):** `smoke.sh script`, `PublishDocumentationResponse`, `net10.0`, `Swashbuckle.AspNetCore (9.0.6)`, `Microsoft.NET.Sdk.Web` (+91 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **15 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Documentation.Contracts` connect `.ProcessAsync` to `Documentation.Application.Abstractions.Persistence`?**
  _High betweenness centrality (0.161) - this node is a cross-community bridge._
- **Why does `IngestionDbContext` connect `IngestionDbContext` to `DocumentChunk`, `DocumentationVersion`, `DatabaseInitializer`, `Documentation.Ingestion.Infrastructure.Persistence`, `ProcessedIntegrationEvent`?**
  _High betweenness centrality (0.099) - this node is a cross-community bridge._
- **What connects `smoke.sh script`, `PublishDocumentationResponse`, `net10.0` to the rest of the system?**
  _96 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Documentation.Application.Abstractions.Persistence` be split into smaller, more focused modules?**
  _Cohesion score 0.08097165991902834 - nodes in this community are weakly interconnected._
- **Should `.ProcessAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.06471631205673758 - nodes in this community are weakly interconnected._
- **Should `Documentation.Ingestion.Infrastructure.csproj` be split into smaller, more focused modules?**
  _Cohesion score 0.13333333333333333 - nodes in this community are weakly interconnected._
- **Should `app.py` be split into smaller, more focused modules?**
  _Cohesion score 0.10416666666666667 - nodes in this community are weakly interconnected._