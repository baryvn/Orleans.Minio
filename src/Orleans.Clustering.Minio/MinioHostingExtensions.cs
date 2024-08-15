using Microsoft.Extensions.DependencyInjection;
using Orleans.Messaging;
using Orleans.Runtime.Membership;
using Orleans.Configuration;
using Minio;

namespace Orleans.Hosting
{

    public static class MinioHostingExtensions
    {
        /// <summary>
        /// Configures the silo to use Minio for cluster membership.
        /// </summary>
        /// <param name="builder">
        /// The builder.
        /// </param>
        /// <param name="configureOptions">
        /// The configuration delegate.
        /// </param>
        /// <returns>
        /// The provided <see cref="ISiloBuilder"/>.
        /// </returns>
        public static ISiloBuilder UseMinioClustering(
            this ISiloBuilder builder,
            Action<MinioClusteringSiloOptions> configureOptions)
        {
            return builder.ConfigureServices(
                services =>
                {
                    if (configureOptions != null)
                    {
                        services.Configure(configureOptions);
                        MinioClusteringSiloOptions option = new MinioClusteringSiloOptions { AccessKey = string.Empty, Endpoint = string.Empty, SecretKey = string.Empty, UseSSl = false };
                        configureOptions.Invoke(option);
                        services.AddMinio(configureClient => configureClient
                                .WithEndpoint(option.Endpoint)
                                .WithCredentials(option.AccessKey, option.SecretKey)
                                .WithSSL(option.UseSSl)
                                .Build());
                    }
                    services.AddSingleton<IMembershipTable, MinioBasedMembershipTable>();
                });
        }


        /// <summary>
        /// Configure the client to use Minio for clustering.
        /// </summary>
        /// <param name="builder">
        /// The builder.
        /// </param>
        /// <param name="configureOptions">
        /// The configuration delegate.
        /// </param>
        /// <returns>
        /// The provided <see cref="IClientBuilder"/>.
        /// </returns>
        public static IClientBuilder UseMinioClustering(
            this IClientBuilder builder,
            Action<MinioGatewayListProviderOptions> configureOptions)
        {
            return builder.ConfigureServices(
                services =>
                {
                    if (configureOptions != null)
                    {
                        services.Configure(configureOptions);
                        MinioGatewayListProviderOptions option = new MinioGatewayListProviderOptions { AccessKey = string.Empty, Endpoint = string.Empty, SecretKey = string.Empty, UseSSl = false };
                        configureOptions.Invoke(option);
                        services.AddMinio(configureClient => configureClient
                                .WithEndpoint(option.Endpoint)
                                .WithCredentials(option.AccessKey, option.SecretKey)
                                .WithSSL(option.UseSSl)
                                .Build());
                    }
                    services.AddSingleton<IGatewayListProvider, MinioGatewayListProvider>();
                });
        }

    }
}
