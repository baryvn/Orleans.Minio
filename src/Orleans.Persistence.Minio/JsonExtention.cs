using System.Text.Json;

namespace Orleans.Persistence.Minio
{
    public static class JsonExtention
    {
        public static async Task<T?> ToObject<T>(this Stream stream)
        {
            if (stream == null || stream.CanRead == false)
            {
                return default;
            }

            return await JsonSerializer.DeserializeAsync<T>(stream);
        }
        public static string ToJson(this object obj)
        {
            if (obj == null)
            {
                return string.Empty;
            }

            return JsonSerializer.Serialize(obj);
        }
    }
}
