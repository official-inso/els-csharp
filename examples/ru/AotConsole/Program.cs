// Консольный пример, дружественный к Native AOT.
//
// Обычный запуск: `dotnet run --project examples/ru/AotConsole`.
//
// Native AOT publish:
//   dotnet publish examples/ru/AotConsole -c Release -p:PublishAot=true -r osx-arm64 --self-contained
// Замечание: в v0.2 SDK использует reflection-based JSON для поля `Meta`
// (полиморфные object?-значения). Native AOT publish даст warnings по
// Inso.Els; этот демо-код использует только примитивный payload, чтобы
// собраться end-to-end. Полная AOT-совместимость — в roadmap
// (docs/FUTURE_IMPROVEMENTS.md).

using Inso.Els;

await using var client = new ElsClient(
    endpoint: "https://api.insoweb.ru/els",
    apiKey: Environment.GetEnvironmentVariable("ELS_API_KEY") ?? "ваш-api-ключ",
    appSlug: "aot-demo");

client.CaptureMessage("aot smoke test", ElsLevel.Info, url: "/aot");

await client.FlushAsync();
Console.WriteLine("готово");
