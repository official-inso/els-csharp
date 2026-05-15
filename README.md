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

> 🇷🇺 [Русская версия → README_RU.md](README_RU.md) &nbsp;•&nbsp; 📚 [SDKs overview → ../README.md](../README.md)

## What you get

Every event lands in the built-in dashboard with full-text search, faceted filtering, AI-assisted diagnosis, and a regressions-by-version widget.

![ELS dashboard preview](https://raw.githubusercontent.com/official-inso/els-go/main/docs/screenshots/01-error-logs-list.png)

→ **[Full UI tour with all 4 screenshots](../README.md#what-you-get)**

Don't have an API key yet? **[Sign up at lk.insoweb.ru](https://lk.insoweb.ru)** — takes under a minute.

## Packages

All three packages are published on [nuget.org](https://www.nuget.org/profiles/inso) under the `Inso.*` prefix.

| Package | What it is | NuGet |
|---|---|---|
| `Inso.Els` | Core SDK: client, options, batching worker, transport, disk buffer. | [nuget.org/packages/Inso.Els](https://www.nuget.org/packages/Inso.Els) |
| `Inso.Els.AspNetCore` | ASP.NET Core middleware + DI extensions. | [nuget.org/packages/Inso.Els.AspNetCore](https://www.nuget.org/packages/Inso.Els.AspNetCore) |
| `Inso.Els.Extensions.Logging` | `ILoggerProvider` that routes log records to ELS. | [nuget.org/packages/Inso.Els.Extensions.Logging](https://www.nuget.org/packages/Inso.Els.Extensions.Logging) |

## Install

### .NET CLI

```bash
dotnet add package Inso.Els
dotnet add package Inso.Els.AspNetCore           # optional, for ASP.NET Core
dotnet add package Inso.Els.Extensions.Logging   # optional, for ILogger
```

### Package Manager Console (Visual Studio)

```powershell
Install-Package Inso.Els
Install-Package Inso.Els.AspNetCore
Install-Package Inso.Els.Extensions.Logging
```

### `PackageReference` in `.csproj`

```xml
<ItemGroup>
  <PackageReference Include="Inso.Els"                       Version="0.2.1" />
  <PackageReference Include="Inso.Els.AspNetCore"            Version="0.2.1" />
  <PackageReference Include="Inso.Els.Extensions.Logging"    Version="0.2.1" />
</ItemGroup>
```

### Paket

```
nuget Inso.Els
nuget Inso.Els.AspNetCore
nuget Inso.Els.Extensions.Logging
```

### Target frameworks

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

## Migration

### From Serilog

**Before:**

```csharp
using Serilog;
using Serilog.Sinks.Http;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Http("https://logs.example.com/ingest", queueLimitBytes: null)
    .CreateLogger();

Log.Information("User {UserId} logged in", 42);
Log.Error(ex, "Payment failed");
```

**After:**

```csharp
using Inso.Els;

await using var client = new ElsClient(new ElsOptions
{
    Endpoint = "https://api.insoweb.ru/els",
    ApiKey   = Environment.GetEnvironmentVariable("ELS_API_KEY")!,
    AppSlug  = "my-service",
    MinLevel = ElsLevel.Info,
});

client.CaptureMessage("User logged in", ElsLevel.Info,
    new CaptureOptions().WithMetaItem("userId", "42"));
client.CaptureError(ex, new CaptureOptions { Url = "/api/pay", Level = ElsLevel.Error });
```

If you want to keep `ILogger<T>` call sites unchanged, plug in the `Inso.Els.Extensions.Logging` provider — see Quick start.

| Serilog | ELS | Notes |
|---|---|---|
| `WriteTo.X(...)` sinks | Built-in HTTP transport | One destination, no sink packages |
| `Log.Information(template, args)` | `client.CaptureMessage(...)` or `ILogger.LogInformation` via the provider | Message templates → meta items |
| `Enrich.FromLogContext()` | `client.User = ...` + `CaptureOptions.WithMeta...` | |
| `WriteTo.File("logs.txt", rollingInterval: ...)` | Server-side retention | File rotation is not in scope |
| Structured properties (`{UserId}`) | `CaptureOptions.WithMetaItem("userId", "...")` | Or rely on `Microsoft.Extensions.Logging` formatter |

**Gotchas:**

- Serilog's destructuring (`{@User}`) is not in scope — flatten objects via `WithMetaItem` or `WithMeta`.
- Pretty-console output is not the SDK's concern — pair with `Microsoft.Extensions.Logging`'s `Console` provider for local dev.

---

### From NLog

**Before:**

```xml
<!-- NLog.config -->
<targets>
  <target xsi:type="Network" name="net" address="tcp://logs.example.com:514" />
</targets>
<rules>
  <logger name="*" minlevel="Info" writeTo="net" />
</rules>
```

```csharp
using NLog;
private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
Logger.Info("User {0} logged in", userId);
Logger.Error(ex, "Payment failed");
```

**After:**

```csharp
using Inso.Els;
using Inso.Els.Extensions.Logging;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection()
    .AddLogging(b => b.AddEls(o =>
    {
        o.Endpoint = "https://api.insoweb.ru/els";
        o.ApiKey   = Environment.GetEnvironmentVariable("ELS_API_KEY")!;
        o.AppSlug  = "my-service";
        o.MinLevel = ElsLevel.Info;
    }))
    .BuildServiceProvider();

var logger = services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("User {UserId} logged in", userId);
logger.LogError(ex, "Payment failed");
```

| NLog | ELS | Notes |
|---|---|---|
| `LogManager.GetCurrentClassLogger()` | `ILogger<T>` via DI | Standard `Microsoft.Extensions.Logging` |
| Targets in `NLog.config` | Built-in HTTP transport | One destination, no XML config |
| Layout renderers (`${aspnet-...}`) | `CaptureOptions.WithHttpContext(...)` | Use the `Inso.Els.AspNetCore` package |
| `minlevel="Info"` | `o.MinLevel = ElsLevel.Info` | Same idea |

**Gotchas:**

- NLog's file targets / archives have no equivalent — retention lives server-side.
- Async wrappers (`AsyncTargetWrapper`) are unnecessary — the ELS SDK is async by default.

---

### From log4net

**Before:**

```xml
<!-- log4net.config -->
<appender name="Http" type="log4net.Appender.HttpAppender">
  <url value="https://logs.example.com/ingest" />
</appender>
<root>
  <level value="INFO" />
  <appender-ref ref="Http" />
</root>
```

```csharp
private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));
Logger.Info("User logged in");
Logger.Error("Payment failed", ex);
```

**After:**

```csharp
using Microsoft.Extensions.Logging;
using Inso.Els.Extensions.Logging;

services.AddLogging(b => b.AddEls(o =>
{
    o.Endpoint = "https://api.insoweb.ru/els";
    o.ApiKey   = "...";
    o.AppSlug  = "my-service";
    o.MinLevel = ElsLevel.Info;
}));

// At call site
logger.LogInformation("User logged in");
logger.LogError(ex, "Payment failed");
```

| log4net | ELS | Notes |
|---|---|---|
| `LogManager.GetLogger(typeof(T))` | `ILogger<T>` via DI | Modern .NET idiom |
| XML appenders | Code-based `AddEls(...)` | One destination |
| `%property{X}` patterns | `CaptureOptions.WithMetaItem("x", ...)` | |
| Rolling file appender | n/a | Retention server-side |

**Gotchas:**

- Migrating to `Microsoft.Extensions.Logging` first lets you keep the rest of your stack untouched — then `AddEls(...)` is a one-liner.
- log4net's MDC (`ThreadContext`) maps to `CaptureOptions.WithMeta(...)` per call or `client.User` for stable values.

---

### From Sentry.NET

**Before:**

```csharp
using SentrySdk = Sentry.SentrySdk;
using SentryOptions = Sentry.SentryOptions;

using (SentrySdk.Init(o =>
{
    o.Dsn = "https://public@sentry.example.com/1";
    o.Environment = "production";
    o.Release = "1.2.3";
}))
{
    SentrySdk.CaptureException(ex);
    SentrySdk.CaptureMessage("payment timeout", SentryLevel.Warning);
    SentrySdk.ConfigureScope(s => s.User = new Sentry.User { Id = "42" });
}
```

**After:**

```csharp
using Inso.Els;

await using var client = new ElsClient(new ElsOptions
{
    Endpoint = "https://api.insoweb.ru/els",
    ApiKey   = Environment.GetEnvironmentVariable("ELS_API_KEY")!,
    AppSlug  = "my-service",
    DeploymentEnv = "PRODUCTION",
    AppVersion = "1.2.3",
});

client.User = new UserContext { Id = "42" };
client.CaptureError(ex);
client.CaptureMessage("payment timeout", ElsLevel.Warning);
```

| Sentry | ELS | Notes |
|---|---|---|
| `Dsn` | `Endpoint` + `ApiKey` + `AppSlug` | Three explicit fields |
| `Environment` | `DeploymentEnv` | Fixed enum |
| `Release` | `AppVersion` | Any string ≤128 chars |
| `CaptureException(ex)` | `client.CaptureError(ex)` | |
| `CaptureMessage(msg, level)` | `client.CaptureMessage(msg, level)` | |
| `ConfigureScope(s => s.User = ...)` | `client.User = new UserContext { ... }` | |
| `BeforeSend` | `ElsOptions.BeforeSend` / `BeforeSendAsync` | |
| Source maps / `SymbolicateException` | Not provided | Keep Sentry alongside if critical |
| Performance / tracing | Not provided | ELS focuses on logging |

**Gotchas:**

- Sentry's `SentryHttpMessageHandler` and ASP.NET Core middleware bundle tracing — drop them when you switch.
- For ASP.NET Core capture parity, use `app.UseElsExceptionHandling()` from `Inso.Els.AspNetCore`.

---

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

## Why ELS

ELS for .NET is a focused logging SaaS, not a full observability suite. It optimises for capture speed, AI-driven triage, and a low integration cost.

- **Lower weight.** A single `netstandard2.0` assembly, no transitive deps.
- **Zero external API calls.** Only `POST /errors[/batch]` and `GET /health`.
- **AI-assisted diagnosis** on every stack trace, out of the box — no add-ons.
- **5-minute integration.** `dotnet add package`, set the API key, you're done.
- **Predictable price.** Tariffs live in your personal cabinet.

| Feature | ELS | Sentry | Datadog | Loki | LogRocket |
|---|---|---|---|---|---|
| AI on stack traces | Built-in | Paid add-on | Paid add-on | None | None |
| Zero-dep SDK | Yes | No | No | No | No |
| Free tier retention | 24h | 30d (limited) | Trial only | Self-cost | 3–30d |
| Setup time | ~5 min | 10–20 min | 30–60 min | Hours | 10–20 min |

ELS does **not** ship full APM / tracing, source-map upload, session replay, frontend RUM, or infra metrics. Pair ELS with Grafana / Datadog or stay on Sentry if you need them.

→ **Sign up at [lk.insoweb.ru](https://lk.insoweb.ru)** to grab an API key.

## Other ELS SDKs

Same wire format, same dashboard — pick by stack.

**.NET** (this repo)
- `Inso.Els` — Core SDK
- `Inso.Els.AspNetCore` — ASP.NET Core middleware
- `Inso.Els.Extensions.Logging` — `ILoggerProvider`

**Node.js family**
- [`@inso_web/els-client`](../js/README.md) — base TS / Node / browser client
- [`@inso_web/els-express`](../express/README.md) — Express middleware
- [`@inso_web/els-next`](../next/README.md) — Next.js helpers
- [`@inso_web/els-nest`](../nest/README.md) — NestJS module
- [`@inso_web/els-react`](../react/README.md) — React Provider, hooks, ErrorBoundary
- [`@inso_web/els-vue`](../vue/README.md) — Vue 3 plugin

**Other stacks**
- [`io.github.official-inso:els-core`](../java/README.md) — Java + Spring Boot starter + SLF4J
- [`github.com/official-inso/els-go`](../els-go/README.md) — Go

→ **Full overview & comparison:** [../README.md](../README.md) · [github.com/official-inso/els-go/blob/main/sdks/README.md](https://github.com/official-inso/els-go/blob/main/sdks/README.md)

## Pricing

Free tier — **24-hour log retention**. See **[lk.insoweb.ru](https://lk.insoweb.ru)** for the full tariff matrix.

## License

MIT
