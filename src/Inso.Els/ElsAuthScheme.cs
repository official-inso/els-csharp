namespace Inso.Els
{
    /// <summary>How the API key is sent to the ELS API.</summary>
    public enum ElsAuthScheme
    {
        /// <summary>Send as <c>Authorization: Bearer &lt;key&gt;</c>. Default.</summary>
        Bearer = 0,

        /// <summary>Send as <c>X-API-Key: &lt;key&gt;</c>.</summary>
        ApiKeyHeader = 1,
    }
}
