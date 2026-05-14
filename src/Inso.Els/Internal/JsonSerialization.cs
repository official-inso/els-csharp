using System.Text.Json;
using System.Text.Json.Serialization;

namespace Inso.Els.Internal
{
    /// <summary>Shared JSON settings for the wire format and the disk buffer.</summary>
    internal static class JsonSerialization
    {
        internal static readonly JsonSerializerOptions Default = CreateDefault();

        private static JsonSerializerOptions CreateDefault()
        {
            var opts = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false,
            };
            opts.Converters.Add(new ElsLevelJsonConverter());
            opts.Converters.Add(new ElsSourceJsonConverter());
            return opts;
        }
    }

    /// <summary>Internal request shape for <c>POST /errors/batch</c>.</summary>
    internal sealed class BatchRequestDto
    {
        [JsonPropertyName("errors")]
        public ErrorEntry[] Errors { get; set; } = System.Array.Empty<ErrorEntry>();
    }
}
