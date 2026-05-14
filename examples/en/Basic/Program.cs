// Example: basic usage of the Inso.Els .NET SDK.
//
// Demonstrates client initialization, async capture, synchronous send for
// critical errors, a health check, and graceful shutdown.
//
// Run:
//   export ELS_ENDPOINT=https://api.insoweb.ru/els
//   export ELS_API_KEY=your-api-key
//   dotnet run

using Inso.Els;

var options = new ElsOptions
{
    Endpoint = Environment.GetEnvironmentVariable("ELS_ENDPOINT") ?? "https://api.insoweb.ru/els",
    ApiKey = Environment.GetEnvironmentVariable("ELS_API_KEY") ?? "your-api-key",
    AppSlug = "my-service",
    DeploymentEnv = "PRODUCTION",
    ServiceName = "api-gateway",
    BatchSize = 50,
    BatchInterval = TimeSpan.FromSeconds(5),
    MinLevel = ElsLevel.Warning,
    OnError = ex => Console.Error.WriteLine($"[ELS] internal error: {ex.Message}"),
};

await using var client = new ElsClient(options);

// 1. Check connectivity (optional, but useful in dev environments).
try
{
    await client.HealthAsync();
    Console.WriteLine("ELS server reachable.");
}
catch (ElsSendException ex)
{
    Console.WriteLine($"ELS server unreachable ({ex.StatusCode}): captures will be buffered.");
}

// 2. Async capture with stack trace.
try
{
    throw new InvalidOperationException("database connection timeout");
}
catch (Exception ex)
{
    client.CaptureError(ex,
        new CaptureOptions
        {
            Url = "/api/users",
            Level = ElsLevel.Critical,
            Meta = new Dictionary<string, object?>
            {
                ["database"] = "postgres-primary",
                ["timeout"] = "30s",
            },
        });
}

// 3. Async message capture.
client.CaptureMessage("memory usage above 80%", ElsLevel.Warning,
    new CaptureOptions { Url = "/health" }.WithMetaItem("memoryPct", 82.5));

// 4. Synchronous send for critical errors.
try
{
    await client.SendAsync(
        new Exception("payment processing failed"),
        new CaptureOptions { Url = "/api/payments/charge", Level = ElsLevel.Critical }
            .WithMetaItem("orderId", "ord_12345")
            .WithMetaItem("amount", 9900));
}
catch (ElsSendException ex)
{
    if (ex.IsRetryable) Console.WriteLine($"Retryable ELS error: {ex.Message}");
    else Console.WriteLine($"Permanent ELS error: {ex.Message}");
}

// 5. Pre-built entry.
client.CaptureEntry(new ErrorEntry
{
    Message = "unusual traffic pattern detected",
    Url = "/api/analytics",
    Level = ElsLevel.Warning,
    Meta = new Dictionary<string, object?>
    {
        ["rps"] = 15000,
        ["threshold"] = 10000,
    },
});

// 6. Ensure pending entries are sent before exit.
await client.FlushAsync();
Console.WriteLine("All errors captured. Shutting down.");
