using Orleans.Storage;

namespace Orleans.Bary.Persistence.Minio.Providers;

public class MinioGrainStorageOptions : IStorageProviderSerializerOptions
{
    public required IGrainStorageSerializer GrainStorageSerializer { get; set; }
}