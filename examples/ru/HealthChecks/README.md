# Health checks (RU)

ASP.NET Core minimal API с `services.AddHealthChecks().AddEls()`.

```bash
dotnet run --project examples/ru/HealthChecks
curl http://localhost:5000/health        # общий
curl http://localhost:5000/health/els    # только ELS
```
