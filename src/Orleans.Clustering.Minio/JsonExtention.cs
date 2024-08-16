using System.Text.Json;

namespace Orleans.Clustering.Minio
{
    public static class JsonExtention
    {
        public static async Task<T?> ToObject<T>(this Stream stream)
        {
            try
            {
                if (stream == null || stream.CanRead == false)
                {
                    return default;
                }

                return await JsonSerializer.DeserializeAsync<T>(stream);
            }
            catch { }
            return default;
        }
        public static string ToJson(this object obj)
        {
            try
            {
                if (obj == null)
                {
                    return string.Empty;
                }

                return JsonSerializer.Serialize(obj);
            }
            catch { }
            return string.Empty;
        }
    }
}
