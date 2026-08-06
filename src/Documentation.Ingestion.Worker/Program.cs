using Documentation.Ingestion.Infrastructure;
using Documentation.Ingestion.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDocumentationIngestionInfrastructure(builder.Configuration);
builder.Services.AddHostedService<DatabaseInitializationHostedService>();
builder.Services.AddHostedService<RabbitMqIngestionWorker>();

await builder.Build().RunAsync();
