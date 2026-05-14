#if NET8_0_OR_GREATER
using System.Text.Json.Serialization;

namespace Inso.Els.Internal
{
    /// <summary>
    /// Source-generated <see cref="JsonSerializerContext"/> for the SDK's
    /// top-level payload types (<see cref="ErrorEntry"/> and
    /// <see cref="BatchRequestDto"/>). Active on <c>net8.0</c> and newer; on
    /// older TFMs the SDK falls back to the reflection-based serializer.
    ///
    /// <para>The <see cref="ErrorEntry.Meta"/> dictionary stays reflection-based
    /// because its values are <c>object?</c> (polymorphic) and source-gen
    /// cannot describe them ahead of time. Native-AOT publish therefore still
    /// produces warnings for callers that exercise <c>Meta</c>. See
    /// <c>docs/FUTURE_IMPROVEMENTS.md</c> for the long-term plan.</para>
    /// </summary>
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false)]
    [JsonSerializable(typeof(ErrorEntry))]
    [JsonSerializable(typeof(BatchRequestDto))]
    internal sealed partial class ElsJsonContext : JsonSerializerContext
    {
    }
}
#endif
