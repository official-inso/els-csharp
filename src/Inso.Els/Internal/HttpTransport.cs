using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Inso.Els.Internal
{
    /// <summary>
    /// HTTP transport for the ELS API. Implements the same retry / backoff
    /// semantics as the Go SDK: 5xx and 429 are retryable, 4xx (except 429)
    /// are permanent, network errors are retryable. Honors Retry-After.
    /// </summary>
    internal sealed class HttpTransport
    {
        private const string SingleEndpoint = "/errors";
        private const string BatchEndpoint = "/errors/batch";
        private const string HealthEndpoint = "/health";

        private readonly ElsOptions _options;
        private readonly HttpClient _http;
        private readonly bool _ownsHttpClient;
        private readonly string _userAgent;
        private readonly DebugLog _debug;
        private readonly Random _random = new Random();

        public HttpTransport(ElsOptions options, DebugLog debug)
        {
            _options = options;
            _debug = debug;
            if (options.HttpClient is not null)
            {
                _http = options.HttpClient;
                _ownsHttpClient = false;
            }
            else
            {
                _http = new HttpClient { Timeout = options.Timeout };
                _ownsHttpClient = true;
            }
            _userAgent = BuildUserAgent();
        }

        public Task SendBatchAsync(IReadOnlyList<ErrorEntry> entries, CancellationToken ct)
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(
                new BatchRequestDto { Errors = ToArray(entries) },
                JsonSerialization.Default);
            return DoWithRetryAsync(BatchEndpoint, payload, ct);
        }

        public Task SendSingleAsync(ErrorEntry entry, CancellationToken ct)
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(entry, JsonSerialization.Default);
            return DoWithRetryAsync(SingleEndpoint, payload, ct);
        }

        public async Task HealthAsync(CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, _options.Endpoint + HealthEndpoint);
            ApplyAuth(req);
            req.Headers.TryAddWithoutValidation("User-Agent", _userAgent);

            HttpResponseMessage resp;
            try
            {
                resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                throw new ElsSendException(0, isRetryable: true, "Health check failed: " + ex.Message, responseBody: null, inner: ex);
            }

            using (resp)
            {
                if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 300) return;
                var body = await SafeReadAsync(resp).ConfigureAwait(false);
                bool retryable = (int)resp.StatusCode >= 500 || (int)resp.StatusCode == 429;
                throw new ElsSendException(
                    (int)resp.StatusCode,
                    retryable,
                    $"Health check returned HTTP {(int)resp.StatusCode}",
                    body);
            }
        }

        private async Task DoWithRetryAsync(string path, byte[] payload, CancellationToken ct)
        {
            var url = _options.Endpoint + path;
            Exception? lastError = null;
            int? lastStatus = null;

            for (int attempt = 0; attempt <= _options.MaxRetries; attempt++)
            {
                if (attempt > 0)
                {
                    var delay = ComputeBackoff(attempt - 1);
                    _debug.Write("retry attempt={0} delay={1}ms", attempt, (long)delay.TotalMilliseconds);
                    try
                    {
                        await Task.Delay(delay, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                }

                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Content = new ByteArrayContent(payload);
                req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
                ApplyAuth(req);
                req.Headers.TryAddWithoutValidation("User-Agent", _userAgent);

                HttpResponseMessage resp;
                try
                {
                    resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    lastStatus = 0;
                    continue;
                }

                int status;
                string? body;
                TimeSpan? retryAfter;
                using (resp)
                {
                    status = (int)resp.StatusCode;
                    body = await SafeReadAsync(resp).ConfigureAwait(false);
                    retryAfter = ParseRetryAfter(resp);
                }

                if (status >= 200 && status < 300) return;

                if (status == 429 && attempt < _options.MaxRetries)
                {
                    var delay = retryAfter ?? ComputeBackoff(attempt);
                    _debug.Write("429 retry-after={0}ms", (long)delay.TotalMilliseconds);
                    try
                    {
                        await Task.Delay(delay, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    lastStatus = status;
                    lastError = new ElsSendException(429, true, "HTTP 429 Too Many Requests", body);
                    continue;
                }

                if (status >= 500)
                {
                    lastStatus = status;
                    lastError = new ElsSendException(status, true, $"HTTP {status}", body);
                    continue;
                }

                // 4xx (except 429) — permanent
                throw new ElsSendException(status, false, $"HTTP {status}", body);
            }

            if (lastError is ElsSendException se) throw se;
            throw new ElsSendException(
                lastStatus ?? 0,
                isRetryable: true,
                $"Request failed after {_options.MaxRetries} retries",
                responseBody: null,
                inner: lastError);
        }

        private TimeSpan ComputeBackoff(int attempt)
        {
            double baseMs = _options.RetryBaseDelay.TotalMilliseconds * Math.Pow(2, attempt);
            double jitter = (_random.NextDouble() * 0.2 - 0.1) * baseMs; // +/- 10%
            double total = Math.Max(0, baseMs + jitter);
            return TimeSpan.FromMilliseconds(total);
        }

        private static TimeSpan? ParseRetryAfter(HttpResponseMessage resp)
        {
            var ra = resp.Headers.RetryAfter;
            if (ra is null) return null;
            if (ra.Delta.HasValue) return ra.Delta.Value;
            if (ra.Date.HasValue)
            {
                var delta = ra.Date.Value - DateTimeOffset.UtcNow;
                if (delta > TimeSpan.Zero) return delta;
            }
            if (resp.Headers.TryGetValues("Retry-After", out var values))
            {
                foreach (var v in values)
                {
                    if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
                        return TimeSpan.FromSeconds(seconds);
                }
            }
            return null;
        }

        private static async Task<string?> SafeReadAsync(HttpResponseMessage resp)
        {
            try
            {
                if (resp.Content is null) return null;
                return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }

        private void ApplyAuth(HttpRequestMessage req)
        {
            if (_options.AuthScheme == ElsAuthScheme.ApiKeyHeader)
            {
                req.Headers.TryAddWithoutValidation("X-API-Key", _options.ApiKey);
            }
            else
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            }
        }

        private static string BuildUserAgent()
        {
            var asm = typeof(HttpTransport).GetTypeInfo().Assembly;
            var version = asm.GetName().Version?.ToString(3) ?? "0.0.0";
            return $"inso-els-csharp/{version} ({RuntimeDescription()})";
        }

        private static string RuntimeDescription()
        {
            try
            {
#if NETSTANDARD2_0
                return ".NET Standard 2.0";
#else
                return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
#endif
            }
            catch
            {
                return ".NET";
            }
        }

        private static T[] ToArray<T>(IReadOnlyList<T> list)
        {
            var arr = new T[list.Count];
            for (int i = 0; i < list.Count; i++) arr[i] = list[i];
            return arr;
        }

        public void DisposeOwned()
        {
            if (_ownsHttpClient) _http.Dispose();
        }
    }
}
