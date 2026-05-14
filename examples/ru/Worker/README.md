# Worker (RU)

Generic Host worker (без web) с `BackgroundService`, который захватывает
ошибки в ELS. `IHostedService` от SDK дренирует очередь при shutdown.

```bash
dotnet run --project examples/ru/Worker
```

`Ctrl+C` — корректное завершение; pending-записи дослают.
