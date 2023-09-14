using common.libs.jsonConverters;
using System.Text.Json;
using System.Text.Unicode;

namespace common.libs.extends
{
    public static class SerialzeExtends
    {
        private static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All),
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new IPAddressJsonConverter(), new IPEndpointJsonConverter(), new DateTimeConverter() }
        };
        private static JsonSerializerOptions jsonSerializerOptionsIndented = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All),
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new IPAddressJsonConverter(), new IPEndpointJsonConverter(), new DateTimeConverter() }
        };
        public static string ToJson(this object obj)
        {
            return JsonSerializer.Serialize(obj, jsonSerializerOptions);
        }
        public static string ToJsonIndented(this object obj)
        {
            return JsonSerializer.Serialize(obj, jsonSerializerOptionsIndented);
        }
        public static T DeJson<T>(this string json)
        {
            return JsonSerializer.Deserialize<T>(json, options: jsonSerializerOptions);
        }
    }
}
