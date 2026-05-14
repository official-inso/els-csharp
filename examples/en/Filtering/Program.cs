// PII filtering + sampling + level filter — combined.

using System.Text.RegularExpressions;
using Inso.Els;

var emailPattern = new Regex(@"[\w\.-]+@[\w\.-]+", RegexOptions.Compiled);

await using var client = new ElsClient(new ElsOptions
{
    Endpoint = "https://api.insoweb.ru/els",
    ApiKey = Environment.GetEnvironmentVariable("ELS_API_KEY") ?? "your-api-key",
    AppSlug = "filtering-demo",

    // Drop anything below Warning at the source.
    MinLevel = ElsLevel.Warning,

    // Sample 25% of non-critical entries. Critical bypass (default).
    SampleRate = 0.25,

    // Sync hook — mask emails in message/stack.
    BeforeSend = entry => entry with
    {
        Message = emailPattern.Replace(entry.Message, "<redacted-email>"),
        Stack = entry.Stack is null ? null : emailPattern.Replace(entry.Stack, "<redacted-email>"),
    },

    // Async hook — runs before the sync one. Drop entries from blocked tenants.
    BeforeSendAsync = async entry =>
    {
        await Task.Yield(); // pretend we hit a cache / database
        if (entry.Meta?.TryGetValue("tenant", out var tenant) == true
            && string.Equals(tenant?.ToString(), "blocked-tenant", StringComparison.Ordinal))
        {
            return null; // drop entirely
        }
        return entry;
    },

    OnError = ex => Console.Error.WriteLine($"[ELS] internal error: {ex.Message}"),
});

// Will be dropped by MinLevel.
client.CaptureMessage("debug detail", ElsLevel.Debug, url: "/ignored");

// Email gets masked by BeforeSend.
client.CaptureMessage("contact john@example.com for details", ElsLevel.Warning, url: "/notify");

// Dropped by async BeforeSendAsync.
client.CaptureMessage("blocked-tenant traffic", ElsLevel.Error, url: "/api",
    meta: new Dictionary<string, object?> { ["tenant"] = "blocked-tenant" });

// Critical always passes regardless of SampleRate.
try
{
    throw new InvalidOperationException("payment gateway timeout for user@example.com");
}
catch (Exception ex)
{
    client.CaptureError(ex, url: "/payments", level: ElsLevel.Critical);
}

await client.FlushAsync();
Console.WriteLine($"Sampled: {client.Stats.Sampled}, Enqueued: {client.Stats.Enqueued}");
