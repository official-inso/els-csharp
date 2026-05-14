# Поля записи ошибки

Все поля, которые .NET SDK может отправить в ELS. JSON-имена полей совпадают с
wire-форматом [Go SDK](https://github.com/official-inso/els-go), так что записи
взаимозаменяемы между SDK.

## Обязательные

| Поле | Тип | Макс. длина | Описание |
|------|------|-------------|----------|
| `message` | string | 10 000 | Текст ошибки. |
| `url` | string | 2 000 | URL, где произошла ошибка. Через `CaptureOptions.WithUrl()` или `WithHttpContext()`. |

## Автозаполняемые SDK

| Поле | Источник по умолчанию | Описание |
|------|----------------------|----------|
| `timestamp` | `DateTimeOffset.UtcNow` | RFC 3339 / ISO 8601 UTC. |
| `level` | `ElsOptions.DefaultLevel` | `critical` / `error` / `warning` / `info` / `debug`. |
| `source` | `ElsOptions.DefaultSource` | `server` / `client`. |
| `appSlug` | `ElsOptions.AppSlug` | Идентификатор приложения. |
| `deploymentEnv` | `ElsOptions.DeploymentEnv` | Нормализуется на сервере (`dev` → `DEV`, …). |
| `serviceName` | `ElsOptions.ServiceName` | Имя микросервиса. |
| `sessionId` | Автогенерация | Process-level идентификатор сессии (`els-<hex>`). |
| `stack` | Из исключения или текущий стек | Только для `CaptureError`. |

## Опциональные

Задаются через `CaptureOptions.WithXxx()` либо прямо на `ErrorEntry`:

| Поле | Тип | Опция | Описание |
|------|------|-------|----------|
| `stack` | string | `WithStack(s)` | Переопределяет авто-стек. |
| `componentStack` | string | `WithComponentStack(s)` | Компонентный стек фреймворка. |
| `userAgent` | string | `WithUserAgent(ua)` | UA клиента. |
| `language` | string | `WithLanguage(l)` | Локаль, например `"en-US"`. |
| `screenSize` | string | — | `WxH`, только клиентская сторона. |
| `viewportSize` | string | — | `WxH`, только клиентская сторона. |
| `referrer` | string | `WithReferrer(r)` | HTTP Referer. |
| `httpStatus` | number | `WithHttpStatus(s)` | Код ответа упавшей операции. |
| `durationMs` | number | `WithDuration(ms)` | Длительность упавшей операции. |
| `appVersion` | string | `WithAppVersion(v)` | Per-entry переопределение версии. |
| `serviceName` | string | `WithServiceName(n)` | Per-entry имя сервиса. |
| `sessionId` | string | `WithSessionId(id)` | Per-entry session id. |
| `meta` | object | `WithMeta(m)` / `WithMetaItem(k, v)` | Произвольные key-value. |

## Удобные опции

| Опция | Эффект |
|-------|--------|
| `WithCause(ex)` | Разворачивает `InnerException` (до 8 уровней) и `AggregateException.InnerExceptions` в `meta["error.causes"]`. |
| `WithHttpContext(ctx)` *(`Inso.Els.AspNetCore`)* | Извлекает URL, UA, Referer, Accept-Language, request id, forwarded-for; добавляет `http.method`, `http.host`, `http.remoteAddr` и др. в `meta`. |
| `WithHttpRequest(req)` *(`Inso.Els.AspNetCore`)* | То же, но принимает `HttpRequest` напрямую. |

## Значения уровня

| Wire-значение | Enum | Когда использовать |
|---------------|------|-------------------|
| `critical` | `ElsLevel.Critical` | Сервис лежит, потеря данных. Никогда не сэмплируется. |
| `error` | `ElsLevel.Error` | Операция упала. |
| `warning` | `ElsLevel.Warning` | Потенциальная проблема. |
| `info` | `ElsLevel.Info` | Значимое событие. |
| `debug` | `ElsLevel.Debug` | Диагностические детали. |

## Значения source

| Wire-значение | Enum | Описание |
|---------------|------|----------|
| `server` | `ElsSource.Server` | Backend / серверная сторона. |
| `client` | `ElsSource.Client` | Frontend / браузер / мобильный клиент. |

## Нормализация окружения

ELS приводит `deploymentEnv` к каноническому виду:

| Что отправлено | Сохраняется как |
|----------------|----------------|
| `dev`, `development`, `test` | `DEV` |
| `staging`, `stage`, `stg` | `STAGING` |
| `prod`, `production` | `PRODUCTION` |
| прочее | в верхний регистр |

## Поля, генерируемые сервером

Эти поля считаются на сервере; SDK их не задаёт:

| Поле | Описание |
|------|----------|
| `traceId` | Уникальный идентификатор записи. |
| `browser` | Распарсенный браузер из `userAgent`. |
| `urlPath` | Нормализованный path (UUID / числовые id заменяются). |
| `errorCategory` | Категория по `message`. |
| `fingerprint` | Хэш `message` + первый фрейм стека + `source`. |
| `ip` | IP клиента. |

## Ключи user-контекста в meta

Когда выставлен `client.User`, SDK добавляет в `meta` каждой последующей записи:

| Ключ meta | Источник |
|-----------|----------|
| `user.id` | `UserContext.Id` |
| `user.email` | `UserContext.Email` |
| `user.name` | `UserContext.Name` |
| `user.<k>` | Каждый ключ `UserContext.Extra` |
