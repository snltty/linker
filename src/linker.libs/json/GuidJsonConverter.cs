using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace linker.libs.json
{
    public sealed class GuidJsonConverter : JsonConverter<Guid>
    {
        public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                return Guid.Parse(stringValue);
            }

            return Guid.Parse(reader.GetString());
        }
        public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
