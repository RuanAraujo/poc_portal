using Documentation.Ingestion.Application.Abstractions;
using Documentation.Ingestion.Application.Services;
using Documentation.Embeddings.Grpc;
using Documentation.Ingestion.Infrastructure.Clients;
using Documentation.Ingestion.Infrastructure.Embeddings;
using Documentation.Ingestion.Infrastructure.OpenApi;
using Documentation.Ingestion.Infrastructure.Options;
using Documentation.Ingestion.Infrastructure.Persistence;
using Documentation.Ingestion.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Documentation.Ingestion.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDocumentationIngestionInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres must be configured.");

        services.Configure<DocumentationApiOptions>(configuration.GetSection(DocumentationApiOptions.SectionName));
        services.Configure<EmbeddingsOptions>(configuration.GetSection(EmbeddingsOptions.SectionName));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseVector();
        var dataSource = dataSourceBuilder.Build();
        services.AddSingleton(dataSource);
        services.AddDbContext<IngestionDbContext>(options =>
            options.UseNpgsql(dataSource, npgsqlOptions => npgsqlOptions.UseVector()));

        services.AddScoped<IChunkRepository, ChunkRepository>();
        services.AddScoped<IProcessedIntegrationEventRepository, ProcessedIntegrationEventRepository>();
        services.AddScoped<IIngestionUnitOfWork, IngestionUnitOfWork>();
        services.AddScoped<IIngestionService, DocumentationIngestionService>();
        services.AddSingleton<IOpenApiChunker, OpenApiChunker>();
        services.AddSingleton<DatabaseInitializer>();

        services.AddHttpClient<IDocumentationApiClient, DocumentationApiClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<DocumentationApiOptions>>().Value;
            httpClient.BaseAddress = new Uri(EnsureTrailingSlash(options.BaseUrl));
        });

        var embeddingOptions = configuration.GetSection(EmbeddingsOptions.SectionName).Get<EmbeddingsOptions>()
            ?? new EmbeddingsOptions();
        if (embeddingOptions.Dimensions != EmbeddingGemmaEmbeddingGenerator.RequiredDimensions)
        {
            throw new InvalidOperationException(
                $"Embeddings:Dimensions must be {EmbeddingGemmaEmbeddingGenerator.RequiredDimensions} for this POC.");
        }

        services.AddGrpcClient<EmbeddingService.EmbeddingServiceClient>(options =>
        {
            options.Address = new Uri(embeddingOptions.BaseUrl);
        });
        services.AddScoped<IEmbeddingGenerator, EmbeddingGemmaEmbeddingGenerator>();

        return services;
    }

    private static string EnsureTrailingSlash(string baseUrl) =>
        baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : $"{baseUrl}/";
}
