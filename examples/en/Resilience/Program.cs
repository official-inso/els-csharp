// Wraps the ELS HttpClient in a Polly resilience pipeline (timeout + retry +
// circuit breaker). The SDK itself already retries, so Polly here only kicks
// in for transport-level edge cases (TLS handshake, DNS, etc).

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
    ApiKey = Environment.GetEnvironmentVariable("ELS_API_KEY") ?? "your-api-key",
    AppSlug = "resilience-demo",
    HttpClient = http,
    MaxRetries = 1, // keep SDK retry minimal; Polly handles the rest
});

client.CaptureMessage("resilience smoke", ElsLevel.Info, url: "/");
await client.FlushAsync();
