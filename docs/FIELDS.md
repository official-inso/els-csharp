# Error entry fields

All fields the .NET SDK can send to the ELS API. JSON property names use the
wire format from the [Go SDK](https://github.com/official-inso/els-go) so
records are interchangeable across SDKs.

## Required

| Field | Type | Max length | Description |
|-------|------|-----------|-------------|
| `message` | string | 10,000 | Error message. |
| `url` | string | 2,000 | URL where the error occurred. Use `CaptureOptions.WithUrl()` or `WithHttpContext()`. |

## Auto-filled by the SDK

| Field | Default source | Description |
|-------|----------------|-------------|
| `timestamp` | `DateTimeOffset.UtcNow` | RFC 3339 / ISO 8601 UTC. |
| `level` | `ElsOptions.DefaultLevel` | `critical` / `error` / `warning` / `info` / `debug`. |
| `source` | `ElsOptions.DefaultSource` | `server` / `client`. |
| `appSlug` | `ElsOptions.AppSlug` | Application identifier. |
| `deploymentEnv` | `ElsOptions.DeploymentEnv` | Normalized server-side (`dev` → `DEV`, …). |
| `serviceName` | `ElsOptions.ServiceName` | Microservice name. |
| `sessionId` | Auto-generated | Process-level session identifier (`els-<hex>`). |
| `stack` | Exception or current stack | Only for `CaptureError`. |

## Optional

Set via `CaptureOptions.WithXxx()` or directly on `ErrorEntry`:

| Field | Type | Option | Description |
|-------|------|--------|-------------|
| `stack` | string | `WithStack(s)` | Override the auto-captured stack. |
| `componentStack` | string | `WithComponentStack(s)` | Framework component trace. |
| `userAgent` | string | `WithUserAgent(ua)` | Client UA. |
| `language` | string | `WithLanguage(l)` | Client locale, e.g. `"en-US"`. |
| `screenSize` | string | — | `WxH`, client-side only. |
| `viewportSize` | string | — | `WxH`, client-side only. |
| `referrer` | string | `WithReferrer(r)` | HTTP Referer. |
| `httpStatus` | number | `WithHttpStatus(s)` | Status code of the failed response. |
| `durationMs` | number | `WithDuration(ms)` | Duration of the failed operation. |
| `appVersion` | string | `WithAppVersion(v)` | Per-entry version override. |
| `serviceName` | string | `WithServiceName(n)` | Per-entry service override. |
| `sessionId` | string | `WithSessionId(id)` | Per-entry session override. |
| `meta` | object | `WithMeta(m)` / `WithMetaItem(k, v)` | Arbitrary key-value data. |

## Convenience options

| Option | Effect |
|--------|--------|
| `WithCause(ex)` | Flattens `InnerException` (up to 8 levels) and `AggregateException.InnerExceptions` into `meta["error.causes"]`. |
| `WithHttpContext(ctx)` *(`Inso.Els.AspNetCore`)* | Extracts URL, UA, Referer, Accept-Language, request id, forwarded-for; adds `http.method`, `http.host`, `http.remoteAddr`, etc. to `meta`. |
| `WithHttpRequest(req)` *(`Inso.Els.AspNetCore`)* | Same, but takes an `HttpRequest` directly. |

## Level values

| Wire value | Enum | When to use |
|------------|------|-------------|
| `critical` | `ElsLevel.Critical` | System down, data loss. Never sampled out. |
| `error` | `ElsLevel.Error` | Operation failed. |
| `warning` | `ElsLevel.Warning` | Potential issue. |
| `info` | `ElsLevel.Info` | Significant event. |
| `debug` | `ElsLevel.Debug` | Diagnostic detail. |

## Source values

| Wire value | Enum | Description |
|------------|------|-------------|
| `server` | `ElsSource.Server` | Backend / server-side. |
| `client` | `ElsSource.Client` | Frontend / browser / mobile. |

## Environment normalization

ELS normalizes `deploymentEnv` case-insensitively:

| You send | Stored as |
|----------|-----------|
| `dev`, `development`, `test` | `DEV` |
| `staging`, `stage`, `stg` | `STAGING` |
| `prod`, `production` | `PRODUCTION` |
| anything else | upper-cased |

## Server-generated fields

These are computed server-side; the SDK does not set them:

| Field | Description |
|-------|-------------|
| `traceId` | Unique per-record identifier. |
| `browser` | Parsed from `userAgent`. |
| `urlPath` | Normalized path (UUIDs / numeric ids replaced). |
| `errorCategory` | Auto-categorized from message. |
| `fingerprint` | Hash of message + first stack frame + source. |
| `ip` | Client IP. |

## User context meta keys

When `client.User` is set, the SDK adds the following entries to `meta` for every
subsequent capture:

| Meta key | Source |
|----------|--------|
| `user.id` | `UserContext.Id` |
| `user.email` | `UserContext.Email` |
| `user.name` | `UserContext.Name` |
| `user.<k>` | Each `UserContext.Extra` entry |
