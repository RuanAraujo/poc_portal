using System.Diagnostics;
using Documentation.Embeddings.Grpc;
using Grpc.Core;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
});
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, listen => listen.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
    options.ListenAnyIP(8081, listen => listen.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1);
});
builder.Services.AddGrpc();
builder.Services.AddSingleton<EmbeddingEngine>();
builder.Services.AddHostedService<ModelInitializer>();
builder.Services.AddHealthChecks().AddCheck<ModelHealthCheck>("model");

var app = builder.Build();
if (args.Contains("--self-check", StringComparer.Ordinal))
{
    var stopwatch = Stopwatch.StartNew();
    app.Logger.LogInformation("Embedding self-check started. Step={Step}", "SelfCheck");
    var engine = app.Services.GetRequiredService<EmbeddingEngine>();
    await engine.InitializeAsync(CancellationToken.None);
    var query = await engine.EmbedAsync(EmbeddingGrpcService.Prepare("self check", EmbeddingGrpcService.QueryPrefix), CancellationToken.None);
    var document = await engine.EmbedAsync(EmbeddingGrpcService.Prepare("self check", EmbeddingGrpcService.DocumentPrefix), CancellationToken.None);
    var truncated = await engine.EmbedAsync(string.Join(' ', Enumerable.Repeat("word", 2100)), CancellationToken.None);
    if (!Valid(query) || !Valid(document) || !Valid(truncated) || query.SequenceEqual(document))
        throw new InvalidOperationException("Embedding self-check failed.");
    foreach (var invalid in new[] { " ", new string('x', 200001) })
    {
        try
        {
            EmbeddingGrpcService.Prepare(invalid, EmbeddingGrpcService.QueryPrefix);
            throw new InvalidOperationException("Invalid text was accepted.");
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.InvalidArgument) { }
    }
    app.Logger.LogInformation(
        "Embedding self-check completed. Step={Step} Outcome={Outcome} ElapsedMs={ElapsedMs}",
        "SelfCheck", "Succeeded", stopwatch.ElapsedMilliseconds);
    return;

    static bool Valid(float[] embedding) =>
        embedding.Length == 768 && embedding.All(float.IsFinite) &&
        Math.Abs(Math.Sqrt(embedding.Sum(x => x * x)) - 1) <= 0.001;
}

app.MapGrpcService<EmbeddingGrpcService>();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true });
app.Run();

sealed class EmbeddingGrpcService(EmbeddingEngine engine, ILogger<EmbeddingGrpcService> logger) : EmbeddingService.EmbeddingServiceBase
{
    internal const string QueryPrefix = "task: search result | query: ";
    internal const string DocumentPrefix = "title: none | text: ";

    public override async Task<EmbedResponse> EmbedQuery(EmbedRequest request, ServerCallContext context) =>
        await Embed(request.Text, QueryPrefix, nameof(EmbedQuery), context);

    public override async Task<EmbedResponse> EmbedDocument(EmbedRequest request, ServerCallContext context) =>
        await Embed(request.Text, DocumentPrefix, nameof(EmbedDocument), context);

    private async Task<EmbedResponse> Embed(string text, string prefix, string rpcMethod, ServerCallContext context)
    {
        var correlationHeaders = context.RequestHeaders
            .Where(header => header.Key == "x-correlation-id")
            .Select(header => header.Value)
            .ToArray();
        var correlationId = correlationHeaders.Length == 1 ? correlationHeaders[0] : null;
        if (correlationId is not { Length: > 0 and <= 128 } ||
            !char.IsAsciiLetterOrDigit(correlationId[0]) ||
            correlationId.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_' and not '.' and not ':'))
            correlationId = Guid.NewGuid().ToString("N");

        using var scope = logger.BeginScope("CorrelationId={CorrelationId} RpcMethod={RpcMethod}", correlationId, rpcMethod);
        var stopwatch = Stopwatch.StartNew();
        var inputCharacters = text.Length;
        logger.LogInformation(
            "Embedding RPC received. Step={Step} InputCharacters={InputCharacters}",
            "Received", inputCharacters);
        var response = new EmbedResponse();
        try
        {
            response.Embedding.Add(await engine.EmbedAsync(Prepare(text, prefix), context.CancellationToken));
            logger.LogInformation(
                "Embedding RPC completed. Step={Step} Outcome={Outcome} InputCharacters={InputCharacters} Dimensions={Dimensions} ElapsedMs={ElapsedMs}",
                "Completed", "Succeeded", inputCharacters, response.Embedding.Count, stopwatch.ElapsedMilliseconds);
        }
        catch (InvalidDataException exception)
        {
            logger.LogWarning(
                "Embedding RPC rejected. Step={Step} Outcome={Outcome} InputCharacters={InputCharacters} ElapsedMs={ElapsedMs} Reason={Reason}",
                "Completed", "InvalidArgument", inputCharacters, stopwatch.ElapsedMilliseconds, exception.Message);
            throw new RpcException(new Status(StatusCode.InvalidArgument, exception.Message));
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.InvalidArgument)
        {
            logger.LogWarning(
                "Embedding RPC rejected. Step={Step} Outcome={Outcome} InputCharacters={InputCharacters} ElapsedMs={ElapsedMs}",
                "Completed", "InvalidArgument", inputCharacters, stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning(
                "Embedding RPC cancelled. Step={Step} Outcome={Outcome} InputCharacters={InputCharacters} ElapsedMs={ElapsedMs}",
                "Completed", "Cancelled", inputCharacters, stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Embedding RPC failed. Step={Step} Outcome={Outcome} InputCharacters={InputCharacters} ElapsedMs={ElapsedMs}",
                "Completed", "Failed", inputCharacters, stopwatch.ElapsedMilliseconds);
            throw;
        }
        return response;
    }

    internal static string Prepare(string text, string prefix)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 200000)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "text must contain 1 to 200000 non-whitespace characters."));
        return prefix + text.Trim();
    }
}

sealed class ModelInitializer(EmbeddingEngine engine) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken) => await engine.InitializeAsync(cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

sealed class ModelHealthCheck(EmbeddingEngine engine) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(engine.IsReady ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Model is still initializing."));
}

sealed class EmbeddingEngine(ILogger<EmbeddingEngine> logger) : IDisposable
{
    private const string Revision = "75a84c732f1884df76bec365346230e32f582c82";
    private const string Repository = "https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/" + Revision + "/";
    private const int MaxTokens = 2048;
    private readonly string _modelDirectory = Environment.GetEnvironmentVariable("MODEL_DIR") ?? "/models/huggingface";
    private readonly HttpClient _http = new();
    private readonly SemaphoreSlim _gate = new(1, 1); // ponytail: one CPU inference at a time; add batching when throughput needs it.
    private InferenceSession? _session;
    private SentencePieceTokenizer? _tokenizer;
    public bool IsReady { get; private set; }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (IsReady) return;
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation(
            "Embedding model initialization started. Step={Step} ModelDirectory={ModelDirectory}",
            "Initialize", _modelDirectory);
        try
        {
            Directory.CreateDirectory(_modelDirectory);
            await DownloadAsync("onnx/model.onnx", "model.onnx", cancellationToken);
            await DownloadAsync("onnx/model.onnx_data", "model.onnx_data", cancellationToken);
            await DownloadAsync("tokenizer.model", "tokenizer.model", cancellationToken);
            logger.LogInformation("Embedding model files ready. Step={Step}", "LoadModel");
            _session = new InferenceSession(Path.Combine(_modelDirectory, "model.onnx"));
            await using var tokenizerFile = File.OpenRead(Path.Combine(_modelDirectory, "tokenizer.model"));
            _tokenizer = SentencePieceTokenizer.Create(tokenizerFile, addBeginningOfSentence: true, addEndOfSentence: true);
            var probe = await EmbedAsync("task: search result | query: probe", cancellationToken);
            if (probe.Length != 768) throw new InvalidOperationException("The model did not return 768 dimensions.");
            IsReady = true;
            logger.LogInformation(
                "Embedding model is ready. Step={Step} Outcome={Outcome} Dimensions={Dimensions} ElapsedMs={ElapsedMs}",
                "Ready", "Succeeded", probe.Length, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Embedding model initialization failed. Step={Step} Outcome={Outcome} ElapsedMs={ElapsedMs}",
                "Initialize", "Failed", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        if (_session is null || _tokenizer is null) throw new InvalidOperationException("Model is not initialized.");
        var ids = _tokenizer.EncodeToIds(text, addBeginningOfSentence: true, addEndOfSentence: true, maxTokenCount: MaxTokens, out _, out _);
        logger.LogInformation("Embedding input tokenized. Step={Step} TokenCount={TokenCount}", "Tokenize", ids.Count);
        var inputIds = new DenseTensor<long>([1, ids.Count]);
        var attentionMask = new DenseTensor<long>([1, ids.Count]);
        for (var i = 0; i < ids.Count; i++) { inputIds[0, i] = ids[i]; attentionMask[0, i] = 1; }

        await _gate.WaitAsync(cancellationToken);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogInformation("Embedding inference started. Step={Step} TokenCount={TokenCount}", "Inference", ids.Count);
            var inputs = CreateInputs(inputIds, attentionMask);
            using var results = _session.Run(inputs);
            var output = results.First(x => x.Name == "sentence_embedding").AsTensor<float>().ToArray();
            if (output.Length != 768) throw new InvalidDataException($"Expected 768 dimensions, got {output.Length}.");
            Normalize(output);
            logger.LogInformation(
                "Embedding inference completed. Step={Step} Outcome={Outcome} TokenCount={TokenCount} Dimensions={Dimensions} ElapsedMs={ElapsedMs}",
                "Inference", "Succeeded", ids.Count, output.Length, stopwatch.ElapsedMilliseconds);
            return output;
        }
        finally { _gate.Release(); }
    }

    private IReadOnlyCollection<NamedOnnxValue> CreateInputs(DenseTensor<long> inputIds, DenseTensor<long> attentionMask)
    {
        var inputs = new List<NamedOnnxValue>();
        foreach (var input in _session!.InputMetadata.Keys)
        {
            inputs.Add(input switch
            {
                "input_ids" => NamedOnnxValue.CreateFromTensor(input, inputIds),
                "attention_mask" => NamedOnnxValue.CreateFromTensor(input, attentionMask),
                "position_ids" => NamedOnnxValue.CreateFromTensor(input, Positions(inputIds.Dimensions[1])),
                _ => throw new InvalidOperationException($"Unsupported ONNX input '{input}'.")
            });
        }
        return inputs;
    }

    private static DenseTensor<long> Positions(int count)
    {
        var positions = new DenseTensor<long>([1, count]);
        for (var i = 0; i < count; i++) positions[0, i] = i;
        return positions;
    }

    private async Task DownloadAsync(string remoteName, string localName, CancellationToken cancellationToken)
    {
        var destination = Path.Combine(_modelDirectory, localName);
        if (File.Exists(destination))
        {
            logger.LogInformation("Embedding model artifact found in cache. Step={Step} Artifact={Artifact}", "Download", localName);
            return;
        }
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Embedding model artifact download started. Step={Step} Artifact={Artifact}", "Download", localName);
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        using var response = await _http.GetAsync(Repository + remoteName, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var target = File.Create(temporary))
            await source.CopyToAsync(target, cancellationToken);
        File.Move(temporary, destination);
        logger.LogInformation(
            "Embedding model artifact download completed. Step={Step} Outcome={Outcome} Artifact={Artifact} ElapsedMs={ElapsedMs}",
            "Download", "Succeeded", localName, stopwatch.ElapsedMilliseconds);
    }

    private static void Normalize(float[] embedding)
    {
        var norm = Math.Sqrt(embedding.Sum(x => x * x));
        if (!double.IsFinite(norm) || norm == 0) throw new InvalidDataException("Model returned an invalid embedding.");
        for (var i = 0; i < embedding.Length; i++) embedding[i] = (float)(embedding[i] / norm);
    }

    public void Dispose()
    {
        _session?.Dispose();
        _http.Dispose();
        _gate.Dispose();
    }
}
