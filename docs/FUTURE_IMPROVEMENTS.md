# Future improvements

Items deferred from v0.2.0 and tracked for later releases.

## Full Native AOT support

The SDK currently uses reflection-based `System.Text.Json` for the `Meta`
field (`Dictionary<string, object?>`). Native AOT publish (`PublishAot=true`)
will produce trim/AOT warnings about `Inso.Els.dll`.

To make the SDK fully AOT-compatible we need to either:

- Replace `object?` values in `Meta` with a custom polymorphic JSON writer
  that handles primitives + a registered set of complex types.
- Or expose a typed alternative (e.g. `MetaBuilder`) and deprecate
  `object?`-based meta.

Until then, AOT users can publish with the SDK and accept the warnings; the
code path works for primitive Meta values.

## `IOptionsMonitor<ElsOptions>` hot-reload

`AddElsFromOptions` resolves `IOptions<ElsOptions>` once on first use. Live
configuration changes (`appsettings.json` reload) are not propagated, because
`ElsClient` is constructed eagerly. Future iteration: react to
`IOptionsMonitor.OnChange` and rebuild the client without losing the disk
buffer.

## Built-in OpenTelemetry exporter

Today `Stats` and `StatsChanged` are pull/event-only. A first-party
OpenTelemetry meter exporter (counters, gauges) would integrate the SDK into
common observability stacks without bespoke glue.

## Source-generated JSON for `ErrorEntry`

Independent of the Meta issue, we can ship a partial `JsonSerializerContext`
for `ErrorEntry` / `BatchRequestDto` and fall back to reflection only for
Meta. This already cuts most of the AOT surface.

## Additional DX notes (from second-pass review of v0.2.0)

- `CaptureError(Exception ex, string? url, ElsLevel? level, IDictionary<string, object?>? meta)`
  does not accept a `cause` exception. Users who need to flatten an
  `InnerException` chain into `meta["error.causes"]` must fall back to the
  full `CaptureOptions` overload. Consider adding an `Exception? cause = null`
  parameter to the convenience overloads.
- `Sdk.Init` and `ElsClient.Dispose` block synchronously on `CloseAsync`
  via `GetAwaiter().GetResult()`. In UI / sync-context applications this is
  a potential deadlock vector — document this and/or replace with safer
  shutdown helper.
- The static `Sdk.Init(...)` replaces a previous global client. If the
  previous client still has unsent entries, `CloseAsync` is awaited for them,
  but the flow is silent. Surface a hook / log event for "replacing client".
- `ElsExceptionHandlerOptions.OnException` callbacks swallow their own
  exceptions to protect the pipeline. Surface those swallowed errors via
  the SDK's `OnError` hook for visibility.
- `ElsStats.BufferedBytes` is a long byte count but is reported alongside
  counters with semantically different units. Consider a richer
  `ElsBufferStats { long Bytes; int RoughEntryCount; }` snapshot.

These are all small papercuts; they don't block v0.2.0 release.
