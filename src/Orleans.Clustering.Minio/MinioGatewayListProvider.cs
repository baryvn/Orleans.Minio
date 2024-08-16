using Orleans.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Clustering.Minio;
using Minio;
using Minio.DataModel.Args;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Orleans.Runtime.Membership
{
    public class MinioGatewayListProvider : IGatewayListProvider
    {
        private readonly ILogger logger;
        private readonly string ClusterId;
        private readonly TimeSpan _maxStaleness;
        IMinioClient _minioClient;

        public MinioGatewayListProvider(
            IMinioClient minioClient,
            ILogger<MinioGatewayListProvider> logger,
            IOptions<GatewayOptions> gatewayOptions,
            IOptions<ClusterOptions> clusterOptions)
        {
            _minioClient = minioClient;
            this.logger = logger;
            ClusterId = clusterOptions.Value.ClusterId.ToLower().Replace("_","-");
            _maxStaleness = gatewayOptions.Value.GatewayListRefreshPeriod;
        }

        /// <summary>
        /// Initializes the Minio based gateway provider
        /// </summary>
        public Task InitializeGatewayListProvider() => Task.CompletedTask;

        /// <summary>
        /// Returns the list of gateways (silos) that can be used by a client to connect to Orleans cluster.
        /// The Uri is in the form of: "gwy.tcp://IP:port/Generation". See Utils.ToGatewayUri and Utils.ToSiloAddress for more details about Uri format.
        /// </summary>
        public async Task<IList<Uri>> GetGateways()
        {
            List<Uri> dataRs = new List<Uri>();

            // Make a bucket on the server, if not already present.
            var beArgs = new BucketExistsArgs().WithBucket(ClusterId);
            bool found = await _minioClient.BucketExistsAsync(beArgs).ConfigureAwait(false);
            if (found)
            {
                try
                {
                    var listArgs = new ListObjectsArgs().WithBucket(ClusterId).WithPrefix("membership_").WithRecursive(false);
                    await foreach (var item in _minioClient.ListObjectsEnumAsync(listArgs).ConfigureAwait(false))
                    {
                        await _minioClient.GetObjectAsync(
                                new GetObjectArgs()
                                    .WithBucket(ClusterId)
                                    .WithObject(item.Key)
                                    .WithCallbackStream(async (stream) =>
                                    {
                                        var member = await stream.ToObject<MembershipEntry>();
                                        if (member != null && member.SiloAddress != null && member.Status == SiloStatus.Active && member.ProxyPort > 0)
                                        {
                                            member.SiloAddress.Endpoint.Port = member.ProxyPort;
                                            dataRs.Add(member.SiloAddress.ToGatewayUri());
                                        }
                                    })
                            );
                    }
                }
                catch (Exception ex) {
                    logger.LogError(ex.Message);
                }

            }
            return dataRs;
        }

        /// <summary>
        /// Specifies how often this IGatewayListProvider is refreshed, to have a bound on max staleness of its returned information.
        /// </summary>
        public TimeSpan MaxStaleness => _maxStaleness;

        /// <summary>
        /// Specifies whether this IGatewayListProvider ever refreshes its returned information, or always returns the same gw list.
        /// (currently only the static config based StaticGatewayListProvider is not updatable. All others are.)
        /// </summary>
        public bool IsUpdatable => true;
    }
}
//tôi muốn tạo một lớp triển khai interface trên thì cần luu ý điều gì