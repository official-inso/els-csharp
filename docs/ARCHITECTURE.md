# Internal architecture

This document is a reading guide for contributors. End users only need
[`README.md`](../README.md).

## Project layout

```
src/Inso.Els/                       — core SDK (no ASP.NET dependency)
    ElsClient.cs                    — public client, IElsClient
    Sdk.cs                          — optional static facade
    ElsOptions.cs                   — immutable options record + Normalize()
    ErrorEntry.cs                   — wire payload
    CaptureOptions(.Extensions).cs  — per-call overrides + fluent helpers
    Exceptions/                     — typed exception hierarchy
    Internal/
        HttpTransport.cs            — POST /errors, /errors/batch, /health
        BackgroundWorker.cs         — Channel<ErrorEntry> + flush loop
        DiskBuffer.cs               — JSONL fallback, Go-compatible format
        EntryEnricher.cs            — defaults, user context, cause chains
        StackTraceCapture.cs        — strips SDK-internal frames
        SessionIdFactory.cs         — "els-<hex>" generator
        JsonSerialization.cs        — shared JsonSerializerOptions
        ElsLevelJsonConverter.cs    — lowercase wire enums
        IsExternalInit.cs           — netstandard2.0 polyfill
src/Inso.Els.AspNetCore/            — middleware + DI extensions
src/Inso.Els.Extensions.Logging/    — ILoggerProvider
tests/                              — xUnit + FluentAssertions + WireMock.Net
examples/                           — runnable EN + RU samples
```

## Lifecycle

1. `new ElsClient(options)` — `Normalize()` validates required values and
   applies defaults. A `BackgroundWorker` is started immediately. The worker
   first replays anything in the disk buffer from a previous run.
2. `CaptureError` / `CaptureMessage` / `CaptureEntry` enrich the entry, apply
   `MinLevel` and `SampleRate` filters, run `BeforeSend`, and push to the
   bounded `Channel<ErrorEntry>` (`DropOldest` on overflow).
3. The worker reads in two ways:
   - When data is available, it drains everything currently in the channel up
     to `BatchSize`, flushes if full, repeats.
   - When idle, a `PeriodicTimer`-equivalent fires every `BatchInterval` and
     flushes any partial batch.
4. On flush, `HttpTransport.SendBatchAsync` posts `{"errors":[...]}` to
   `/errors/batch`. 5xx and 429 trigger exponential backoff with `±10%`
   jitter; 429 honours `Retry-After`. 4xx (except 429) is permanent.
5. On any failure (retries exhausted or transport exception), the worker
   appends the batch to `DiskBuffer`. The next process run reads and replays
   the file before any new captures are sent.
6. `DisposeAsync` (or `Dispose`) drains the channel, sends a final batch, then
   stops. Idempotent.

## Wire compatibility

- `ErrorEntry` JSON property names are pinned via `[JsonPropertyName(...)]`.
- Enums are serialized to lowercase strings (`"critical"`, `"server"`, …) via
  custom converters.
- Timestamps use `yyyy-MM-ddTHH:mm:ss.fffffffK` — wire-equivalent to Go's
  `time.RFC3339Nano`.
- Disk buffer file (`.els-buffer.jsonl`) is one JSON object per line, exactly
  matching the Go SDK so the two SDKs can read each other's leftovers.

## Threading

- `ElsClient` is safe for concurrent use. Counters use `Interlocked`. The
  user context is guarded by a small lock; reads and writes are infrequent.
- The background worker is a single reader (`SingleReader = true` on the
  channel). Producers are concurrent.
- `DisposeAsync` is idempotent via `Interlocked.Exchange`.

## What we deliberately did not do

- We do not depend on any non-stdlib NuGet package in the core (besides
  `System.Text.Json` and `System.Threading.Channels` on netstandard targets,
  which ship as part of the BCL on net6.0+).
- No reflection-based serialization at runtime: every entry serializes
  through `JsonSerializer` with explicit converters, ready for AOT/trim work
  later via `JsonSerializerContext`.
- No custom thread pool. `Task.Run` puts the worker on the .NET thread pool;
  this is sufficient for the I/O-bound batching loop.
