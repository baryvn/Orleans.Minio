namespace Orleans.Reminders.Minio
{
    public class MinioReminderStorageOptions
    {
        public required string Endpoint { get; set; }
        public required string AccessKey { get; set; } = string.Empty;
        public required string SecretKey { get; set; } = string.Empty;

    }
}