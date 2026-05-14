// Статический фасад: ambient-клиент через Inso.Els.Sdk для консольных
// скриптов и glue-кода, где dependency injection избыточен.

using Inso.Els;

Sdk.Init(new ElsOptions
{
    Endpoint = "https://api.insoweb.ru/els",
    ApiKey = Environment.GetEnvironmentVariable("ELS_API_KEY") ?? "ваш-api-ключ",
    AppSlug = "static-facade-demo",
    DeploymentEnv = "DEVELOPMENT",
});

try
{
    DoUnitOfWork();
}
catch (Exception ex)
{
    // Ссылка на клиент не нужна — Sdk.CaptureError направляет в ambient-клиент.
    Sdk.CaptureError(ex, url: "/work", level: ElsLevel.Critical);
}

Sdk.CaptureMessage("скрипт завершён", ElsLevel.Info, url: "/");

await Sdk.FlushAsync();
await Sdk.CloseAsync();

static void DoUnitOfWork()
{
    throw new InvalidOperationException("синтетический сбой для примера");
}
