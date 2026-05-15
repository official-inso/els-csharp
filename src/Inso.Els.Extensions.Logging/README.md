# Inso.Els.Extensions.Logging

`Microsoft.Extensions.Logging` provider for [Inso.Els](https://www.nuget.org/packages/Inso.Els).
Routes `ILogger` calls to ELS so existing logging code automatically captures
errors without rewrites.

![ELS dashboard — event detail with AI diagnosis](https://raw.githubusercontent.com/official-inso/els-go/main/docs/screenshots/03-error-detail-ai.png)

## Install

```bash
dotnet add package Inso.Els
dotnet add package Inso.Els.Extensions.Logging
```

or in `.csproj`:

```xml
<PackageReference Include="Inso.Els"                    Version="0.2.1" />
<PackageReference Include="Inso.Els.Extensions.Logging" Version="0.2.1" />
```

## Quick start

```csharp
using Inso.Els;
using Inso.Els.Extensions.Logging;
using Inso.Els.AspNetCore; // for services.AddEls()

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEls(opts =>
{
    opts.Endpoint = builder.Configuration["Els:Endpoint"]!;
    opts.ApiKey   = builder.Configuration["Els:ApiKey"]!;
});

builder.Logging.AddEls(o => o.MinLevel = ElsLevel.Warning);

// Existing logger calls automatically end up in ELS:
//   _logger.LogError(ex, "user {UserId} failed to load", 42);
```

## Companion packages

- [`Inso.Els`](https://www.nuget.org/packages/Inso.Els) — the core SDK (required).
- [`Inso.Els.AspNetCore`](https://www.nuget.org/packages/Inso.Els.AspNetCore) — exception-capturing middleware + DI.

## Links

- Source & full documentation: <https://github.com/official-inso/els-csharp>
- NuGet: <https://www.nuget.org/packages/Inso.Els.Extensions.Logging>
- License: MIT
