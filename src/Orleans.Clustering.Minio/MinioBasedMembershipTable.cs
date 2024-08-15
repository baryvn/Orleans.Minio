
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Clustering.Minio;
using Minio.DataModel.Args;
using Minio;
using System.Text;

namespace Orleans.Runtime.Membership
{

    public class MinioBasedMembershipTable : IMembershipTable
    {
        private readonly ILogger logger;
        private readonly string ClusterId;
        private static readonly TableVersion DefaultTableVersion = new TableVersion(0, "0");

        public bool IsInitialized { get; private set; }
        private readonly IMinioClient _minioClient;
        public MinioBasedMembershipTable(
            IMinioClient minioClient,
            ILogger<MinioBasedMembershipTable> logger,
            IOptions<MinioClusteringSiloOptions> membershipTableOptions,
            IOptions<ClusterOptions> clusterOptions)
        {
            this.logger = logger;
            ClusterId = clusterOptions.Value.ClusterId.ToLower().Replace("_", "-");
            _minioClient = minioClient;
        }
        /// <summary>
        /// Initialize Membership Table
        /// </summary>
        /// <param name="tryInitTableVersion"></param>
        /// <returns></returns>
        public async Task InitializeMembershipTable(bool tryInitTableVersion)
        {
            if (tryInitTableVersion)
            {
                try
                {
                    // Make a bucket on the server, if not already present.
                    var beArgs = new BucketExistsArgs().WithBucket(ClusterId);
                    bool found = await _minioClient.BucketExistsAsync(beArgs).ConfigureAwait(false);
                    if (!found)
                    {
                        var mbArgs = new MakeBucketArgs().WithBucket(ClusterId);
                        await _minioClient.MakeBucketAsync(mbArgs).ConfigureAwait(false);
                    }
                    IsInitialized = true;

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, ex.Message);
                }
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Atomically reads the Membership Table information about a given silo.
        /// The returned MembershipTableData includes one MembershipEntry entry for a given silo and the 
        /// TableVersion for this table. The MembershipEntry and the TableVersion have to be read atomically.
        /// </summary>
        /// <param name="siloAddress">The address of the silo whose membership information needs to be read.</param>
        /// <returns>The membership information for a given silo: MembershipTableData consisting one MembershipEntry entry and
        /// TableVersion, read atomically.</returns>
        public async Task<MembershipTableData> ReadRow(SiloAddress siloAddress)
        {
            try
            {
                TableVersion tableVersion = DefaultTableVersion;
                MembershipTableData memberTable = new MembershipTableData(tableVersion);
                await _minioClient.GetObjectAsync(
                    new GetObjectArgs().WithBucket(ClusterId).WithObject($"tablevertion_{ClusterId}").WithCallbackStream(async (stream) =>
                    {
                        var table = await stream.ToObject<TableVersionData>();
                        if (table != null) tableVersion = new TableVersion(table.Version,table.VersionEtag);
                    })
                );
                await _minioClient.GetObjectAsync(
                    new GetObjectArgs().WithBucket(ClusterId).WithObject(siloAddress.ToString()).WithCallbackStream(async (stream) =>
                    {
                        var member = await stream.ToObject<MembershipEntry>();
                        if (member != null)
                        {
                            var tup = Tuple.Create(member, tableVersion.VersionEtag);
                            if (tup != null)
                            {
                                memberTable = new MembershipTableData(tup, tableVersion);
                            }
                        }
                    })
                );
                return memberTable;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
            return new MembershipTableData(DefaultTableVersion);
        }

        /// <summary>
        /// Atomically reads the full content of the Membership Table.
        /// The returned MembershipTableData includes all MembershipEntry entry for all silos in the table and the 
        /// TableVersion for this table. The MembershipEntries and the TableVersion have to be read atomically.
        /// </summary>
        /// <returns>The membership information for a given table: MembershipTableData consisting multiple MembershipEntry entries and
        /// TableVersion, all read atomically.</returns>
        public async Task<MembershipTableData> ReadAll()
        {
            try
            {
                TableVersion tableVersion = DefaultTableVersion;
                await _minioClient.GetObjectAsync(
                    new GetObjectArgs().WithBucket(ClusterId).WithObject($"tablevertion_{ClusterId}").WithCallbackStream(async (stream) =>
                    {
                        var table = await stream.ToObject<TableVersionData>();
                        if (table != null) tableVersion = new TableVersion(table.Version, table.VersionEtag);
                    })
                );

                List<Tuple<MembershipEntry, string>> members = new List<Tuple<MembershipEntry, string>>();

                // Make a bucket on the server, if not already present.
                var beArgs = new BucketExistsArgs().WithBucket(ClusterId);
                bool found = await _minioClient.BucketExistsAsync(beArgs).ConfigureAwait(false);
                if (found)
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
                                        if (member != null)
                                        {
                                            members.Add(Tuple.Create(member, tableVersion.VersionEtag));
                                        }
                                    })
                            );
                    }
                }
                return new MembershipTableData(members, tableVersion);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
            return new MembershipTableData(DefaultTableVersion);
        }


        /// <summary>
        /// Atomically tries to insert (add) a new MembershipEntry for one silo and also update the TableVersion.
        /// If operation succeeds, the following changes would be made to the table:
        /// 1) New MembershipEntry will be added to the table.
        /// 2) The newly added MembershipEntry will also be added with the new unique automatically generated eTag.
        /// 3) TableVersion.Version in the table will be updated to the new TableVersion.Version.
        /// 4) TableVersion etag in the table will be updated to the new unique automatically generated eTag.
        /// All those changes to the table, insert of a new row and update of the table version and the associated etags, should happen atomically, or fail atomically with no side effects.
        /// The operation should fail in each of the following conditions:
        /// 1) A MembershipEntry for a given silo already exist in the table
        /// 2) Update of the TableVersion failed since the given TableVersion etag (as specified by the TableVersion.VersionEtag property) did not match the TableVersion etag in the table.
        /// </summary>
        /// <param name="entry">MembershipEntry to be inserted.</param>
        /// <param name="tableVersion">The new TableVersion for this table, along with its etag.</param>
        /// <returns>True if the insert operation succeeded and false otherwise.</returns>
        public async Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
        {
            return await InsertOrUpdateMember(entry, new TableVersionData
            {
                VersionEtag = tableVersion.VersionEtag,
                Version = tableVersion.Version,
            }, true);
        }

        /// <summary>
        /// Atomically tries to update the MembershipEntry for one silo and also update the TableVersion.
        /// If operation succeeds, the following changes would be made to the table:
        /// 1) The MembershipEntry for this silo will be updated to the new MembershipEntry (the old entry will be fully substituted by the new entry) 
        /// 2) The eTag for the updated MembershipEntry will also be eTag with the new unique automatically generated eTag.
        /// 3) TableVersion.Version in the table will be updated to the new TableVersion.Version.
        /// 4) TableVersion etag in the table will be updated to the new unique automatically generated eTag.
        /// All those changes to the table, update of a new row and update of the table version and the associated etags, should happen atomically, or fail atomically with no side effects.
        /// The operation should fail in each of the following conditions:
        /// 1) A MembershipEntry for a given silo does not exist in the table
        /// 2) A MembershipEntry for a given silo exist in the table but its etag in the table does not match the provided etag.
        /// 3) Update of the TableVersion failed since the given TableVersion etag (as specified by the TableVersion.VersionEtag property) did not match the TableVersion etag in the table.
        /// </summary>
        /// <param name="entry">MembershipEntry to be updated.</param>
        /// <param name="etag">The etag  for the given MembershipEntry.</param>
        /// <param name="tableVersion">The new TableVersion for this table, along with its etag.</param>
        /// <returns>True if the update operation succeeded and false otherwise.</returns>
        public async Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
        {
            return await InsertOrUpdateMember(entry, new TableVersionData
            {
                VersionEtag = tableVersion.VersionEtag,
                Version = tableVersion.Version,
            }, true);
        }
        private async Task<bool> InsertOrUpdateMember(MembershipEntry entry, TableVersionData tableVersion, bool updateTableVersion)
        {
            try
            {
                if (updateTableVersion)
                {
                    var tbbs = Encoding.UTF8.GetBytes(tableVersion.ToJson());
                    var tbfilestream = new MemoryStream(tbbs);
                    var tbargs = new PutObjectArgs()
                        .WithBucket(ClusterId).WithObject($"tablevertion_{ClusterId}")
                        .WithStreamData(tbfilestream).WithObjectSize(tbfilestream.Length)
                        .WithContentType("application/octet-stream");
                    _ = await _minioClient.PutObjectAsync(tbargs).ConfigureAwait(false);
                }
                var bs = Encoding.UTF8.GetBytes(entry.ToJson());
                var filestream = new MemoryStream(bs);
                var args = new PutObjectArgs()
                    .WithBucket(ClusterId).WithObject($"membership_{entry.SiloAddress.ToString()}")
                    .WithStreamData(filestream).WithObjectSize(filestream.Length)
                    .WithContentType("application/octet-stream");
                _ = await _minioClient.PutObjectAsync(args).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, ex, ex.Message);
            }
            return false;
        }





        /// <summary>
        /// Updates the IAmAlive part (column) of the MembershipEntry for this silo.
        /// This operation should only update the IAmAlive column and not change other columns.
        /// This operation is a "dirty write" or "in place update" and is performed without etag validation. 
        /// With regards to eTags update:
        /// This operation may automatically update the eTag associated with the given silo row, but it does not have to. It can also leave the etag not changed ("dirty write").
        /// With regards to TableVersion:
        /// this operation should not change the TableVersion of the table. It should leave it untouched.
        /// There is no scenario where this operation could fail due to table semantical reasons. It can only fail due to network problems or table unavailability.
        /// </summary>
        /// <param name="entry">The target MembershipEntry tp update</param>
        /// <returns>Task representing the successful execution of this operation. </returns>
        public async Task UpdateIAmAlive(MembershipEntry entry)
        {
            try
            {
                TableVersion tableVersion = DefaultTableVersion;
                MembershipTableData memberTable = new MembershipTableData(tableVersion);
                await _minioClient.GetObjectAsync(
                    new GetObjectArgs().WithBucket(ClusterId).WithObject($"tablevertion_{ClusterId}").WithCallbackStream(async (stream) =>
                    {
                        var table = await stream.ToObject<TableVersionData>();
                        if (table != null) tableVersion = new TableVersion(table.Version, table.VersionEtag);
                    })
                );
                await _minioClient.GetObjectAsync(
                    new GetObjectArgs().WithBucket(ClusterId).WithObject(entry.SiloAddress.ToString()).WithCallbackStream(async (stream) =>
                    {
                        var member = await stream.ToObject<MembershipEntry>();
                        if (member != null)
                        {
                            member.IAmAliveTime = entry.IAmAliveTime;
                            await InsertOrUpdateMember(member, new TableVersionData
                            {
                                VersionEtag = tableVersion.VersionEtag,
                                Version = tableVersion.Version,
                            }, updateTableVersion: false);
                        }
                    })
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }

        /// <summary>
        /// Deletes all table entries of the given clusterId
        /// </summary>
        public async Task DeleteMembershipTableEntries(string clusterId)
        {
            // Make a bucket on the server, if not already present.
            var beArgs = new BucketExistsArgs().WithBucket(ClusterId);
            bool found = await _minioClient.BucketExistsAsync(beArgs).ConfigureAwait(false);
            if (found)
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
                                    if (member != null)
                                    {
                                        var args = new RemoveObjectArgs().WithBucket(ClusterId).WithObject(item.Key);
                                        await _minioClient.RemoveObjectAsync(args).ConfigureAwait(false);
                                    }
                                })
                        );
                }
            }
        }

        public async Task CleanupDefunctSiloEntries(DateTimeOffset beforeDate)
        {
            // Make a bucket on the server, if not already present.
            var beArgs = new BucketExistsArgs().WithBucket(ClusterId);
            bool found = await _minioClient.BucketExistsAsync(beArgs).ConfigureAwait(false);
            if (found)
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
                                    if (member != null && member.Status == SiloStatus.Dead && member.IAmAliveTime < beforeDate)
                                    {
                                        var args = new RemoveObjectArgs().WithBucket(ClusterId).WithObject(item.Key);
                                        await _minioClient.RemoveObjectAsync(args).ConfigureAwait(false);
                                    }
                                })
                        );
                }
            }
        }
    }
}
