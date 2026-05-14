// Static facade: ambient client via Inso.Els.Sdk for console scripts and
// glue code where dependency injection would be overkill.

using Inso.Els;

Sdk.Init(new ElsOptions
{
    Endpoint = "https://api.insoweb.ru/els",
    ApiKey = Environment.GetEnvironmentVariable("ELS_API_KEY") ?? "your-api-key",
    AppSlug = "static-facade-demo",
    DeploymentEnv = "DEVELOPMENT",
});

try
{
    DoUnitOfWork();
}
catch (Exception ex)
{
    // No client reference needed — Sdk.CaptureError routes to the ambient client.
    Sdk.CaptureError(ex, url: "/work", level: ElsLevel.Critical);
}

Sdk.CaptureMessage("script finished", ElsLevel.Info, url: "/");

await Sdk.FlushAsync();
await Sdk.CloseAsync();

static void DoUnitOfWork()
{
    throw new InvalidOperationException("synthetic failure for the example");
}
