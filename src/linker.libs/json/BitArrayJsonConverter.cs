using System;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Collections;
using System.Linq;

namespace linker.libs.json
{
    public sealed class BitArrayJsonConverter : JsonConverter<BitArray>
    {
        public override BitArray Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new BitArray(reader.GetString().Select(c => c == '1').ToArray());
        }

        public override void Write(Utf8JsonWriter writer, BitArray value, JsonSerializerOptions options)
        {
            Span<char> chars = stackalloc char[value.Length];
            for (int i = 0; i < value.Length; i++)
            {
                chars[i] = value[i] ? '1' : '0';
            }
            writer.WriteStringValue(new string(chars));
        }
    }
}
