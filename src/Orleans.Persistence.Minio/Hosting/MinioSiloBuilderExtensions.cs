using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using Orleans.Hosting;
using Orleans.Persistence.Minio.Providers;
using Orleans.Persistence.Minio.Storage;
using Orleans.Providers;
using Orleans.Runtime.Hosting;
using Orleans.Storage;

namespace Orleans.Persistence.Minio.Hosting;

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
        MinioGrainStorageOptions option = new MinioGrainStorageOptions
        {
            AccessKey = string.Empty,
            Endpoint = string.Empty,
            SecretKey = string.Empty,
            UseSSl = false,
            GrainStorageSerializer = null
        };
        options.Invoke(option);
        services.AddMinio(configureClient => configureClient
                .WithEndpoint(option.Endpoint)
                .WithCredentials(option.AccessKey, option.SecretKey)
                .WithSSL(option.UseSSl)
                .Build());

        services.AddTransient<IPostConfigureOptions<MinioGrainStorageOptions>, DefaultStorageProviderSerializerOptionsConfigurator<MinioGrainStorageOptions>>();
        return services.AddGrainStorage(providerName, MinioGrainStorageFactory.Create);
    }
}