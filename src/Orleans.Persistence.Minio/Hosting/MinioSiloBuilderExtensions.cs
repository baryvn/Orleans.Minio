using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Hosting;
using Orleans.Bary.Persistence.Minio.Providers;
using Orleans.Bary.Persistence.Minio.Storage;
using Orleans.Providers;
using Orleans.Runtime.Hosting;
using Orleans.Storage;
using Microsoft.Extensions.Logging;

namespace Orleans.Bary.Persistence.Minio.Hosting;

public static class MinioSiloBuilderExtensions
{
    public static ISiloBuilder AddMinioGrainStorageAsDefault(this ISiloBuilder builder, Action<MinioGrainStorageOptions> options)
    {
        return builder.AddMinioGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, options);
    }

    public static ISiloBuilder AddMinioGrainStorage(this ISiloBuilder builder, string providerName, Action<MinioGrainStorageOptions> options)
    {
        return builder.ConfigureServices(services => services.AddMinioGrainStorage(providerName, options));
    }

    public static IServiceCollection AddMinioGrainStorage(this IServiceCollection services, string providerName, Action<MinioGrainStorageOptions> options)
    {
        services.AddOptions<MinioGrainStorageOptions>(providerName).Configure(options);
        services.AddTransient<IPostConfigureOptions<MinioGrainStorageOptions>, DefaultStorageProviderSerializerOptionsConfigurator<MinioGrainStorageOptions>>();
        services.AddTransient(provider => provider.GetRequiredService<ILoggerFactory>().CreateLogger<MinioGrainStorage>());
        return services.AddGrainStorage(providerName, MinioGrainStorageFactory.Create);
    }
}