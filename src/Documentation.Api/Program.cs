using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Documentation.Application.Services;
using Documentation.Infrastructure;
using Documentation.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff zzz ";
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration["PORTAL_ORIGIN"] ?? "http://localhost:3000")
    .AllowAnyHeader()
    .AllowAnyMethod()));
builder.Services.AddDocumentationInfrastructure(builder.Configuration);
builder.Services.AddScoped<DocumentationApplicationService>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DocumentationDbContext>();
    await dbContext.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS documentation;");
    await dbContext.Database.EnsureCreatedAsync();
}

app.Use(async (context, next) =>
{
    const string correlationHeader = "X-Correlation-ID";
    var hasCorrelationHeader = context.Request.Headers.TryGetValue(correlationHeader, out var values);
    var hasValidCorrelationId = hasCorrelationHeader
        && values.Count == 1
        && IsValidCorrelationId(values[0]);
    var correlationId = hasValidCorrelationId ? values[0]! : Guid.NewGuid().ToString("N");

    context.Response.Headers[correlationHeader] = correlationId;
    Activity.Current?.SetBaggage("CorrelationId", correlationId);

    using var scope = app.Logger.BeginScope("CorrelationId:{CorrelationId}", correlationId);

    if (hasCorrelationHeader && !hasValidCorrelationId)
    {
        app.Logger.LogWarning(
            "Ignoring invalid correlation header with {ValueCount} value(s) and first value length {ValueLength}",
            values.Count,
            values.Count == 0 ? 0 : values[0]?.Length ?? 0);
    }

    var stopwatch = Stopwatch.StartNew();

    try
    {
        await next(context);

        if (IsQuietSuccessfulEndpoint(context))
        {
            return;
        }

        if (context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            app.Logger.LogError(
                "HTTP request completed {Method} {Path} with {StatusCode} in {ElapsedMs} ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        else if (context.Response.StatusCode >= StatusCodes.Status400BadRequest)
        {
            app.Logger.LogWarning(
                "HTTP request completed {Method} {Path} with {StatusCode} in {ElapsedMs} ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        else
        {
            app.Logger.LogInformation(
                "HTTP request completed {Method} {Path} with {StatusCode} in {ElapsedMs} ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
    catch (Exception exception)
    {
        app.Logger.LogError(
            exception,
            "HTTP request failed {Method} {Path} in {ElapsedMs} ms",
            context.Request.Method,
            context.Request.Path,
            stopwatch.ElapsedMilliseconds);
        throw;
    }
});

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

static bool IsValidCorrelationId(string? value) =>
    value is { Length: >= 1 and <= 128 }
    && char.IsAsciiLetterOrDigit(value[0])
    && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or ':' or '-');

static bool IsQuietSuccessfulEndpoint(HttpContext context) =>
    context.Response.StatusCode < StatusCodes.Status400BadRequest
    && (context.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase)
        || context.Request.Path.StartsWithSegments("/swagger"));

public partial class Program
{
}
