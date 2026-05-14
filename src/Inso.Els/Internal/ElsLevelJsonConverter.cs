using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Inso.Els.Internal
{
    /// <summary>Serializes <see cref="ElsLevel"/> as its lowercase wire string.</summary>
    internal sealed class ElsLevelJsonConverter : JsonConverter<ElsLevel?>
    {
        public override ElsLevel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType == JsonTokenType.String)
            {
                return ElsLevelExtensions.Parse(reader.GetString());
            }
            throw new JsonException($"Unexpected token {reader.TokenType} for ElsLevel.");
        }

        public override void Write(Utf8JsonWriter writer, ElsLevel? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }
            writer.WriteStringValue(value.Value.ToWireValue());
        }
    }

    /// <summary>Serializes <see cref="ElsSource"/> as its lowercase wire string.</summary>
    internal sealed class ElsSourceJsonConverter : JsonConverter<ElsSource?>
    {
        public override ElsSource? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType == JsonTokenType.String)
            {
                return ElsSourceExtensions.Parse(reader.GetString());
            }
            throw new JsonException($"Unexpected token {reader.TokenType} for ElsSource.");
        }

        public override void Write(Utf8JsonWriter writer, ElsSource? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }
            writer.WriteStringValue(value.Value.ToWireValue());
        }
    }
}
