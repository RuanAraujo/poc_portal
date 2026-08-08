using Documentation.Application.Abstractions.Messaging;
using Documentation.Application.Abstractions.Persistence;
using Documentation.Infrastructure.Messaging;
using Documentation.Infrastructure.Persistence;
using Documentation.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Documentation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDocumentationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionStrings:Postgres"]
            ?? configuration["ConnectionStrings:DocumentationDb"]
            ?? "Host=localhost;Port=5432;Database=documentation_portal;Username=postgres;Password=postgres";

        services.AddDbContext<DocumentationDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IApiDocumentationRepository, ApiDocumentationRepository>();
        services.AddScoped<IDocumentationVersionRepository, DocumentationVersionRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<DocumentationDbContext>());
        services.AddSingleton(RabbitMqOptions.FromConfiguration(configuration));
        services.AddSingleton<IDocumentationEventPublisher, RabbitMqDocumentationEventPublisher>();

        return services;
    }
}
