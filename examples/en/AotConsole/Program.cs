// AOT-friendly console example.
//
// Build a regular release: `dotnet run --project examples/en/AotConsole`.
//
// Publish as native AOT:
//   dotnet publish examples/en/AotConsole -c Release -p:PublishAot=true -r osx-arm64 --self-contained
// Note: as of v0.2 the SDK uses reflection-based JSON for the `Meta` field
// (object?-polymorphic values). Native AOT publish will produce trim/AOT
// warnings about Inso.Els; the demo here exercises only the primitive
// payload path so it runs end-to-end. Full AOT compatibility is on the
// roadmap (docs/FUTURE_IMPROVEMENTS.md).

using Inso.Els;

await using var client = new ElsClient(
    endpoint: "https://api.insoweb.ru/els",
    apiKey: Environment.GetEnvironmentVariable("ELS_API_KEY") ?? "your-api-key",
    appSlug: "aot-demo");

client.CaptureMessage("aot smoke test", ElsLevel.Info, url: "/aot");

await client.FlushAsync();
Console.WriteLine("done");
