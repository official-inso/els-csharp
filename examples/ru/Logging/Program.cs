// Пример: маршрутизация Microsoft.Extensions.Logging в ELS.
//
// Запуск:
//   export ELS__Endpoint=https://api.example.com/els
//   export ELS__ApiKey=ваш-api-ключ
//   dotnet run --project examples/ru/Logging

using Inso.Els;
using Inso.Els.AspNetCore;
using Inso.Els.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using var host = Host.CreateApplicationBuilder(args).BuildHost();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("старт задачи");

    using (logger.BeginScope(new Dictionary<string, object?> { ["tenant"] = "acme" }))
    {
        logger.LogWarning("использование памяти выше {Threshold}% (текущее {Actual}%)", 80, 91);

        try
        {
            throw new InvalidOperationException("БД недоступна");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "пользователь {UserId} не смог загрузить профиль", 42);
        }
    }

    logger.LogInformation("задача завершена");
}
finally
{
    await Sdk.FlushAsync();
}

internal static class HostHelper
{
    public static IHost BuildHost(this HostApplicationBuilder builder)
    {
        builder.Services.AddEls(opts =>
        {
            opts.Endpoint = builder.Configuration["Els:Endpoint"] ?? "https://api.example.com/els";
            opts.ApiKey = builder.Configuration["Els:ApiKey"] ?? "ваш-api-ключ";
            opts.AppSlug = "logging-demo";
        });
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddEls(o => o.MinLevel = ElsLevel.Warning);
        return builder.Build();
    }
}
