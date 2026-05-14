// Bring-your-own HttpClient: route ELS traffic through a configured handler
// chain (here: a custom user agent and a logging delegating handler). Could
// equally be a corporate proxy, SOCKS, mTLS handler, etc.

using Inso.Els;

var http = new HttpClient(new LoggingHandler(new HttpClientHandler())
{
    InnerHandler = new HttpClientHandler(),
})
{
    Timeout = TimeSpan.FromSeconds(30),
};
http.DefaultRequestHeaders.UserAgent.ParseAdd("custom-app/1.0");

await using var client = new ElsClient(new ElsOptions
{
    Endpoint = "https://api.insoweb.ru/els",
    ApiKey = Environment.GetEnvironmentVariable("ELS_API_KEY") ?? "your-api-key",
    AppSlug = "custom-http-demo",
    HttpClient = http, // SDK uses this client and does not dispose it
});

client.CaptureMessage("hello via custom HttpClient", ElsLevel.Info, url: "/");
await client.FlushAsync();

internal sealed class LoggingHandler : DelegatingHandler
{
    public LoggingHandler(HttpMessageHandler inner) : base(inner) { }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"--> {request.Method} {request.RequestUri}");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"<-- {(int)response.StatusCode} in {sw.Elapsed.TotalMilliseconds:F0}ms");
        return response;
    }
}
