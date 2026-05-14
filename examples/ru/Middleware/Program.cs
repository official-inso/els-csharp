// Пример: ASP.NET Core minimal API с middleware Inso.Els.
//
// Запуск:
//   export ELS__Endpoint=https://api.example.com/els
//   export ELS__ApiKey=ваш-api-ключ
//   dotnet run --project examples/ru/Middleware
//
// Затем триггер исключения:
//   curl http://localhost:5000/boom

using Inso.Els;
using Inso.Els.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Регистрируем SDK в DI. ServiceCollection extensions также подключает
// hosted service, который сбрасывает очередь перед остановкой приложения.
builder.Services.AddEls(opts =>
{
    opts.Endpoint = builder.Configuration["Els:Endpoint"] ?? "https://api.example.com/els";
    opts.ApiKey = builder.Configuration["Els:ApiKey"] ?? "ваш-api-ключ";
    opts.AppSlug = "demo-web";
    opts.DeploymentEnv = builder.Environment.EnvironmentName;
});

var app = builder.Build();

// Регистрируем middleware для перехвата исключений как можно раньше, чтобы
// исключения downstream-обработчиков попадали в ELS.
app.UseElsExceptionHandling();

app.MapGet("/", () => "OK");

app.MapGet("/boom", () =>
{
    // Любое необработанное исключение попадёт в ELS с уровнем Critical.
    throw new InvalidOperationException("имитация сбоя");
});

app.MapGet("/manual", (IElsClient client, HttpContext ctx) =>
{
    client.CaptureMessage("ручной захват", ElsLevel.Warning,
        new CaptureOptions().WithHttpContext(ctx).WithMetaItem("userId", 42));
    return "захвачено";
});

app.Run();
