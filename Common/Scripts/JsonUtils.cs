using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FastDragon
{
    public static class JsonUtils
    {
        private static JsonSerializerSettings _newtonsoftSettings => new()
        {
            Formatting = Formatting.Indented,
        };

        public static T FromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static string ToJson<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj, _newtonsoftSettings);
        }

        public static int? PeekInt(string json, string propertyName)
        {
            var jobj = JObject.Parse(json);
            return jobj.GetValue(nameof(SaveFile.SaveFormatVersion))?.ToObject<int>();
        }
    }
}