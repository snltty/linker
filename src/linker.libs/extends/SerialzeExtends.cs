using linker.libs.json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace linker.libs.extends
{
    public static class SerialzeExtends
    {
        private static JsonSerializerOptions jsonSerializerOptions1 = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All),
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            PropertyNameCaseInsensitive = true,
            Converters = { new IPAddressJsonConverter(), new IPEndpointJsonConverter(), new DateTimeJsonConverter(), new BitArrayJsonConverter() }
        };
        private static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All),
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new IPAddressJsonConverter(), new IPEndpointJsonConverter(), new DateTimeJsonConverter(), new BitArrayJsonConverter()},
        };
        public static void AddAOT(JsonSerializerContext[] contexts)
        {
            foreach (var context in contexts)
            {
                jsonSerializerOptions1.TypeInfoResolverChain.Insert(0, context);
                jsonSerializerOptions.TypeInfoResolverChain.Insert(0, context);
            }
        }
        public static void AddJsonConverter(JsonConverter jsonConverter)
        {
            jsonSerializerOptions.Converters.Add(jsonConverter);
            jsonSerializerOptions1.Converters.Add(jsonConverter);
        }

        public static string ToJson(this object obj)
        {
            return JsonSerializer.Serialize(obj, jsonSerializerOptions1);
        }
        public static string ToJsonFormat(this object obj)
        {
            return JsonSerializer.Serialize(obj, jsonSerializerOptions);
        }
        public static T DeJson<T>(this string json)
        {
            return JsonSerializer.Deserialize<T>(json, options: jsonSerializerOptions);
        }
    }


}
