# Пример с middleware (RU)

ASP.NET Core minimal API, демонстрирующий `AddEls` + `UseElsExceptionHandling`.

```bash
export ELS__Endpoint=https://api.insoweb.ru/els
export ELS__ApiKey=ваш-api-ключ
dotnet run --project examples/ru/Middleware
```

Затем:

```bash
curl http://localhost:5000/boom    # захватится как Critical
curl http://localhost:5000/manual  # ручной захват как Warning
```
