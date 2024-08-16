using Orleans.Storage;

namespace Orleans.Bary.Persistence.Minio.Providers;

public class MinioGrainStorageOptions : IStorageProviderSerializerOptions
{
    public required string Endpoint { get; set; }
    public required string AccessKey { get; set; } = string.Empty;
    public required string SecretKey { get; set; } = string.Empty;
    public required bool UseSSl { get; set; } = false;

    public required IGrainStorageSerializer GrainStorageSerializer { get; set; }
}