using Newtonsoft.Json;

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
    }
}