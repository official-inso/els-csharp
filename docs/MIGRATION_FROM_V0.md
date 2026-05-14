# Migration from the v0.x SDK

The earlier `Inso.Els` v0.x package shipped a much smaller surface
(`ELSClient`, `ELSOptions`, `ELSException`). The new SDK is binary-incompatible
but the migration is mechanical.

| v0.x | v1.x |
|---|---|
| `ELSClient` | `ElsClient` (PascalCase, single `s`) |
| `ELSOptions` | `ElsOptions` |
| `ELSException` | `ElsException` (base) / `ElsSendException` (transport) / `ElsConfigurationException` |
| `ELSOptions.AuthHeader = "bearer"` | `ElsOptions.AuthScheme = ElsAuthScheme.Bearer` |
| `ELSOptions.AuthHeader = "x-api-key"` | `ElsOptions.AuthScheme = ElsAuthScheme.ApiKeyHeader` |
| `client.SendErrorAsync(entry)` (sync HTTP) | `client.CaptureEntry(entry)` (async batched) or `client.SendEntryAsync(entry, ct)` (awaits server) |
| `client.SendBatchAsync(entries)` | The batching worker now handles this automatically. Call `CaptureEntry` for each, then `FlushAsync()` before exit. |
| Constructor: `new ELSClient(httpClient, options)` | Constructor: `new ElsClient(options)`. To bring your own `HttpClient`, set `ElsOptions.HttpClient`. |
| Hand-built JSON with `traceId` | The SDK no longer sets `traceId` client-side; ELS generates it server-side. Remove client-side `Guid.NewGuid()` calls. |

## Common migration patterns

### Before

```csharp
var http = new HttpClient();
var client = new ELSClient(http, new ELSOptions
{
    Endpoint = "https://api.insoweb.ru/els",
    ApiKey = "k",
    AppSlug = "svc",
    Retries = 3,
});

var resp = await client.SendErrorAsync(new ErrorEntry
{
    Message = "boom",
    Url = "/api",
    Level = "error",
});
```

### After

```csharp
await using var client = new ElsClient(new ElsOptions
{
    Endpoint = "https://api.insoweb.ru/els",
    ApiKey = "k",
    AppSlug = "svc",
    MaxRetries = 3,
});

client.CaptureMessage("boom", ElsLevel.Error, new CaptureOptions { Url = "/api" });
await client.FlushAsync();
```

### ASP.NET Core

If you previously registered the client manually in `Startup.cs` or
`Program.cs`, replace the registration with `services.AddEls(...)` from the
new `Inso.Els.AspNetCore` package.

## Behavioural differences

- `SendErrorAsync` in v0.x returned `HttpResponseMessage` and the caller was
  expected to inspect `StatusCode`. The new `SendEntryAsync` throws
  `ElsSendException` on failure and unwraps the response body into the
  exception's `ResponseBody`.
- The new SDK batches by default. If you were sending in a tight loop, expect
  fewer HTTP calls but slightly higher tail latency for any given entry
  (bounded by `BatchInterval`).
- The new SDK persists undelivered entries to `.els-buffer.jsonl` and replays
  them on next startup. If you ran multiple processes against the same buffer
  directory in v0.x, set `ElsOptions.BufferDir` to a per-process path.
