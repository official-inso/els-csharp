// Свой HttpClient: пускаем ELS-трафик через настроенную цепочку handler-ов
// (здесь — кастомный User-Agent и логгирующий DelegatingHandler). Может быть
// корпоративный proxy, SOCKS, mTLS handler и т.д.

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
    ApiKey = Environment.GetEnvironmentVariable("ELS_API_KEY") ?? "ваш-api-ключ",
    AppSlug = "custom-http-demo",
    HttpClient = http, // SDK использует этот клиент и не диспозит его
});

client.CaptureMessage("привет через кастомный HttpClient", ElsLevel.Info, url: "/");
await client.FlushAsync();

internal sealed class LoggingHandler : DelegatingHandler
{
    public LoggingHandler(HttpMessageHandler inner) : base(inner) { }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"--> {request.Method} {request.RequestUri}");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"<-- {(int)response.StatusCode} за {sw.Elapsed.TotalMilliseconds:F0}мс");
        return response;
    }
}
