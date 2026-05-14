// Microsoft.Extensions.Diagnostics.HealthChecks integration. Exposes
//   GET /health      — overall (includes ELS connectivity)
//   GET /health/els  — ELS only
// Use these endpoints from Kubernetes / your load-balancer.

using Inso.Els;
using Inso.Els.AspNetCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEls(opts =>
{
    opts.Endpoint = builder.Configuration["Els:Endpoint"] ?? "https://api.insoweb.ru/els";
    opts.ApiKey = builder.Configuration["Els:ApiKey"] ?? "your-api-key";
    opts.AppSlug = "health-demo";
});

builder.Services.AddHealthChecks()
    .AddEls(name: "els", failureStatus: HealthStatus.Degraded);

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/els", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Name == "els",
});

app.MapGet("/", () => "OK");

app.Run();
