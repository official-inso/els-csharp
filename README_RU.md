# Inso.Els — .NET SDK для ELS

[![NuGet — Inso.Els](https://img.shields.io/nuget/v/Inso.Els.svg?label=Inso.Els)](https://www.nuget.org/packages/Inso.Els)
[![NuGet — AspNetCore](https://img.shields.io/nuget/v/Inso.Els.AspNetCore.svg?label=Inso.Els.AspNetCore)](https://www.nuget.org/packages/Inso.Els.AspNetCore)
[![NuGet — Logging](https://img.shields.io/nuget/v/Inso.Els.Extensions.Logging.svg?label=Inso.Els.Extensions.Logging)](https://www.nuget.org/packages/Inso.Els.Extensions.Logging)
[![CI](https://github.com/official-inso/els-csharp/actions/workflows/ci.yml/badge.svg)](https://github.com/official-inso/els-csharp/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

.NET SDK для **сервиса логирования ошибок (ELS — Error Logs Service)**.
Асинхронный батчинг, retry с экспоненциальным backoff, буферизация на диск,
middleware для ASP.NET Core, провайдер для `Microsoft.Extensions.Logging`.
Wire-формат совместим с [Go SDK](https://github.com/official-inso/els-go).

> 🇬🇧 [English version → README.md](README.md) &nbsp;•&nbsp; 📚 [Обзор всех SDK → ../README_RU.md](../README_RU.md)

## Что вы получаете

Каждое событие попадает во встроенную панель с полнотекстовым поиском, фасетной фильтрацией, AI-диагностикой и виджетом регрессий по версиям.

![Превью панели ELS](https://raw.githubusercontent.com/official-inso/els-go/main/docs/screenshots/01-error-logs-list.png)

→ **[Полный обзор UI с 4 скриншотами](../README_RU.md#что-вы-получаете)**

Ещё нет API-ключа? **[Зарегистрируйтесь на lk.insoweb.ru](https://lk.insoweb.ru)** — займёт минуту.

## Пакеты

Все три пакета опубликованы на [nuget.org](https://www.nuget.org/profiles/inso) с префиксом `Inso.*`.

| Пакет | Что внутри | NuGet |
|---|---|---|
| `Inso.Els` | Основной SDK: клиент, опции, batching-worker, transport, disk-buffer. | [nuget.org/packages/Inso.Els](https://www.nuget.org/packages/Inso.Els) |
| `Inso.Els.AspNetCore` | Middleware для ASP.NET Core + DI-расширения. | [nuget.org/packages/Inso.Els.AspNetCore](https://www.nuget.org/packages/Inso.Els.AspNetCore) |
| `Inso.Els.Extensions.Logging` | `ILoggerProvider`, направляющий записи в ELS. | [nuget.org/packages/Inso.Els.Extensions.Logging](https://www.nuget.org/packages/Inso.Els.Extensions.Logging) |

## Установка

### .NET CLI

```bash
dotnet add package Inso.Els
dotnet add package Inso.Els.AspNetCore           # опционально, для ASP.NET Core
dotnet add package Inso.Els.Extensions.Logging   # опционально, для ILogger
```

### Package Manager Console (Visual Studio)

```powershell
Install-Package Inso.Els
Install-Package Inso.Els.AspNetCore
Install-Package Inso.Els.Extensions.Logging
```

### `PackageReference` в `.csproj`

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

## Быстрый старт

```csharp
using Inso.Els;

await using var client = new ElsClient(new ElsOptions
{
    Endpoint = "https://api.insoweb.ru/els",
    ApiKey   = "ваш-api-ключ",
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
app.UseElsExceptionHandling(); // ловит необработанные исключения
app.MapGet("/", () => "OK");
app.Run();
```

### Microsoft.Extensions.Logging

```csharp
using Inso.Els.Extensions.Logging;

builder.Logging.AddEls(o => o.MinLevel = ElsLevel.Warning);

// Существующий код продолжает работать как обычно:
_logger.LogError(ex, "пользователь {UserId} не найден", 42);
```

## Когда использовать `Sdk`, а когда DI

| Сценарий | Что использовать |
|---|---|
| ASP.NET Core, Worker Services и всё, где уже есть `IServiceCollection` | `services.AddEls(...)` — `IElsClient` приходит через constructor injection |
| Консольные скрипты, разовые утилиты, glue-код, ситуации где DI избыточен | `Sdk.Init(opts)` + `Sdk.CaptureError(...)` |
| Библиотеки, которые не должны навязывать DI и lifetime потребителю | Принимать `IElsClient` параметром конструктора, host сам его подключит |

Оба пути идут через один `ElsClient` — переключение не меняет wire-формат или поведение.

## Основные концепции

### Async vs sync

Захват — **асинхронный**, возвращает управление сразу, SDK отправляет батчи в
фоне:

```csharp
client.CaptureError(ex, new CaptureOptions { Url = "/api" });   // неблокирующий
client.CaptureMessage("started", ElsLevel.Info);                // неблокирующий
```

Для критических ошибок, где нужна **гарантия доставки** до продолжения, есть
`SendAsync`:

```csharp
try
{
    await client.SendAsync(paymentEx,
        new CaptureOptions { Url = "/api/pay", Level = ElsLevel.Critical });
}
catch (ElsSendException ex) when (ex.IsRetryable)
{
    // 5xx / 429 / сеть — можно безопасно повторить попытку
}
```

### Паттерн опций

`CaptureOptions` — это `record` с init-only свойствами; fluent-хелперы в
`CaptureOptionsExtensions` повторяют стиль `WithX(...)` из Go SDK.

```csharp
var opts = new CaptureOptions()
    .WithUrl("/api/orders")
    .WithLevel(ElsLevel.Critical)
    .WithMetaItem("orderId", "ord_123")
    .WithCause(innerException);

client.CaptureError(ex, opts);
```

Доступно: `WithUrl`, `WithLevel`, `WithSource`, `WithStack`,
`WithComponentStack`, `WithUserAgent`, `WithLanguage`, `WithReferrer`,
`WithSessionId`, `WithServiceName`, `WithAppVersion`, `WithHttpStatus`,
`WithDuration`, `WithMeta`, `WithMetaItem`, `WithCause`. В пакете ASP.NET Core
дополнительно — `WithHttpContext`, `WithHttpRequest`.

## Возможности

- **Асинхронный батчинг** с настраиваемым размером и интервалом.
- **Retry с экспоненциальным backoff** и jitter, полное соблюдение
  `Retry-After` (delta-seconds и HTTP-date).
- **Disk buffering** при недоступности сервера. Формат байт-в-байт совместим с
  Go SDK (`.els-buffer.jsonl`), так что один и тот же файл могут читать оба SDK.
- **Sampling и фильтрация по `MinLevel`**. Critical-уровень никогда не
  отбрасывается сэмплингом.
- **User context**. `client.User = new UserContext { ... }` добавляет в каждую
  запись `user.id`, `user.email`, `user.name` и ключи `Extra` под `user.<k>`.
- **Хуки `BeforeSend` / `OnError`**.
- **Health check** (`HealthAsync`).
- **Статистика** (`client.Stats`, `client.QueueSize`).
- **Типизированные ошибки**: `ElsSendException.IsRetryable` отделяет временные
  сбои от постоянных. `Sdk.IsRetryable(ex)` разворачивает
  `AggregateException` / `InnerException`.
- **Корректное завершение**: `DisposeAsync()` дренирует очередь, делает
  финальную попытку отправки, остаток пишет в disk buffer.

## Конфигурация

```csharp
new ElsOptions
{
    // Обязательно
    Endpoint = "https://api.insoweb.ru/els",
    ApiKey   = "...",

    // Идентификация (рекомендуется)
    AppSlug = "my-service",
    DeploymentEnv = "PRODUCTION",
    ServiceName = "api-gateway",
    AppVersion = "1.2.3",

    // Батчинг
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

    // Фильтрация / сэмплинг
    MinLevel = ElsLevel.Warning,
    SampleRate = 1.0,

    // Хуки
    BeforeSend = entry => entry, // вернуть null чтобы отбросить запись
    OnError = ex => Console.Error.WriteLine(ex),

    // Дополнительно
    AuthScheme = ElsAuthScheme.Bearer,  // или ApiKeyHeader
    HttpClient = null,                   // свой HttpClient
    Debug = false,
}
```

### Ограничения биндинга из `IConfiguration`

`services.AddEls(IConfiguration)` биндит все примитивы (`Endpoint`, `ApiKey`,
batching, retry, sampling, `MinLevel`, `MaxBufferFileSize` строкой `"100MB"`,
и т.д.) и бросает `ElsConfigurationException` на некорректное значение.

**Делегаты нельзя связать из `appsettings.json`** — `BeforeSend`,
`BeforeSendAsync`, `OnError`, `HttpClient` нужно задавать кодом. Чистый
паттерн — комбинировать оба способа:

```csharp
services.AddEls(builder =>
{
    // примитивы из конфига
    builder.Endpoint = builder.Configuration["Els:Endpoint"]!;
    builder.ApiKey   = builder.Configuration["Els:ApiKey"]!;

    // делегаты из кода
    builder.OnError = ex => logger.LogWarning(ex, "ELS internal error");
    builder.BeforeSendAsync = async entry =>
    {
        await Task.Yield();
        return entry with { Message = pii.Redact(entry.Message) };
    };
});
```

Опции можно прелинтовать на старте:

```csharp
var issues = options.Validate();
if (issues.Count > 0) throw new InvalidOperationException(string.Join("; ", issues));
```

## Шпаргалка

| Что нужно | Как |
|---|---|
| Быстрый захват в консоли | `new ElsClient(endpoint, key, appSlug)` + `Sdk` facade |
| ASP.NET Core | `services.AddEls(...)` + `app.UseElsExceptionHandling()` |
| `Microsoft.Extensions.Logging` | `builder.Logging.AddEls(o => o.MinLevel = ElsLevel.Warning)` |
| Liveness probe | `services.AddHealthChecks().AddEls(timeout: TimeSpan.FromSeconds(2))` |
| Без 500 от middleware | `app.UseElsExceptionHandling(o => o.Mode = ElsExceptionMode.CaptureAndHandle)` |
| `IOptions<>` биндинг | `services.Configure<ElsOptions>(section)` + `services.AddElsFromOptions()` |
| Маскирование PII | `ElsOptions.BeforeSend` (sync) или `BeforeSendAsync` (I/O hook) |
| Сэмплинг без потери critical | `SampleRate = 0.1` (с `AlwaysCaptureCritical = true`) |
| Подписаться на внутренние метрики | `client.StatsChanged += (_, s) => ...` |

## Миграция

### С Serilog

**Было:**

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

**Стало:**

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

Чтобы оставить call-сайты на `ILogger<T>` нетронутыми — подключите провайдер `Inso.Els.Extensions.Logging` (см. Быстрый старт).

| Serilog | ELS | Заметки |
|---|---|---|
| `WriteTo.X(...)` sinks | Встроенный HTTP-транспорт | Одна цель, без sink-пакетов |
| `Log.Information(template, args)` | `client.CaptureMessage(...)` или `ILogger.LogInformation` через провайдер | Шаблоны → meta-поля |
| `Enrich.FromLogContext()` | `client.User = ...` + `CaptureOptions.WithMeta...` | |
| `WriteTo.File("logs.txt", ...)` | Server-side retention | Ротация файлов — вне scope |
| Структурированные свойства (`{UserId}`) | `CaptureOptions.WithMetaItem("userId", "...")` | Или через formatter `Microsoft.Extensions.Logging` |

**Подводные камни:**

- Destructuring Serilog (`{@User}`) аналога не имеет — flatten через `WithMetaItem` или `WithMeta`.
- Pretty-console — не задача SDK; для локального dev используйте `Console`-провайдер `Microsoft.Extensions.Logging`.

---

### С NLog

**Было:**

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

**Стало:**

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

| NLog | ELS | Заметки |
|---|---|---|
| `LogManager.GetCurrentClassLogger()` | `ILogger<T>` через DI | Стандартный `Microsoft.Extensions.Logging` |
| Targets в `NLog.config` | Встроенный HTTP-транспорт | Одна цель, без XML |
| Layout renderers (`${aspnet-...}`) | `CaptureOptions.WithHttpContext(...)` | Через `Inso.Els.AspNetCore` |
| `minlevel="Info"` | `o.MinLevel = ElsLevel.Info` | То же |

**Подводные камни:**

- File targets / архивы NLog аналога не имеют — retention на сервере.
- Async wrappers (`AsyncTargetWrapper`) не нужны — SDK async по умолчанию.

---

### С log4net

**Было:**

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

**Стало:**

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

logger.LogInformation("User logged in");
logger.LogError(ex, "Payment failed");
```

| log4net | ELS | Заметки |
|---|---|---|
| `LogManager.GetLogger(typeof(T))` | `ILogger<T>` через DI | Современная .NET-идиома |
| XML-appenders | Code-based `AddEls(...)` | Одна цель |
| `%property{X}` | `CaptureOptions.WithMetaItem("x", ...)` | |
| Rolling file appender | n/a | Retention на сервере |

**Подводные камни:**

- Сначала переход на `Microsoft.Extensions.Logging` оставляет остальной стек нетронутым — затем `AddEls(...)` в одну строчку.
- MDC (`ThreadContext`) ложится на `CaptureOptions.WithMeta(...)` per call или `client.User` для стабильных значений.

---

### С Sentry.NET

**Было:**

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

**Стало:**

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

| Sentry | ELS | Заметки |
|---|---|---|
| `Dsn` | `Endpoint` + `ApiKey` + `AppSlug` | Три явных поля |
| `Environment` | `DeploymentEnv` | Фиксированный enum |
| `Release` | `AppVersion` | Любая строка ≤128 |
| `CaptureException(ex)` | `client.CaptureError(ex)` | |
| `CaptureMessage(msg, level)` | `client.CaptureMessage(msg, level)` | |
| `ConfigureScope(s => s.User = ...)` | `client.User = new UserContext { ... }` | |
| `BeforeSend` | `ElsOptions.BeforeSend` / `BeforeSendAsync` | |
| Source maps / `SymbolicateException` | не предоставляется | Sentry оставляйте рядом, если критично |
| Performance / tracing | не предоставляется | ELS — про логи |

**Подводные камни:**

- `SentryHttpMessageHandler` и ASP.NET Core middleware в Sentry привязаны к tracing — отключайте при переходе.
- Для парности захвата в ASP.NET Core используйте `app.UseElsExceptionHandling()` из `Inso.Els.AspNetCore`.

---

## Примеры

- [Базовый (EN)](examples/en/Basic) / [Базовый (RU)](examples/ru/Basic)
- [Middleware для ASP.NET Core (EN)](examples/en/Middleware) / [(RU)](examples/ru/Middleware)
- [ILogger (EN)](examples/en/Logging) / [(RU)](examples/ru/Logging)
- [Минимальный quick-start (EN)](examples/en/MinimalQuickstart) / [(RU)](examples/ru/MinimalQuickstart)
- [Static facade (EN)](examples/en/StaticFacade) / [(RU)](examples/ru/StaticFacade)
- [Health checks (EN)](examples/en/HealthChecks) / [(RU)](examples/ru/HealthChecks)
- [Worker (EN)](examples/en/Worker) / [(RU)](examples/ru/Worker)
- [Фильтрация (EN)](examples/en/Filtering) / [(RU)](examples/ru/Filtering)
- [Multi-tenant (EN)](examples/en/MultiTenant) / [(RU)](examples/ru/MultiTenant)
- [Custom HttpClient (EN)](examples/en/CustomHttpClient) / [(RU)](examples/ru/CustomHttpClient)
- [AOT-консоль (EN)](examples/en/AotConsole) / [(RU)](examples/ru/AotConsole)
- [Polly resilience (EN)](examples/en/Resilience) / [(RU)](examples/ru/Resilience)

## Почему ELS

ELS для .NET — сфокусированный SaaS для логирования, а не observability-комбайн. Оптимизирован под скорость захвата, AI-диагностику и дешевизну интеграции.

- **Меньше веса.** Одна `netstandard2.0`-сборка, без транзитивных зависимостей.
- **Ноль внешних API.** Только `POST /errors[/batch]` и `GET /health`.
- **AI-диагностика** на каждом stack trace, из коробки — без аддонов.
- **5 минут интеграции.** `dotnet add package`, API-ключ — готово.
- **Прозрачные тарифы.** Цены в личном кабинете.

| Возможность | ELS | Sentry | Datadog | Loki | LogRocket |
|---|---|---|---|---|---|
| AI на stack-trace | Встроено | Платный аддон | Платный аддон | Нет | Нет |
| Zero-dep SDK | Да | Нет | Нет | Нет | Нет |
| Free-tier retention | 24ч | 30д (лимит) | Только триал | Self-cost | 3–30д |
| Время setup | ~5 мин | 10–20 мин | 30–60 мин | Часы | 10–20 мин |

ELS **не предоставляет**: full APM / tracing, source-map upload, session replay, frontend RUM, метрики инфраструктуры. Парьте с Grafana / Datadog или оставляйте Sentry, если критично.

→ **Регистрация на [lk.insoweb.ru](https://lk.insoweb.ru)** для API-ключа.

## Другие ELS SDK

Тот же wire-формат, та же панель — выбирайте по стеку.

**.NET** (этот репо)
- `Inso.Els` — основной SDK
- `Inso.Els.AspNetCore` — middleware для ASP.NET Core
- `Inso.Els.Extensions.Logging` — `ILoggerProvider`

**Node.js**
- [`@inso_web/els-client`](../js/README_RU.md) — базовый TS / Node / browser клиент
- [`@inso_web/els-express`](../express/README_RU.md) — Express middleware
- [`@inso_web/els-next`](../next/README_RU.md) — хелперы Next.js
- [`@inso_web/els-nest`](../nest/README_RU.md) — NestJS module
- [`@inso_web/els-react`](../react/README_RU.md) — React Provider, hooks, ErrorBoundary
- [`@inso_web/els-vue`](../vue/README_RU.md) — Vue 3 plugin

**Другие стеки**
- [`io.github.official-inso:els-core`](../java/README_RU.md) — Java + Spring Boot starter + SLF4J
- [`github.com/official-inso/els-go`](../els-go/README_RU.md) — Go

→ **Обзор и сравнение:** [../README_RU.md](../README_RU.md) · [github.com/official-inso/els-go/blob/main/sdks/README_RU.md](https://github.com/official-inso/els-go/blob/main/sdks/README_RU.md)

## Тарифы

Free-тариф — **хранение логов 24 часа**. Полный прайс на **[lk.insoweb.ru](https://lk.insoweb.ru)**.

## Справочник полей

См. [docs/FIELDS_RU.md](docs/FIELDS_RU.md) (RU) и [docs/FIELDS.md](docs/FIELDS.md) (EN).

## Лицензия

MIT
