namespace Orleans.Persistence.Minio.Storage;

internal class BaseStateModel<T>
{
    public string GrainId { get; set; } = string.Empty;

    public string ETag { get; set; } = string.Empty;

    public T? State { get; set; }
}