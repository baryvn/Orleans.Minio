using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Configuration.Overrides;
using Orleans.Bary.Persistence.Minio.Providers;

namespace Orleans.Bary.Persistence.Minio.Storage;

public static class MinioGrainStorageFactory
{
    public static MinioGrainStorage Create(IServiceProvider service, string name)
    {
        var options = service.GetRequiredService<IOptionsMonitor<MinioGrainStorageOptions>>();

        return ActivatorUtilities.CreateInstance<MinioGrainStorage>(service, name, options.Get(name), service.GetProviderClusterOptions(name));
    }
}