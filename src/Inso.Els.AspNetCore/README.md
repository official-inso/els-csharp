# Inso.Els.AspNetCore

ASP.NET Core integration for [Inso.Els](https://www.nuget.org/packages/Inso.Els):

- Exception-capturing middleware (`UseElsExceptionHandling`).
- `IServiceCollection.AddEls(...)` for DI registration.
- `IHostedService` that flushes the SDK on graceful shutdown.

![ELS dashboard — analytics & version regressions](https://raw.githubusercontent.com/official-inso/els-go/main/docs/screenshots/04-analytics-dashboard.png)

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

See the [project repository](https://github.com/official-inso/els-csharp) for full
documentation and examples.
