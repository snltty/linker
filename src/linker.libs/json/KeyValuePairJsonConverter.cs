using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace linker.libs.json
{
    public class KeyValuePairJsonConverter<TKey, TValue> : JsonConverter<KeyValuePair<TKey, TValue>>
    {
        public override KeyValuePair<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                var root = doc.RootElement;
                TKey key = JsonSerializer.Deserialize<TKey>(root.GetProperty("Key").GetRawText());
                TValue value = JsonSerializer.Deserialize<TValue>(root.GetProperty("Value").GetRawText());
                return new KeyValuePair<TKey, TValue>(key, value);
            }
        }

        public override void Write(Utf8JsonWriter writer, KeyValuePair<TKey, TValue> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Key");
            JsonSerializer.Serialize(writer, value.Key, options);
            writer.WritePropertyName("Value");
            JsonSerializer.Serialize(writer, value.Value, options);
            writer.WriteEndObject();
        }
    }

}
