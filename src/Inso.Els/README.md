# Inso.Els

.NET SDK for ELS (Error Logs Service). Async batching, retry with exponential
backoff, disk-based buffering, zero non-standard runtime dependencies.

![ELS dashboard — logs list](https://raw.githubusercontent.com/official-inso/els-go/main/docs/screenshots/01-error-logs-list.png)
![ELS dashboard — event detail with AI diagnosis](https://raw.githubusercontent.com/official-inso/els-go/main/docs/screenshots/03-error-detail-ai.png)

## Install

```bash
dotnet add package Inso.Els
```

or in `.csproj`:

```xml
<PackageReference Include="Inso.Els" Version="0.2.1" />
```

## Quick start

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

## Companion packages

- [`Inso.Els.AspNetCore`](https://www.nuget.org/packages/Inso.Els.AspNetCore) — exception-capturing middleware + `services.AddEls(...)` for ASP.NET Core.
- [`Inso.Els.Extensions.Logging`](https://www.nuget.org/packages/Inso.Els.Extensions.Logging) — `ILoggerProvider` that routes `Microsoft.Extensions.Logging` records to ELS.

## Links

- Source & full documentation: <https://github.com/official-inso/els-csharp>
- NuGet: <https://www.nuget.org/packages/Inso.Els>
- License: MIT
