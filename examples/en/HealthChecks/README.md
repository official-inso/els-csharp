# Health checks (EN)

ASP.NET Core minimal API showing `services.AddHealthChecks().AddEls()` integration.

```bash
dotnet run --project examples/en/HealthChecks
curl http://localhost:5000/health        # overall
curl http://localhost:5000/health/els    # ELS only
```
