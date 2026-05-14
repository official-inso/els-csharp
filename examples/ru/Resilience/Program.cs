// Оборачиваем HttpClient ELS в Polly-пайплайн (timeout + retry +
// circuit breaker). SDK сам делает retry, Polly здесь покрывает
// transport-level edge cases (TLS handshake, DNS и т.п.).

using Inso.Els;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;

var policy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .OrResult(r => (int)r.StatusCode == 408)
    .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)));

var pollyHandler = new PolicyHttpMessageHandler(policy) { InnerHandler = new HttpClientHandler() };

var http = new HttpClient(pollyHandler)
{
    Timeout = TimeSpan.FromSeconds(30),
};

await using var client = new ElsClient(new ElsOptions
{
    Endpoint = "https://api.insoweb.ru/els",
    ApiKey = Environment.GetEnvironmentVariable("ELS_API_KEY") ?? "ваш-api-ключ",
    AppSlug = "resilience-demo",
    HttpClient = http,
    MaxRetries = 1, // SDK ретраит минимально; остальное — Polly
});

client.CaptureMessage("resilience smoke", ElsLevel.Info, url: "/");
await client.FlushAsync();
