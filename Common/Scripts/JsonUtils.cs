namespace FastDragon
{
    public static class JsonUtils
    {
        private static System.Text.Json.JsonSerializerOptions _jsonSettings => new()
        {
            WriteIndented = true,
            IncludeFields = true,
            IgnoreReadOnlyProperties = true,
            IgnoreReadOnlyFields = true,
        };

        public static T FromJson<T>(string json)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, _jsonSettings);
        }

        public static string ToJson<T>(T obj)
        {
            return System.Text.Json.JsonSerializer.Serialize(obj, _jsonSettings);
        }

        public static int? PeekInt(string json, string propertyName)
        {
            var doc = System.Text.Json.JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty(propertyName, out var property))
                return null;

            if (!property.TryGetInt32(out int result))
                return null;

            return result;
        }
    }
}