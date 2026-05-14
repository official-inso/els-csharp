using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Inso.Els
{
    /// <summary>Configuration for an <see cref="ElsClient"/>.</summary>
    public sealed record ElsOptions
    {
        // ---------- Required ----------

        /// <summary>ELS API base URL, e.g. <c>https://api.insoweb.ru/els</c>.</summary>
        public string Endpoint { get; init; } = string.Empty;

        /// <summary>API key. Sent according to <see cref="AuthScheme"/>.</summary>
        public string ApiKey { get; init; } = string.Empty;

        // ---------- Identity (recommended) ----------

        /// <summary>Application slug, e.g. <c>my-service</c>.</summary>
        public string? AppSlug { get; init; }

        /// <summary>Deployment environment, e.g. <c>DEV</c>, <c>STAGING</c>, <c>PRODUCTION</c>.</summary>
        public string? DeploymentEnv { get; init; }

        /// <summary>Microservice name.</summary>
        public string? ServiceName { get; init; }

        /// <summary>Application version (any string up to 128 chars).</summary>
        public string? AppVersion { get; init; }

        // ---------- Batching ----------

        /// <summary>Maximum entries per batch request. Default: 50.</summary>
        public int BatchSize { get; init; } = 50;

        /// <summary>Maximum time to wait before flushing a partial batch. Default: 5s.</summary>
        public TimeSpan BatchInterval { get; init; } = TimeSpan.FromSeconds(5);

        /// <summary>In-memory queue capacity. When full, oldest entries are dropped. Default: 1000.</summary>
        public int BufferSize { get; init; } = 1000;

        // ---------- Retry / timeouts ----------

        /// <summary>Number of retry attempts for failed requests. Default: 3.</summary>
        public int MaxRetries { get; init; } = 3;

        /// <summary>Initial delay between retries (doubles each attempt). Default: 1s.</summary>
        public TimeSpan RetryBaseDelay { get; init; } = TimeSpan.FromSeconds(1);

        /// <summary>HTTP request timeout. Default: 10s.</summary>
        public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);

        /// <summary>Maximum time <see cref="IElsClient.FlushAsync"/> will wait. Default: 10s.</summary>
        public TimeSpan FlushTimeout { get; init; } = TimeSpan.FromSeconds(10);

        // ---------- Disk buffer ----------

        /// <summary>Directory for the disk buffer file. Null = <see cref="Path.GetTempPath"/>.</summary>
        public string? BufferDir { get; init; }

        /// <summary>Maximum size of the disk buffer file in bytes. Default: 100 MB.</summary>
        public long MaxBufferFileSize { get; init; } = 100L * 1024 * 1024;

        /// <summary>Disk buffer file name. Compatible with the Go SDK default.</summary>
        public string BufferFileName { get; init; } = ".els-buffer.jsonl";

        // ---------- Filtering / sampling ----------

        /// <summary>Minimum severity level to capture. Null = no filter.</summary>
        public ElsLevel? MinLevel { get; init; }

        /// <summary>
        /// Fraction of non-critical entries to actually send (0.0 – 1.0).
        /// Entries at level <see cref="ElsLevel.Critical"/> bypass the filter
        /// when <see cref="AlwaysCaptureCritical"/> is true (default). Default: 1.0.
        /// </summary>
        public double SampleRate { get; init; } = 1.0;

        /// <summary>
        /// When true (default), critical-level entries are always captured regardless
        /// of <see cref="SampleRate"/>. Set to false to subject criticals to sampling too.
        /// </summary>
        public bool AlwaysCaptureCritical { get; init; } = true;

        // ---------- Hooks ----------

        /// <summary>
        /// Called before each entry is enqueued. Return null to drop the entry,
        /// or return a (possibly modified) entry to send. Exceptions thrown by
        /// the hook are caught and forwarded to <see cref="OnError"/>.
        /// </summary>
        public Func<ErrorEntry, ErrorEntry?>? BeforeSend { get; init; }

        /// <summary>
        /// Asynchronous variant of <see cref="BeforeSend"/>. When both are set, the
        /// async hook runs first. Use for filters that need I/O (e.g. PII lookup).
        /// </summary>
        public Func<ErrorEntry, ValueTask<ErrorEntry?>>? BeforeSendAsync { get; init; }

        /// <summary>
        /// Called when the SDK hits an internal error (failed send, disk
        /// write error, hook exception). Must not throw.
        /// </summary>
        public Action<Exception>? OnError { get; init; }

        // ---------- Defaults ----------

        /// <summary>Default level applied when an entry has no <see cref="ErrorEntry.Level"/>.</summary>
        public ElsLevel DefaultLevel { get; init; } = ElsLevel.Error;

        /// <summary>Default source applied when an entry has no <see cref="ErrorEntry.Source"/>.</summary>
        public ElsSource DefaultSource { get; init; } = ElsSource.Server;

        // ---------- Advanced ----------

        /// <summary>Custom <see cref="HttpClient"/>. The SDK does not dispose it.</summary>
        public HttpClient? HttpClient { get; init; }

        /// <summary>How the API key is sent. Default: <see cref="ElsAuthScheme.Bearer"/>.</summary>
        public ElsAuthScheme AuthScheme { get; init; } = ElsAuthScheme.Bearer;

        /// <summary>Verbose internal logging.</summary>
        public bool Debug { get; init; }

        /// <summary>Where verbose logs go when <see cref="Debug"/> is on. Default: <see cref="Console.Error"/>.</summary>
        public TextWriter? DebugWriter { get; init; }

        /// <summary>
        /// When <see cref="Sdk.Init(ElsOptions)"/> is used and this is <c>true</c>,
        /// the SDK registers <c>AppDomain.ProcessExit</c> to call
        /// <see cref="Sdk.Close"/> automatically. Default: <c>true</c>.
        /// Set to <c>false</c> if you manage shutdown yourself or if your host
        /// already calls <see cref="Sdk.CloseAsync(System.Threading.CancellationToken)"/>.
        /// Ignored when constructing <see cref="ElsClient"/> directly.
        /// </summary>
        public bool AutoFlushOnExit { get; init; } = true;

        /// <summary>
        /// Inspects this <see cref="ElsOptions"/> and returns a list of problems
        /// found. Empty list = configuration is acceptable. Unlike the
        /// constructor of <see cref="ElsClient"/>, this method does not throw —
        /// callers can use it for startup linting or health endpoints.
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<string> Validate()
        {
            var issues = new System.Collections.Generic.List<string>();
            if (string.IsNullOrWhiteSpace(Endpoint)) issues.Add("Endpoint is required.");
            if (string.IsNullOrWhiteSpace(ApiKey)) issues.Add("ApiKey is required.");
            if (!string.IsNullOrEmpty(Endpoint) && !Uri.TryCreate(Endpoint, UriKind.Absolute, out _))
                issues.Add($"Endpoint '{Endpoint}' is not a valid absolute URI.");
            if (BatchSize <= 0) issues.Add("BatchSize must be positive.");
            if (BufferSize <= 0) issues.Add("BufferSize must be positive.");
            if (MaxRetries < 0) issues.Add("MaxRetries must not be negative.");
            if (BatchInterval <= TimeSpan.Zero) issues.Add("BatchInterval must be positive.");
            if (RetryBaseDelay <= TimeSpan.Zero) issues.Add("RetryBaseDelay must be positive.");
            if (Timeout <= TimeSpan.Zero) issues.Add("Timeout must be positive.");
            if (FlushTimeout <= TimeSpan.Zero) issues.Add("FlushTimeout must be positive.");
            if (MaxBufferFileSize <= 0) issues.Add("MaxBufferFileSize must be positive.");
            if (double.IsNaN(SampleRate) || SampleRate < 0.0 || SampleRate > 1.0)
                issues.Add($"SampleRate must be in [0.0, 1.0] (got {SampleRate}).");
            if (!string.IsNullOrEmpty(AppVersion) && AppVersion!.Length > 128)
                issues.Add($"AppVersion must be at most 128 characters (got {AppVersion.Length}).");
            return issues;
        }

        // ---------- Internal ----------

        internal ElsOptions Normalize()
        {
            if (string.IsNullOrWhiteSpace(Endpoint))
                throw new ElsConfigurationException("ElsOptions.Endpoint is required.");
            if (string.IsNullOrWhiteSpace(ApiKey))
                throw new ElsConfigurationException("ElsOptions.ApiKey is required.");

            var sample = SampleRate;
            if (double.IsNaN(sample) || sample < 0.0 || sample > 1.0) sample = 1.0;

            return this with
            {
                Endpoint = Endpoint.TrimEnd('/'),
                BatchSize = BatchSize <= 0 ? 50 : BatchSize,
                BatchInterval = BatchInterval <= TimeSpan.Zero ? TimeSpan.FromSeconds(5) : BatchInterval,
                BufferSize = BufferSize <= 0 ? 1000 : BufferSize,
                MaxRetries = MaxRetries < 0 ? 3 : MaxRetries,
                RetryBaseDelay = RetryBaseDelay <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : RetryBaseDelay,
                Timeout = Timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(10) : Timeout,
                FlushTimeout = FlushTimeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(10) : FlushTimeout,
                MaxBufferFileSize = MaxBufferFileSize <= 0 ? 100L * 1024 * 1024 : MaxBufferFileSize,
                BufferFileName = string.IsNullOrWhiteSpace(BufferFileName) ? ".els-buffer.jsonl" : BufferFileName,
                SampleRate = sample,
            };
        }
    }
}
