# Inso.Els.AspNetCore

ASP.NET Core integration for [Inso.Els](https://www.nuget.org/packages/Inso.Els):

- Exception-capturing middleware (`UseElsExceptionHandling`).
- `IServiceCollection.AddEls(...)` for DI registration.
- `Microsoft.Extensions.Diagnostics.HealthChecks` integration (`AddEls()` with optional `timeout`).
- `IHostedService` that flushes the SDK on graceful shutdown.

![ELS dashboard — analytics & version regressions](https://raw.githubusercontent.com/official-inso/els-go/main/docs/screenshots/04-analytics-dashboard.png)

## Install

```bash
dotnet add package Inso.Els
dotnet add package Inso.Els.AspNetCore
```

or in `.csproj`:

```xml
<PackageReference Include="Inso.Els"            Version="0.2.1" />
<PackageReference Include="Inso.Els.AspNetCore" Version="0.2.1" />
```

## Quick start

```csharp
using Inso.Els;
using Inso.Els.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEls(opts =>
{
    opts.Endpoint = "https://api.insoweb.ru/els";
    opts.ApiKey   = builder.Configuration["Els:ApiKey"]!;
    opts.AppSlug  = "my-web-app";
});

var app = builder.Build();
app.UseElsExceptionHandling();
app.MapGet("/", () => "hello");
app.Run();
```

## Companion packages

- [`Inso.Els`](https://www.nuget.org/packages/Inso.Els) — the core SDK (required).
- [`Inso.Els.Extensions.Logging`](https://www.nuget.org/packages/Inso.Els.Extensions.Logging) — `ILoggerProvider` that routes `Microsoft.Extensions.Logging` records to ELS.

## Links

- Source & full documentation: <https://github.com/official-inso/els-csharp>
- NuGet: <https://www.nuget.org/packages/Inso.Els.AspNetCore>
- License: MIT

---

# Inso.Els.AspNetCore (RU)

Интеграция ASP.NET Core для [Inso.Els](https://www.nuget.org/packages/Inso.Els):

- Middleware, перехватывающий исключения (`UseElsExceptionHandling`).
- `IServiceCollection.AddEls(...)` для регистрации в DI.
- Интеграция с `Microsoft.Extensions.Diagnostics.HealthChecks` (`AddEls()` + опциональный `timeout`).
- `IHostedService`, сбрасывающий очередь SDK при корректном завершении.

## Установка

```bash
dotnet add package Inso.Els
dotnet add package Inso.Els.AspNetCore
```

или в `.csproj`:

```xml
<PackageReference Include="Inso.Els"            Version="0.2.1" />
<PackageReference Include="Inso.Els.AspNetCore" Version="0.2.1" />
```

## Быстрый старт

```csharp
using Inso.Els;
using Inso.Els.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEls(opts =>
{
    opts.Endpoint = "https://api.insoweb.ru/els";
    opts.ApiKey   = builder.Configuration["Els:ApiKey"]!;
    opts.AppSlug  = "my-web-app";
});

var app = builder.Build();
app.UseElsExceptionHandling();
app.MapGet("/", () => "hello");
app.Run();
```

## Сопутствующие пакеты

- [`Inso.Els`](https://www.nuget.org/packages/Inso.Els) — основной SDK (обязательно).
- [`Inso.Els.Extensions.Logging`](https://www.nuget.org/packages/Inso.Els.Extensions.Logging) — `ILoggerProvider`, маршрутизирующий записи `Microsoft.Extensions.Logging` в ELS.

## Ссылки

- Исходники и полная документация: <https://github.com/official-inso/els-csharp>
- NuGet: <https://www.nuget.org/packages/Inso.Els.AspNetCore>
- Лицензия: MIT
