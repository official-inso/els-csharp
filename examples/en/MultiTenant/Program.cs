// Multi-tenant: per-request tenant id is added to every capture via
// CaptureOptions.WithMetaItem("tenantId", ...). The singleton ELS client is
// shared across tenants; tenant context is supplied per call (the client.User
// property is process-wide, so we don't mutate it from concurrent requests).

using Inso.Els;
using Inso.Els.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEls(opts =>
{
    opts.Endpoint = builder.Configuration["Els:Endpoint"] ?? "https://api.insoweb.ru/els";
    opts.ApiKey = builder.Configuration["Els:ApiKey"] ?? "your-api-key";
    opts.AppSlug = "multitenant-demo";
});

var app = builder.Build();
app.UseElsExceptionHandling(o =>
{
    // Enrich captures from the middleware with tenant id taken from the request header.
    o.OnException = (ex, ctx) =>
    {
        // No-op: example shows that you could update telemetry, mutate response, etc.
        return Task.CompletedTask;
    };
});

// Per-request capture with tenantId in meta.
app.MapPost("/tenants/{tenantId}/orders", (string tenantId, IElsClient els) =>
{
    try
    {
        // ... business logic ...
        throw new InvalidOperationException("downstream timeout");
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
