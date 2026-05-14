# Worker (EN)

Generic Host worker (no web) with a `BackgroundService` that captures errors
into ELS. The SDK's `IHostedService` flushes the queue on shutdown.

```bash
dotnet run --project examples/en/Worker
```

Send `Ctrl+C` to trigger graceful shutdown; pending entries are flushed.
