# Graph Report - poc_portal  (2026-08-14)

## Corpus Check
- 85 files · ~81,401 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 607 nodes · 939 edges · 44 communities (29 shown, 15 thin omitted)
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 28 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `a811b0d3`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Documentation.Application.Abstractions.Persistence
- DocumentationPublished
- .ProcessAsync
- Documentation.Ingestion.Infrastructure.csproj
- .PublishVersionAsync
- app.py
- Documentation.Ingestion.Application.Models
- DocumentationVersion
- ApiDocumentation
- RabbitMqIngestionWorker
- AgentTests
- DatabaseInitializer
- .Create
- Documentation.Ingestion.Application.Abstractions
- DocumentationApiClient
- EmbeddingEngine
- Documentation.Ingestion.Infrastructure.Options
- Documentation.Api
- Especificação — serviço de ingestão de documentações
- .AddDocumentationIngestionInfrastructure
- .GetContent
- Documentation API
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

## God Nodes (most connected - your core abstractions)
1. `RabbitMqIngestionWorker` - 18 edges
2. `ApiDocumentation` - 17 edges
3. `DocumentationVersion` - 16 edges
4. `AgentTests` - 15 edges
5. `EmbeddingEngine` - 15 edges
6. `Documentation.Ingestion.Application.Abstractions` - 14 edges
7. `DocumentationIngestionService` - 12 edges
8. `Especificação — serviço de ingestão de documentações` - 11 edges
9. `_log()` - 10 edges
10. `IngestionDbContext` - 10 edges

## Surprising Connections (you probably didn't know these)
- `ApiDocumentation` --references--> `DocumentationVersion`  [EXTRACTED]
  src/Documentation.Domain/Entities/ApiDocumentation.cs → src/Documentation.Domain/Entities/DocumentationVersion.cs
- `DocumentationDbContext` --references--> `ApiDocumentation`  [EXTRACTED]
  src/Documentation.Infrastructure/Persistence/DocumentationDbContext.cs → src/Documentation.Domain/Entities/ApiDocumentation.cs
- `DocumentationDbContext` --references--> `DocumentationVersion`  [EXTRACTED]
  src/Documentation.Infrastructure/Persistence/DocumentationDbContext.cs → src/Documentation.Domain/Entities/DocumentationVersion.cs
- `DocumentationIngestionService` --references--> `IDocumentationApiClient`  [EXTRACTED]
  src/Documentation.Ingestion.Application/Services/DocumentationIngestionService.cs → src/Documentation.Ingestion.Application/Abstractions/IDocumentationApiClient.cs
- `DocumentationApiClient` --implements--> `IDocumentationApiClient`  [EXTRACTED]
  src/Documentation.Ingestion.Infrastructure/Clients/DocumentationApiClient.cs → src/Documentation.Ingestion.Application/Abstractions/IDocumentationApiClient.cs

## Import Cycles
- None detected.

## Communities (44 total, 15 thin omitted)

### Community 0 - "Documentation.Application.Abstractions.Persistence"
Cohesion: 0.06
Nodes (28): Documentation.Infrastructure.Messaging, Documentation.Infrastructure, Documentation.Application.Abstractions.Messaging, Documentation.Application.Abstractions.Persistence, Documentation.Application.Models, Documentation.Api.Controllers, Documentation.Infrastructure.Persistence, Documentation.Domain.Entities (+20 more)

### Community 1 - "DocumentationPublished"
Cohesion: 0.11
Nodes (16): ConnectionFactory, Documentation.Contracts, CancellationToken, Task, IDocumentationEventPublisher, string, DocumentationPublished, string (+8 more)

### Community 2 - ".ProcessAsync"
Cohesion: 0.06
Nodes (33): EntityTypeBuilder, CancellationToken, Guid, IReadOnlyCollection, Task, CancellationToken, Guid, Task (+25 more)

### Community 3 - "Documentation.Ingestion.Infrastructure.csproj"
Cohesion: 0.05
Nodes (40): Grpc.Net.ClientFactory (2.80.0), Microsoft.EntityFrameworkCore (10.0.0), Microsoft.Extensions.Configuration.Abstractions (10.0.0), Microsoft.Extensions.Configuration.Binder (10.0.0), Microsoft.Extensions.Hosting (10.0.0), Microsoft.Extensions.Http (10.0.0), Microsoft.Extensions.Options.ConfigurationExtensions (10.0.0), Pgvector.EntityFrameworkCore (0.2.2) (+32 more)

### Community 4 - ".PublishVersionAsync"
Cohesion: 0.23
Nodes (11): CreateDocumentationCommand, DocumentationContent, DocumentationSummary, DocumentationVersionSummary, PublishAttemptResult, PublishAttemptResultKind, CancellationToken, Guid (+3 more)

### Community 5 - "app.py"
Cohesion: 0.11
Nodes (28): Any, BaseModel, ChatOpenAI, FastAPI, get, middleware, post, Request (+20 more)

### Community 6 - "Documentation.Ingestion.Application.Models"
Cohesion: 0.14
Nodes (12): Documentation.Ingestion.Domain.ValueObjects, Documentation.Ingestion.Application.Models, IDeserializer, JsonDocument, JsonElement, IReadOnlyList, IOpenApiChunker, DocumentationContent (+4 more)

### Community 7 - "DocumentationVersion"
Cohesion: 0.13
Nodes (12): CancellationToken, Guid, Task, IDocumentationVersionRepository, DateTimeOffset, Guid, DocumentationVersion, DocumentationVersionStatus (+4 more)

### Community 8 - "ApiDocumentation"
Cohesion: 0.14
Nodes (14): ICollection, CancellationToken, Guid, IReadOnlyList, Task, IApiDocumentationRepository, DateTimeOffset, Guid (+6 more)

### Community 9 - "RabbitMqIngestionWorker"
Cohesion: 0.20
Nodes (11): BackgroundService, BasicDeliverEventArgs, IDictionary, IModel, CancellationToken, ILogger, IServiceScopeFactory, JsonSerializerOptions (+3 more)

### Community 10 - "AgentTests"
Cohesion: 0.11
Nodes (4): object, AgentTests, FailingSupervisor, SuccessfulSupervisor

### Community 11 - "DatabaseInitializer"
Cohesion: 0.16
Nodes (12): IHostedService, CancellationToken, ILogger, IServiceScopeFactory, string, Task, DatabaseInitializer, CancellationToken (+4 more)

### Community 12 - ".Create"
Cohesion: 0.30
Nodes (10): HttpPost, ActionResult, CancellationToken, Guid, HttpGet, IActionResult, IReadOnlyList, ProducesResponseType (+2 more)

### Community 13 - "Documentation.Ingestion.Application.Abstractions"
Cohesion: 0.05
Nodes (35): Documentation.Ingestion.Infrastructure.OpenApi, Documentation.Ingestion.Infrastructure.Clients, Documentation.Ingestion.Infrastructure.Persistence, Documentation.Ingestion.Infrastructure.Persistence.Repositories, Documentation.Ingestion.Application.Abstractions, Documentation.Ingestion.Infrastructure.Embeddings, Documentation.Ingestion.Application.Services, Documentation.Ingestion.Infrastructure (+27 more)

### Community 14 - "DocumentationApiClient"
Cohesion: 0.20
Nodes (10): HttpRequestMessage, HttpResponseMessage, DocumentationIndexingStatus, CancellationToken, Guid, HttpClient, JsonSerializerOptions, Task (+2 more)

### Community 15 - "EmbeddingEngine"
Cohesion: 0.09
Nodes (23): DenseTensor, EmbeddingServiceBase, EmbedRequest, EmbedResponse, HealthCheckContext, HealthCheckResult, IDisposable, IHealthCheck (+15 more)

### Community 16 - "Documentation.Ingestion.Infrastructure.Options"
Cohesion: 0.20
Nodes (7): Documentation.Ingestion.Infrastructure.Options, string, DocumentationApiOptions, string, EmbeddingsOptions, string, RabbitMqOptions

### Community 17 - "Documentation.Api"
Cohesion: 0.18
Nodes (10): applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, launchUrl, ASPNETCORE_ENVIRONMENT, profiles (+2 more)

### Community 18 - "Especificação — serviço de ingestão de documentações"
Cohesion: 0.17
Nodes (11): Configuração, Contrato de mensageria, Contrato HTTP esperado da API, Embeddings, Especificação — serviço de ingestão de documentações, Fluxo de processamento, Inicialização local, Limitações deliberadas da POC (+3 more)

### Community 19 - ".AddDocumentationIngestionInfrastructure"
Cohesion: 0.50
Nodes (3): IConfiguration, IServiceCollection, DependencyInjection

### Community 20 - ".GetContent"
Cohesion: 0.23
Nodes (10): ControllerBase, HttpPut, ActionResult, CancellationToken, Guid, HttpGet, IActionResult, ProducesResponseType (+2 more)

### Community 22 - "Documentation API"
Cohesion: 0.22
Nodes (8): Arquitetura, Configuração, Contrato HTTP, Documentation API, Limites da POC, Mensageria, Modelo de persistência, Responsabilidade

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

## Knowledge Gaps
- **103 isolated node(s):** `smoke.sh script`, `PublishDocumentationResponse`, `net10.0`, `Swashbuckle.AspNetCore (9.0.6)`, `Microsoft.NET.Sdk.Web` (+98 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **15 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Documentation.Contracts` connect `DocumentationPublished` to `Documentation.Application.Abstractions.Persistence`, `Documentation.Ingestion.Application.Abstractions`?**
  _High betweenness centrality (0.148) - this node is a cross-community bridge._
- **Why does `IngestionDbContext` connect `.ProcessAsync` to `Documentation.Application.Abstractions.Persistence`, `DatabaseInitializer`, `Documentation.Ingestion.Application.Abstractions`?**
  _High betweenness centrality (0.089) - this node is a cross-community bridge._
- **Why does `DatabaseInitializationHostedService` connect `DatabaseInitializer` to `Documentation.Ingestion.Application.Abstractions`?**
  _High betweenness centrality (0.088) - this node is a cross-community bridge._
- **What connects `smoke.sh script`, `PublishDocumentationResponse`, `net10.0` to the rest of the system?**
  _103 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Documentation.Application.Abstractions.Persistence` be split into smaller, more focused modules?**
  _Cohesion score 0.061979648473635525 - nodes in this community are weakly interconnected._
- **Should `DocumentationPublished` be split into smaller, more focused modules?**
  _Cohesion score 0.10507246376811594 - nodes in this community are weakly interconnected._
- **Should `.ProcessAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.0563265306122449 - nodes in this community are weakly interconnected._