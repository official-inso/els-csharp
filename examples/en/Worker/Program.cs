// Generic Host worker: BackgroundService with ELS capture and graceful
// shutdown via the SDK's built-in IHostedService.

using Inso.Els;
using Inso.Els.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddEls(opts =>
{
    opts.Endpoint = builder.Configuration["Els:Endpoint"] ?? "https://api.insoweb.ru/els";
    opts.ApiKey = builder.Configuration["Els:ApiKey"] ?? "your-api-key";
    opts.AppSlug = "worker-demo";
    opts.DeploymentEnv = "DEVELOPMENT";
});

builder.Services.AddHostedService<PeriodicCheck>();

await builder.Build().RunAsync();

internal sealed class PeriodicCheck : BackgroundService
{
    private readonly IElsClient _els;
    private readonly ILogger<PeriodicCheck> _log;

    public PeriodicCheck(IElsClient els, ILogger<PeriodicCheck> log)
    {
        _els = els;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        int tick = 0;
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            tick++;
            try
            {
                await DoWorkAsync(tick);
            }
            catch (Exception ex)
            {
                _els.CaptureError(ex, url: "/worker/tick", level: ElsLevel.Error,
                    meta: new Dictionary<string, object?> { ["tick"] = tick });
                _log.LogError(ex, "tick {Tick} failed", tick);
            }
        }
    }

    private static Task DoWorkAsync(int tick)
    {
        if (tick % 3 == 0) throw new InvalidOperationException($"synthetic failure on tick {tick}");
        return Task.CompletedTask;
    }
}
