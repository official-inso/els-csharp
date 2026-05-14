// Минимальный quick-start: кратчайшая интеграция ELS.

using Inso.Els;

await using var client = new ElsClient(
    endpoint: "https://api.insoweb.ru/els",
    apiKey: Environment.GetEnvironmentVariable("ELS_API_KEY") ?? "ваш-api-ключ",
    appSlug: "quickstart");

client.CaptureMessage("привет из .NET", ElsLevel.Info, url: "/");

await client.FlushAsync();
