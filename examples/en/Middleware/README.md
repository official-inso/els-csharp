# Middleware example (EN)

ASP.NET Core minimal API that demonstrates `AddEls` + `UseElsExceptionHandling`.

```bash
export ELS__Endpoint=https://api.example.com/els
export ELS__ApiKey=your-api-key
dotnet run --project examples/en/Middleware
```

Then trigger an exception:

```bash
curl http://localhost:5000/boom    # captured as Critical
curl http://localhost:5000/manual  # captured manually as Warning
```
