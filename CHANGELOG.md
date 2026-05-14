# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.0] - 2026-05-14

### Added

- Convenience `Capture*` overloads: `client.CaptureError(ex, url: "/api")` and
  `client.CaptureMessage(msg, level, url: "/api")` without constructing
  `CaptureOptions`.
- `ElsClient(string endpoint, string apiKey, string? appSlug = null)` quick-start
  constructor.
- `ElsOptions.BeforeSendAsync` — async filter/mutator hook for I/O-heavy
  PII filters. Runs before the synchronous `BeforeSend`.
- `ElsOptions.AlwaysCaptureCritical` (default `true`) — controls whether
  critical-level entries bypass `SampleRate`.
- `IElsClient.TryHealthAsync` — non-throwing health probe returning
  `ElsHealthResult { IsHealthy, StatusCode, Latency, Error }`.
- `IElsClient.StatsChanged` event — pushed after every batch send, disk write,
  drop, or sampled drop.
- `Inso.Els.AspNetCore`: `services.AddHealthChecks().AddEls()` integration with
  `Microsoft.Extensions.Diagnostics.HealthChecks`.
- `Inso.Els.AspNetCore`: `services.AddElsFromOptions()` to register the client
  from a pre-bound `IOptions<ElsOptions>` (standard MS Configure pattern).
- `appsettings.json` and `Properties/launchSettings.json` templates in
  `examples/*/Middleware` and `examples/*/Logging`.

### Changed

- `ElsStats.Buffered` renamed to `ElsStats.BufferedBytes` — the value was
  always bytes, the new name reflects that.
- `UserContext.Extra` is now `IReadOnlyDictionary<string, object?>` (was
  `<string, string>`). Non-string values are preserved into `meta["user.<k>"]`.
- `UseElsExceptionHandling(bool rethrow)` replaced with
  `UseElsExceptionHandling(Action<ElsExceptionHandlerOptions>?)` exposing an
  enum (`ElsExceptionMode.CaptureAndRethrow | CaptureAndHandle`), a
  configurable level, and an `OnException` callback.
- `AddEls(IConfiguration)` now **throws** `ElsConfigurationException` on
  unparseable values (e.g. unknown `MinLevel`, malformed `Timeout`) instead of
  silently keeping defaults.
- `AddEls(IConfiguration)` accepts byte-size strings for `MaxBufferFileSize`:
  `"100MB"`, `"50KB"`, `"1GB"`, `"512000000B"` are all valid.
- `EntryEnricher` now substitutes an empty `Url` with the configured `AppSlug`
  (or `"unknown"`) so an accidental `CaptureError(ex)` is logged instead of
  being rejected server-side as a validation error.

### Removed

- Old `UseElsExceptionHandling(bool rethrow)` overload.

### Fixed

- `EntryEnricher` no longer mutates caller-provided `Meta` dictionaries.

[Unreleased]: https://github.com/official-inso/els-csharp/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/official-inso/els-csharp/compare/v0.1.0...v0.2.0
