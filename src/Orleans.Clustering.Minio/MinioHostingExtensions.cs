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
        public static ISiloBuilder UseMinioClustering(this ISiloBuilder builder)
        {
            return builder.ConfigureServices(
                services =>
                {
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
        public static IClientBuilder UseMinioClustering(this IClientBuilder builder)
        {
            return builder.ConfigureServices(
                services =>
                {
                    services.AddSingleton<IGatewayListProvider, MinioGatewayListProvider>();
                });
        }

    }
}
