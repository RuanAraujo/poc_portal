using Documentation.Ingestion.Infrastructure;
using Documentation.Ingestion.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffzzz ";
});

builder.Services.AddDocumentationIngestionInfrastructure(builder.Configuration);
builder.Services.AddHostedService<DatabaseInitializationHostedService>();
builder.Services.AddHostedService<RabbitMqIngestionWorker>();

await builder.Build().RunAsync();
