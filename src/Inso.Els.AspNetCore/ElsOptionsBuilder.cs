using System;
using System.IO;
using System.Net.Http;

namespace Inso.Els.AspNetCore
{
    /// <summary>
    /// Mutable builder used inside <c>AddEls(Action&lt;ElsOptionsBuilder&gt;)</c>
    /// because the immutable <see cref="ElsOptions"/> record cannot be
    /// configured field-by-field through an <c>Action</c>.
    /// </summary>
    public sealed class ElsOptionsBuilder
    {
        /// <summary>ELS API base URL (required).</summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>API key (required).</summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>Application slug.</summary>
        public string? AppSlug { get; set; }

        /// <summary>Deployment environment.</summary>
        public string? DeploymentEnv { get; set; }

        /// <summary>Microservice name.</summary>
        public string? ServiceName { get; set; }

        /// <summary>Application version (any string up to 128 chars).</summary>
        public string? AppVersion { get; set; }

        /// <summary>Max entries per batch.</summary>
        public int BatchSize { get; set; } = 50;

        /// <summary>Max time before flushing a partial batch.</summary>
        public TimeSpan BatchInterval { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>In-memory queue capacity.</summary>
        public int BufferSize { get; set; } = 1000;

        /// <summary>Retry attempts.</summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>Initial retry delay.</summary>
        public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>HTTP request timeout.</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>Max time <c>FlushAsync</c> waits.</summary>
        public TimeSpan FlushTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>Disk buffer directory.</summary>
        public string? BufferDir { get; set; }

        /// <summary>Disk buffer file size cap.</summary>
        public long MaxBufferFileSize { get; set; } = 100L * 1024 * 1024;

        /// <summary>Disk buffer file name.</summary>
        public string BufferFileName { get; set; } = ".els-buffer.jsonl";

        /// <summary>Minimum level to capture.</summary>
        public ElsLevel? MinLevel { get; set; }

        /// <summary>Sampling rate (0.0 – 1.0).</summary>
        public double SampleRate { get; set; } = 1.0;

        /// <summary>Optional filter/mutator hook.</summary>
        public Func<ErrorEntry, ErrorEntry?>? BeforeSend { get; set; }

        /// <summary>Optional internal-error callback.</summary>
        public Action<Exception>? OnError { get; set; }

        /// <summary>Default level applied when an entry has none.</summary>
        public ElsLevel DefaultLevel { get; set; } = ElsLevel.Error;

        /// <summary>Default source applied when an entry has none.</summary>
        public ElsSource DefaultSource { get; set; } = ElsSource.Server;

        /// <summary>Optional custom <see cref="HttpClient"/>.</summary>
        public HttpClient? HttpClient { get; set; }

        /// <summary>How the API key is sent.</summary>
        public ElsAuthScheme AuthScheme { get; set; } = ElsAuthScheme.Bearer;

        /// <summary>Verbose internal logging.</summary>
        public bool Debug { get; set; }

        /// <summary>Where verbose logs go.</summary>
        public TextWriter? DebugWriter { get; set; }

        /// <summary>Builds an immutable <see cref="ElsOptions"/> from this builder.</summary>
        public ElsOptions Build()
        {
            return new ElsOptions
            {
                Endpoint = Endpoint,
                ApiKey = ApiKey,
                AppSlug = AppSlug,
                DeploymentEnv = DeploymentEnv,
                ServiceName = ServiceName,
                AppVersion = AppVersion,
                BatchSize = BatchSize,
                BatchInterval = BatchInterval,
                BufferSize = BufferSize,
                MaxRetries = MaxRetries,
                RetryBaseDelay = RetryBaseDelay,
                Timeout = Timeout,
                FlushTimeout = FlushTimeout,
                BufferDir = BufferDir,
                MaxBufferFileSize = MaxBufferFileSize,
                BufferFileName = BufferFileName,
                MinLevel = MinLevel,
                SampleRate = SampleRate,
                BeforeSend = BeforeSend,
                OnError = OnError,
                DefaultLevel = DefaultLevel,
                DefaultSource = DefaultSource,
                HttpClient = HttpClient,
                AuthScheme = AuthScheme,
                Debug = Debug,
                DebugWriter = DebugWriter,
            };
        }
    }
}
