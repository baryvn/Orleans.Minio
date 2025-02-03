using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Minio;

namespace Orleans.Bary.Persistence.Minio.Storage;

public static class MinioGrainStorageFactory
{
    public static MinioGrainStorage Create(IServiceProvider service, string name)
    {
        var logger = service.GetRequiredService<ILogger<MinioGrainStorage>>();
        var minioClient = service.GetRequiredService<IMinioClient>();
        return ActivatorUtilities.CreateInstance<MinioGrainStorage>(service, name, logger, minioClient);
    }
}