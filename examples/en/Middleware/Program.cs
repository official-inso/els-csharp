// Example: ASP.NET Core minimal API with the Inso.Els exception middleware.
//
// Run:
//   export ELS__Endpoint=https://api.example.com/els
//   export ELS__ApiKey=your-api-key
//   dotnet run --project examples/en/Middleware
//
// Then trigger an exception:
//   curl http://localhost:5000/boom

using Inso.Els;
using Inso.Els.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Register the SDK with DI. ServiceCollection extensions also wire up the
// hosted service that flushes pending entries on shutdown.
builder.Services.AddEls(opts =>
{
    opts.Endpoint = builder.Configuration["Els:Endpoint"] ?? "https://api.example.com/els";
    opts.ApiKey = builder.Configuration["Els:ApiKey"] ?? "your-api-key";
    opts.AppSlug = "demo-web";
    opts.DeploymentEnv = builder.Environment.EnvironmentName;
});

var app = builder.Build();

// Register the exception-capturing middleware as early as possible so
// exceptions thrown by any downstream handler are observed.
app.UseElsExceptionHandling();

app.MapGet("/", () => "OK");

app.MapGet("/boom", () =>
{
    // Any unhandled exception ends up in ELS as a Critical-level entry.
    throw new InvalidOperationException("simulated failure");
});

app.MapGet("/manual", (IElsClient client, HttpContext ctx) =>
{
    client.CaptureMessage("manual capture", ElsLevel.Warning,
        new CaptureOptions().WithHttpContext(ctx).WithMetaItem("userId", 42));
    return "captured";
});

app.Run();
