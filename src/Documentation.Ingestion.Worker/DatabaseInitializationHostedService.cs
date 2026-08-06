using Documentation.Ingestion.Infrastructure.Persistence;

namespace Documentation.Ingestion.Worker;

public sealed class DatabaseInitializationHostedService : IHostedService
{
    private const int MaximumAttempts = 10;

    private readonly DatabaseInitializer _databaseInitializer;
    private readonly ILogger<DatabaseInitializationHostedService> _logger;

    public DatabaseInitializationHostedService(
        DatabaseInitializer databaseInitializer,
        ILogger<DatabaseInitializationHostedService> logger)
    {
        _databaseInitializer = databaseInitializer;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            try
            {
                await _databaseInitializer.InitializeAsync(cancellationToken);
                return;
            }
            catch (Exception exception) when (attempt < MaximumAttempts && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    exception,
                    "Ingestion database is not ready yet (attempt {Attempt}/{MaximumAttempts}).",
                    attempt,
                    MaximumAttempts);
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }

        await _databaseInitializer.InitializeAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
