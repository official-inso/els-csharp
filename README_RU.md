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

[English version](README.md)

## Что вы получаете

ELS из коробки даёт админ-панель. Все события, отправленные из вашего .NET
приложения, попадают сюда — с полнотекстовым поиском, фасетной фильтрацией,
AI-диагностикой и обнаружением регрессий по версиям.

| | |
|---|---|
| ![Список логов](https://raw.githubusercontent.com/official-inso/els-go/main/docs/screenshots/01-error-logs-list.png) | ![Карточка события](https://raw.githubusercontent.com/official-inso/els-go/main/docs/screenshots/02-event-detail-info.png) |
| Виртуальная таблица с сайдбаром фильтров (приложение, окружение, **версия**, источник, уровень, браузер, IP, категория). | Полные метаданные события: время, гео, окружение, **версия приложения**, fingerprint, session. |
| ![AI-диагностика](https://raw.githubusercontent.com/official-inso/els-go/main/docs/screenshots/03-error-detail-ai.png) | ![Аналитика](https://raw.githubusercontent.com/official-inso/els-go/main/docs/screenshots/04-analytics-dashboard.png) |
| Stack trace + AI-анализ: что сломалось, где, как чинить. | Хронология, donut'ы, виджет **«Регрессии по версиям»**. |
| ![API-ключи](https://raw.githubusercontent.com/official-inso/els-go/main/docs/screenshots/05-api-keys.png) | ![Избранные](https://raw.githubusercontent.com/official-inso/els-go/main/docs/screenshots/07-favorites.png) |
| Scoped API-ключи (write / read / read-any), окружения live и test, ротация. | Закладки по trace ID — сохраняются между сессиями. |

## Пакеты

| Пакет | Что внутри |
|---|---|
| `Inso.Els` | Основной SDK: клиент, опции, batching-worker, transport, disk-buffer. |
| `Inso.Els.AspNetCore` | Middleware для ASP.NET Core + DI-расширения. |
| `Inso.Els.Extensions.Logging` | `ILoggerProvider`, направляющий записи в ELS. |

## Установка

```bash
dotnet add package Inso.Els
dotnet add package Inso.Els.AspNetCore           # опционально
dotnet add package Inso.Els.Extensions.Logging   # опционально
```

Target frameworks:

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

## Примеры

- [Базовый (EN)](examples/en/Basic) / [Базовый (RU)](examples/ru/Basic)
- [Middleware для ASP.NET Core (EN)](examples/en/Middleware) / [(RU)](examples/ru/Middleware)
- [ILogger (EN)](examples/en/Logging) / [(RU)](examples/ru/Logging)

## Справочник полей

См. [docs/FIELDS_RU.md](docs/FIELDS_RU.md) (RU) и [docs/FIELDS.md](docs/FIELDS.md) (EN).

## Лицензия

MIT
