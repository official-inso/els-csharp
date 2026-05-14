namespace Inso.Els.Extensions.Logging
{
    /// <summary>Options for the ELS <see cref="Microsoft.Extensions.Logging.ILoggerProvider"/>.</summary>
    public sealed class ElsLoggerOptions
    {
        /// <summary>
        /// Minimum severity to forward to ELS. Log records below this level
        /// are dropped before they reach the SDK queue. Default: <see cref="ElsLevel.Warning"/>.
        /// </summary>
        public ElsLevel MinLevel { get; set; } = ElsLevel.Warning;

        /// <summary>
        /// URL value attached to every entry when the record itself does not
        /// supply one. Mirrors the <c>els-go</c> SlogHandler default.
        /// </summary>
        public string DefaultUrl { get; set; } = "logger";

        /// <summary>If <c>true</c>, scopes are included in <c>meta</c> under the <c>scope.</c> prefix.</summary>
        public bool IncludeScopes { get; set; } = true;
    }
}
