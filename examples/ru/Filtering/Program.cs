// PII-фильтрация + sampling + level-фильтр — комбинированно.

using System.Text.RegularExpressions;
using Inso.Els;

var emailPattern = new Regex(@"[\w\.-]+@[\w\.-]+", RegexOptions.Compiled);

await using var client = new ElsClient(new ElsOptions
{
    Endpoint = "https://api.insoweb.ru/els",
    ApiKey = Environment.GetEnvironmentVariable("ELS_API_KEY") ?? "ваш-api-ключ",
    AppSlug = "filtering-demo",

    // Всё ниже Warning отсекаем на источнике.
    MinLevel = ElsLevel.Warning,

    // 25% non-critical записей. Critical обходит фильтр (по умолчанию).
    SampleRate = 0.25,

    // Синхронный хук — маскируем email в message/stack.
    BeforeSend = entry => entry with
    {
        Message = emailPattern.Replace(entry.Message, "<скрыто-email>"),
        Stack = entry.Stack is null ? null : emailPattern.Replace(entry.Stack, "<скрыто-email>"),
    },

    // Async-хук — выполняется первым. Отбрасываем заблокированных tenant'ов.
    BeforeSendAsync = async entry =>
    {
        await Task.Yield(); // имитация обращения к кешу / БД
        if (entry.Meta?.TryGetValue("tenant", out var tenant) == true
            && string.Equals(tenant?.ToString(), "blocked-tenant", StringComparison.Ordinal))
        {
            return null; // полностью отбросить
        }
        return entry;
    },

    OnError = ex => Console.Error.WriteLine($"[ELS] внутренняя ошибка: {ex.Message}"),
});

// Будет отброшено по MinLevel.
client.CaptureMessage("debug detail", ElsLevel.Debug, url: "/ignored");

// Email замаскируется BeforeSend.
client.CaptureMessage("свяжитесь с john@example.com для деталей", ElsLevel.Warning, url: "/notify");

// Отбросится BeforeSendAsync.
client.CaptureMessage("трафик от blocked-tenant", ElsLevel.Error, url: "/api",
    meta: new Dictionary<string, object?> { ["tenant"] = "blocked-tenant" });

// Critical всегда проходит независимо от SampleRate.
try
{
    throw new InvalidOperationException("таймаут платёжного шлюза для user@example.com");
}
catch (Exception ex)
{
    client.CaptureError(ex, url: "/payments", level: ElsLevel.Critical);
}

await client.FlushAsync();
Console.WriteLine($"Sampled: {client.Stats.Sampled}, Enqueued: {client.Stats.Enqueued}");
