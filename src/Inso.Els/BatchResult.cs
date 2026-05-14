namespace Inso.Els
{
    /// <summary>Response from the ELS batch ingest endpoint.</summary>
    public sealed record BatchResult
    {
        /// <summary>Number of entries created server-side.</summary>
        public int Created { get; init; }

        /// <summary>Server-side error message, if any.</summary>
        public string? Error { get; init; }
    }
}
