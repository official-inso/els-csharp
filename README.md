# Inso.Els — .NET SDK for ELS

.NET SDK for the **Error Logs Service (ELS)**. Asynchronous batching, retry
with exponential backoff, disk-based buffering, ASP.NET Core middleware,
`Microsoft.Extensions.Logging` provider — wire-compatible with the
[Go SDK](https://github.com/official-inso/els-go).

[Русская версия](README_RU.md)

## What you get

ELS ships with a built-in admin dashboard. Every event captured from your .NET
application shows up there with full-text search, faceted filtering, AI-assisted
diagnosis, and version-aware regression detection.

| | |
|---|---|
| ![Logs list](https://raw.githubusercontent.com/official-inso/els-go/main/docs/screenshots/01-error-logs-list.png) | ![Event detail](https://raw.githubusercontent.com/official-inso/els-go/main/docs/screenshots/02-event-detail-info.png) |
| Virtual table with facet sidebar (app, env, **version**, source, level, browser, IP, category). | Full event metadata: timestamps, geo, env, **app version**, fingerprint, session. |
| ![AI diagnosis](https://raw.githubusercontent.com/official-inso/els-go/main/docs/screenshots/03-error-detail-ai.png) | ![Analytics](https://raw.githubusercontent.com/official-inso/els-go/main/docs/screenshots/04-analytics-dashboard.png) |
| Stack trace + AI diagnosis: what broke, where, how to fix. | Timeline, donuts, **version regressions** widget. |
| ![API keys](https://raw.githubusercontent.com/official-inso/els-go/main/docs/screenshots/05-api-keys.png) | ![Favorites](https://raw.githubusercontent.com/official-inso/els-go/main/docs/screenshots/07-favorites.png) |
| Scoped API keys (write / read / read-any), live & test environments, rotation. | Bookmarks for trace IDs that survive across sessions. |

## Packages

| Package | What it is |
|---|---|
| `Inso.Els` | Core SDK: client, options, batching worker, transport, disk buffer. |
| `Inso.Els.AspNetCore` | ASP.NET Core middleware + DI extensions. |
| `Inso.Els.Extensions.Logging` | `ILoggerProvider` that routes log records to ELS. |

## Install

```bash
dotnet add package Inso.Els
dotnet add package Inso.Els.AspNetCore           # optional
dotnet add package Inso.Els.Extensions.Logging   # optional
```

Target frameworks:

- `Inso.Els` — `netstandard2.0`, `netstandard2.1`, `net6.0`, `net8.0`.
- `Inso.Els.AspNetCore` — `net6.0`, `net8.0`.
- `Inso.Els.Extensions.Logging` — `netstandard2.0`, `net6.0`, `net8.0`.

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

try
{
    DoWork();
}
catch (Exception ex)
{
    client.CaptureError(ex, new CaptureOptions { Url = "/api/users" });
}

await client.FlushAsync();
```

### ASP.NET Core

```csharp
using Inso.Els.AspNetCore;

builder.Services.AddEls(opts =>
{
    opts.Endpoint = builder.Configuration["Els:Endpoint"]!;
    opts.ApiKey   = builder.Configuration["Els:ApiKey"]!;
    opts.AppSlug  = "my-web-app";
});

var app = builder.Build();
app.UseElsExceptionHandling(); // captures unhandled exceptions
app.MapGet("/", () => "OK");
app.Run();
```

### Microsoft.Extensions.Logging

```csharp
using Inso.Els.Extensions.Logging;

builder.Logging.AddEls(o => o.MinLevel = ElsLevel.Warning);

// Existing code keeps working:
_logger.LogError(ex, "user {UserId} not found", 42);
```

## Core concepts

### Async vs synchronous

Captures are **asynchronous** — they return immediately, and the SDK delivers
batches in the background:

```csharp
client.CaptureError(ex, new CaptureOptions { Url = "/api" });   // non-blocking
client.CaptureMessage("started", ElsLevel.Info);                // non-blocking
```

For critical errors where you need **delivery confirmation** before continuing,
use `SendAsync`:

```csharp
try
{
    await client.SendAsync(paymentEx,
        new CaptureOptions { Url = "/api/pay", Level = ElsLevel.Critical });
}
catch (ElsSendException ex) when (ex.IsRetryable)
{
    // 5xx / 429 / network — safe to retry from the caller
}
```

### Options pattern

`CaptureOptions` is a `record` with init-only properties; fluent helpers in
`CaptureOptionsExtensions` mirror the `WithX(...)` style used by the Go SDK.

```csharp
var opts = new CaptureOptions()
    .WithUrl("/api/orders")
    .WithLevel(ElsLevel.Critical)
    .WithMetaItem("orderId", "ord_123")
    .WithCause(innerException);

client.CaptureError(ex, opts);
```

Available: `WithUrl`, `WithLevel`, `WithSource`, `WithStack`,
`WithComponentStack`, `WithUserAgent`, `WithLanguage`, `WithReferrer`,
`WithSessionId`, `WithServiceName`, `WithAppVersion`, `WithHttpStatus`,
`WithDuration`, `WithMeta`, `WithMetaItem`, `WithCause`. ASP.NET Core adds
`WithHttpContext`, `WithHttpRequest`.

## Features

- **Async batching** with configurable size and interval.
- **Retry with exponential backoff** including jitter, plus full respect for
  `Retry-After` (delta-seconds and HTTP-date).
- **Disk buffering** when the server is unreachable. Format is byte-compatible
  with the Go SDK (`.els-buffer.jsonl`) so existing on-disk buffers can be read
  by either SDK.
- **Sampling and `MinLevel` filtering**. Critical-level entries are never
  sampled out.
- **User context**. `client.User = new UserContext { ... }` enriches every
  subsequent capture with `user.id`, `user.email`, `user.name`, and each
  `Extra` key under `user.<k>`.
- **`BeforeSend` / `OnError` hooks**.
- **Health check** (`HealthAsync`).
- **Stats** (`client.Stats`, `client.QueueSize`).
- **Typed errors**: `ElsSendException.IsRetryable` distinguishes transient
  failures from permanent ones. `Sdk.IsRetryable(ex)` walks
  `AggregateException` / `InnerException` chains.
- **Graceful shutdown**: `DisposeAsync()` drains the queue, attempts one final
  send, and writes anything that didn't make it to the disk buffer.

## Configuration

```csharp
new ElsOptions
{
    // Required
    Endpoint = "https://api.insoweb.ru/els",
    ApiKey   = "...",

    // Identity (recommended)
    AppSlug = "my-service",
    DeploymentEnv = "PRODUCTION",
    ServiceName = "api-gateway",
    AppVersion = "1.2.3",

    // Batching
    BatchSize = 50,
    BatchInterval = TimeSpan.FromSeconds(5),
    BufferSize = 1000,

    // Retry / timeouts
    MaxRetries = 3,
    RetryBaseDelay = TimeSpan.FromSeconds(1),
    Timeout = TimeSpan.FromSeconds(10),
    FlushTimeout = TimeSpan.FromSeconds(10),

    // Disk buffer
    BufferDir = null,           // null = Path.GetTempPath()
    MaxBufferFileSize = 100L * 1024 * 1024,

    // Filtering / sampling
    MinLevel = ElsLevel.Warning,
    SampleRate = 1.0,

    // Hooks
    BeforeSend = entry => entry, // return null to drop
    OnError = ex => Console.Error.WriteLine(ex),

    // Advanced
    AuthScheme = ElsAuthScheme.Bearer,  // or ApiKeyHeader
    HttpClient = null,                   // bring your own
    Debug = false,
}
```

## Examples

- [Basic (EN)](examples/en/Basic) / [Basic (RU)](examples/ru/Basic)
- [ASP.NET Core middleware (EN)](examples/en/Middleware) / [(RU)](examples/ru/Middleware)
- [ILogger integration (EN)](examples/en/Logging) / [(RU)](examples/ru/Logging)

## Field reference

See [docs/FIELDS.md](docs/FIELDS.md) (EN) and [docs/FIELDS_RU.md](docs/FIELDS_RU.md).

## License

MIT
