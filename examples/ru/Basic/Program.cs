// Пример: базовое использование .NET SDK для Inso.Els.
//
// Показывает инициализацию клиента, асинхронный захват ошибок,
// синхронную отправку критических ошибок, проверку доступности сервера
// и корректное завершение работы.
//
// Запуск:
//   export ELS_ENDPOINT=https://api.insoweb.ru/els
//   export ELS_API_KEY=ваш-api-ключ
//   dotnet run

using Inso.Els;

var options = new ElsOptions
{
    Endpoint = Environment.GetEnvironmentVariable("ELS_ENDPOINT") ?? "https://api.insoweb.ru/els",
    ApiKey = Environment.GetEnvironmentVariable("ELS_API_KEY") ?? "ваш-api-ключ",
    AppSlug = "my-service",
    DeploymentEnv = "PRODUCTION",
    ServiceName = "api-gateway",
    BatchSize = 50,
    BatchInterval = TimeSpan.FromSeconds(5),
    MinLevel = ElsLevel.Warning,
    OnError = ex => Console.Error.WriteLine($"[ELS] внутренняя ошибка: {ex.Message}"),
};

await using var client = new ElsClient(options);

// 1. Проверка доступности сервера (необязательно, но полезно в dev-окружении).
try
{
    await client.HealthAsync();
    Console.WriteLine("Сервер ELS доступен.");
}
catch (ElsSendException ex)
{
    Console.WriteLine($"Сервер ELS недоступен ({ex.StatusCode}): записи будут буферизованы.");
}

// 2. Асинхронный захват ошибки с автоматическим stack trace.
try
{
    throw new InvalidOperationException("таймаут подключения к БД");
}
catch (Exception ex)
{
    client.CaptureError(ex,
        new CaptureOptions
        {
            Url = "/api/users",
            Level = ElsLevel.Critical,
            Meta = new Dictionary<string, object?>
            {
                ["database"] = "postgres-primary",
                ["timeout"] = "30s",
            },
        });
}

// 3. Асинхронный захват сообщения — короткая перегрузка с named-параметрами.
client.CaptureMessage("использование памяти выше 80%", ElsLevel.Warning,
    url: "/health",
    meta: new Dictionary<string, object?> { ["memoryPct"] = 82.5 });

// 4. Синхронная отправка критической ошибки (ожидает подтверждение сервера).
try
{
    await client.SendAsync(
        new Exception("сбой обработки платежа"),
        new CaptureOptions { Url = "/api/payments/charge", Level = ElsLevel.Critical }
            .WithMetaItem("orderId", "ord_12345")
            .WithMetaItem("amount", 9900));
}
catch (ElsSendException ex)
{
    if (ex.IsRetryable) Console.WriteLine($"Временная ошибка ELS: {ex.Message}");
    else Console.WriteLine($"Постоянная ошибка ELS: {ex.Message}");
}

// 5. Готовая структура.
client.CaptureEntry(new ErrorEntry
{
    Message = "обнаружен необычный паттерн трафика",
    Url = "/api/analytics",
    Level = ElsLevel.Warning,
    Meta = new Dictionary<string, object?>
    {
        ["rps"] = 15000,
        ["threshold"] = 10000,
    },
});

// 6. Дождаться отправки оставшихся записей перед выходом.
await client.FlushAsync();
Console.WriteLine("Все ошибки захвачены. Завершение работы.");
