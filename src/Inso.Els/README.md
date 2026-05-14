# Inso.Els

.NET SDK for ELS (Error Logs Service). Async batching, retry with exponential
backoff, disk-based buffering, zero non-standard runtime dependencies.

![ELS dashboard — logs list](https://raw.githubusercontent.com/official-inso/els-go/main/docs/screenshots/01-error-logs-list.png)
![ELS dashboard — event detail with AI diagnosis](https://raw.githubusercontent.com/official-inso/els-go/main/docs/screenshots/03-error-detail-ai.png)

```csharp
using Inso.Els;

await using var client = new ElsClient(new ElsOptions
{
    Endpoint = "https://api.insoweb.ru/els",
    ApiKey   = "your-api-key",
    AppSlug  = "my-service",
    DeploymentEnv = "PRODUCTION",
});

client.CaptureError(ex, new CaptureOptions { Url = "/api/users" });
await client.FlushAsync();
```

See the [project repository](https://github.com/official-inso/els-csharp) for full
documentation, examples, and migration notes.
