namespace Orleans.Configuration
{
    /// <summary>
    /// Option to configure MinioMembership
    /// </summary>
    public class MinioClusteringSiloOptions
    {
        /// <summary>
        /// Connection string for Minio storage
        /// </summary>
        public required string Endpoint { get; set; } = "http://localhost:4001";
        public required string AccessKey { get; set; } = string.Empty;
        public required string SecretKey { get; set; } = string.Empty;
        public required bool UseSSl { get; set; } = false;
    }
}
