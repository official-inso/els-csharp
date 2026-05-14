// Multi-tenant: per-request tenantId добавляется в каждый capture через
// CaptureOptions.WithMetaItem("tenantId", ...). Singleton-клиент ELS общий
// для всех tenant'ов; контекст передаётся per-call (client.User —
// процесс-глобальный, поэтому конкурентно мы его не трогаем).

using Inso.Els;
using Inso.Els.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEls(opts =>
{
    opts.Endpoint = builder.Configuration["Els:Endpoint"] ?? "https://api.insoweb.ru/els";
    opts.ApiKey = builder.Configuration["Els:ApiKey"] ?? "ваш-api-ключ";
    opts.AppSlug = "multitenant-demo";
});

var app = builder.Build();
app.UseElsExceptionHandling(o =>
{
    o.OnException = (ex, ctx) =>
    {
        // No-op: пример показывает, что здесь можно поднять метрики, изменить ответ и т.п.
        return Task.CompletedTask;
    };
});

// Per-request захват с tenantId в meta.
app.MapPost("/tenants/{tenantId}/orders", (string tenantId, IElsClient els) =>
{
    try
    {
        // ... бизнес-логика ...
        throw new InvalidOperationException("таймаут downstream-сервиса");
    }
    catch (Exception ex)
    {
        els.CaptureError(ex,
            url: $"POST /tenants/{tenantId}/orders",
            level: ElsLevel.Error,
            meta: new Dictionary<string, object?>
            {
                ["tenantId"] = tenantId,
                ["feature"] = "orders",
            });
        return Results.StatusCode(500);
    }
});

app.MapGet("/", () => "OK");

app.Run();
