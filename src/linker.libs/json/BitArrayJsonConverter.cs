using linker.libs.extends;
using System;
using System.Collections;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            try
            {
                Span<char> chars = stackalloc char[value.Length];
                for (int index = 0; index < value.Length; index++)
                {
                    chars[index] = value[index] ? '1' : '0';
                }
                writer.WriteStringValue(new string(chars));
            }
            catch (Exception)
            {
                writer.WriteStringValue(new string('0', value.Length));
            }
        }
    }
}
