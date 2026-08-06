using Microsoft.Extensions.Configuration;

namespace Documentation.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public string UserName { get; init; } = "guest";

    public string Password { get; init; } = "guest";

    public string VirtualHost { get; init; } = "/";

    public static RabbitMqOptions FromConfiguration(IConfiguration configuration)
    {
        var rawPort = configuration["RabbitMq:Port"];
        return new RabbitMqOptions
        {
            Host = configuration["RabbitMq:Host"]
                ?? configuration["RabbitMq:HostName"]
                ?? "localhost",
            Port = int.TryParse(rawPort, out var port) ? port : 5672,
            UserName = configuration["RabbitMq:UserName"] ?? "guest",
            Password = configuration["RabbitMq:Password"] ?? "guest",
            VirtualHost = configuration["RabbitMq:VirtualHost"] ?? "/"
        };
    }
}
