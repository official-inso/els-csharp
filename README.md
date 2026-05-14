# Inso.Els — .NET SDK for ELS

[![NuGet — Inso.Els](https://img.shields.io/nuget/v/Inso.Els.svg?label=Inso.Els)](https://www.nuget.org/packages/Inso.Els)
[![NuGet — AspNetCore](https://img.shields.io/nuget/v/Inso.Els.AspNetCore.svg?label=Inso.Els.AspNetCore)](https://www.nuget.org/packages/Inso.Els.AspNetCore)
[![NuGet — Logging](https://img.shields.io/nuget/v/Inso.Els.Extensions.Logging.svg?label=Inso.Els.Extensions.Logging)](https://www.nuget.org/packages/Inso.Els.Extensions.Logging)
[![CI](https://github.com/official-inso/els-csharp/actions/workflows/ci.yml/badge.svg)](https://github.com/official-inso/els-csharp/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

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

## When to use `Sdk` vs dependency injection

| Scenario | Use |
|---|---|
| ASP.NET Core, Worker Services, anything that already has `IServiceCollection` | `services.AddEls(...)` — get `IElsClient` via constructor injection |
| Console scripts, one-off tools, sample / glue code, scenarios where DI is overkill | `Sdk.Init(opts)` + `Sdk.CaptureError(...)` |
| Libraries that should not impose lifetime / DI choices on consumers | Take `IElsClient` as a constructor parameter, let the host wire it up |

Both styles route through the same `ElsClient` — switching does not change wire format or behavior.

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

### Binding limits from `IConfiguration`

`services.AddEls(IConfiguration)` binds all primitive values (`Endpoint`,
`ApiKey`, batching, retry, sampling, `MinLevel`, `MaxBufferFileSize` byte
strings, etc.) and throws `ElsConfigurationException` on a malformed value.

**Delegate-typed options cannot be bound from `appsettings.json`** —
`BeforeSend`, `BeforeSendAsync`, `OnError`, and `HttpClient` have to be
provided programmatically. The cleanest pattern is to combine both forms:

```csharp
services.AddEls(builder =>
{
    // primitives from config
    builder.Endpoint = builder.Configuration["Els:Endpoint"]!;
    builder.ApiKey   = builder.Configuration["Els:ApiKey"]!;

    // delegates from code
    builder.OnError = ex => logger.LogWarning(ex, "ELS internal error");
    builder.BeforeSendAsync = async entry =>
    {
        await Task.Yield();
        return entry with { Message = pii.Redact(entry.Message) };
    };
});
```

You can also pre-validate options at startup:

```csharp
var issues = options.Validate();
if (issues.Count > 0) throw new InvalidOperationException(string.Join("; ", issues));
```

## Quick reference

| Need | Use |
|---|---|
| Quick console capture | `new ElsClient(endpoint, key, appSlug)` + `Sdk` facade |
| ASP.NET Core | `services.AddEls(...)` + `app.UseElsExceptionHandling()` |
| `Microsoft.Extensions.Logging` | `builder.Logging.AddEls(o => o.MinLevel = ElsLevel.Warning)` |
| Liveness probe | `services.AddHealthChecks().AddEls(timeout: TimeSpan.FromSeconds(2))` |
| Suppress 500 from the middleware | `app.UseElsExceptionHandling(o => o.Mode = ElsExceptionMode.CaptureAndHandle)` |
| `IOptions<>` binding | `services.Configure<ElsOptions>(section)` + `services.AddElsFromOptions()` |
| PII masking | `ElsOptions.BeforeSend` (sync) or `BeforeSendAsync` (I/O hook) |
| Sampling without losing criticals | `SampleRate = 0.1` (keeps `AlwaysCaptureCritical = true`) |
| Watch internal stats | `client.StatsChanged += (_, s) => ...` |

## Examples

- [Basic (EN)](examples/en/Basic) / [Basic (RU)](examples/ru/Basic)
- [ASP.NET Core middleware (EN)](examples/en/Middleware) / [(RU)](examples/ru/Middleware)
- [ILogger integration (EN)](examples/en/Logging) / [(RU)](examples/ru/Logging)
- [Minimal quick-start (EN)](examples/en/MinimalQuickstart) / [(RU)](examples/ru/MinimalQuickstart)
- [Static facade (EN)](examples/en/StaticFacade) / [(RU)](examples/ru/StaticFacade)
- [Health checks (EN)](examples/en/HealthChecks) / [(RU)](examples/ru/HealthChecks)
- [Worker (EN)](examples/en/Worker) / [(RU)](examples/ru/Worker)
- [Filtering (EN)](examples/en/Filtering) / [(RU)](examples/ru/Filtering)
- [Multi-tenant (EN)](examples/en/MultiTenant) / [(RU)](examples/ru/MultiTenant)
- [Custom HttpClient (EN)](examples/en/CustomHttpClient) / [(RU)](examples/ru/CustomHttpClient)
- [AOT console (EN)](examples/en/AotConsole) / [(RU)](examples/ru/AotConsole)
- [Polly resilience (EN)](examples/en/Resilience) / [(RU)](examples/ru/Resilience)

## Field reference

See [docs/FIELDS.md](docs/FIELDS.md) (EN) and [docs/FIELDS_RU.md](docs/FIELDS_RU.md).

## License

MIT
