// Minimal quick-start: the shortest possible ELS integration.

using Inso.Els;

await using var client = new ElsClient(
    endpoint: "https://api.insoweb.ru/els",
    apiKey: Environment.GetEnvironmentVariable("ELS_API_KEY") ?? "your-api-key",
    appSlug: "quickstart");

client.CaptureMessage("hello from .NET", ElsLevel.Info, url: "/");

await client.FlushAsync();
