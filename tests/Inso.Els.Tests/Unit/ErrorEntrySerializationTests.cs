using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Inso.Els.Internal;
using Xunit;

namespace Inso.Els.Tests.Unit
{
    public class ErrorEntrySerializationTests
    {
        [Fact]
        public void Serialize_UsesCamelCaseAndOmitsNulls()
        {
            var entry = new ErrorEntry
            {
                Message = "boom",
                Url = "/api",
                Level = ElsLevel.Critical,
                Source = ElsSource.Server,
                Meta = new Dictionary<string, object?> { ["k"] = 1 },
            };

            var json = JsonSerializer.Serialize(entry, JsonSerialization.Default);

            json.Should().Contain("\"message\":\"boom\"");
            json.Should().Contain("\"url\":\"/api\"");
            json.Should().Contain("\"level\":\"critical\"");
            json.Should().Contain("\"source\":\"server\"");
            json.Should().Contain("\"meta\":{\"k\":1}");

            // Null fields omitted
            json.Should().NotContain("\"stack\"");
            json.Should().NotContain("\"componentStack\"");
            json.Should().NotContain("\"userAgent\"");
            json.Should().NotContain("\"timestamp\":null");
        }

        [Fact]
        public void Serialize_BatchWrapper()
        {
            var batch = new BatchRequestDto
            {
                Errors = new[]
                {
                    new ErrorEntry { Message = "a", Url = "u" },
                    new ErrorEntry { Message = "b", Url = "u" },
                },
            };

            var json = JsonSerializer.Serialize(batch, JsonSerialization.Default);

            json.Should().StartWith("{\"errors\":[");
            json.Should().Contain("\"message\":\"a\"");
            json.Should().Contain("\"message\":\"b\"");
        }

        [Fact]
        public void Roundtrip_PreservesFields()
        {
            var original = new ErrorEntry
            {
                Message = "boom",
                Url = "/api",
                Level = ElsLevel.Warning,
                Source = ElsSource.Client,
                AppSlug = "svc",
                Meta = new Dictionary<string, object?>
                {
                    ["count"] = 42,
                    ["name"] = "value",
                },
            };

            var json = JsonSerializer.Serialize(original, JsonSerialization.Default);
            var deserialized = JsonSerializer.Deserialize<ErrorEntry>(json, JsonSerialization.Default);

            deserialized.Should().NotBeNull();
            deserialized!.Message.Should().Be("boom");
            deserialized.Url.Should().Be("/api");
            deserialized.Level.Should().Be(ElsLevel.Warning);
            deserialized.Source.Should().Be(ElsSource.Client);
            deserialized.AppSlug.Should().Be("svc");
            deserialized.Meta.Should().NotBeNull();
            deserialized.Meta!.Should().ContainKey("count");
        }
    }
}
