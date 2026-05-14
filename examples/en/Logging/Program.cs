// Example: routing Microsoft.Extensions.Logging to ELS.
//
// Run:
//   export ELS__Endpoint=https://api.example.com/els
//   export ELS__ApiKey=your-api-key
//   dotnet run --project examples/en/Logging

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
    logger.LogInformation("starting workload");

    using (logger.BeginScope(new Dictionary<string, object?> { ["tenant"] = "acme" }))
    {
        logger.LogWarning("memory usage above {Threshold}% (current {Actual}%)", 80, 91);

        try
        {
            throw new InvalidOperationException("database unavailable");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "user {UserId} could not load profile", 42);
        }
    }

    logger.LogInformation("workload finished");
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
            opts.ApiKey = builder.Configuration["Els:ApiKey"] ?? "your-api-key";
            opts.AppSlug = "logging-demo";
        });
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddEls(o => o.MinLevel = ElsLevel.Warning);
        return builder.Build();
    }
}
