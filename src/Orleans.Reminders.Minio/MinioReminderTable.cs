
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Orleans.Configuration;

namespace Orleans.Reminders.Minio
{
    public class MinioReminderTable : IReminderTable
    {
        private readonly ILogger _logger;
        private readonly ClusterOptions _clusterOptions;
        private readonly IMinioClient _minioClient;
        private readonly string _storageName;

        public MinioReminderTable(
            IMinioClient minioClient,
            ILogger<MinioReminderTable> logger,
            IOptions<ClusterOptions> clusterOptions)
        {
            _logger = logger;
            _clusterOptions = clusterOptions.Value;
            _minioClient = minioClient;
            _storageName = _clusterOptions.ClusterId.ToLower().Replace("_", "-") + "-reminders";
        }
        public async Task Init()
        {
            try
            {
                // Make a bucket on the server, if not already present.
                var beArgs = new BucketExistsArgs().WithBucket(_storageName);
                bool found = await _minioClient.BucketExistsAsync(beArgs).ConfigureAwait(false);
                if (!found)
                {
                    var mbArgs = new MakeBucketArgs().WithBucket(_storageName);
                    await _minioClient.MakeBucketAsync(mbArgs).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                var notfound = ex as ObjectNotFoundException;
                if (notfound == null)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }
            finally
            {
                await Task.CompletedTask;
            }

        }

        public async Task<ReminderEntry?> ReadRow(GrainId grainId, string reminderName)
        {
            ReminderEntry? reminder = null;
            try
            {
                var objectName = string.Join("-", grainId.ToString(), reminderName);
                await _minioClient.GetObjectAsync(
                    new GetObjectArgs().WithBucket(_storageName).WithObject(objectName).WithCallbackStream(async (stream) =>
                    {
                        var g = await stream.ToObject<ReminderEntry>();
                        if (g != null)
                        {
                            reminder = g;
                        }
                    })
                );
            }
            catch (Exception ex)
            {
                var notfound = ex as ObjectNotFoundException;
                if (notfound == null)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }
            return reminder;
        }

        public async Task<ReminderTableData> ReadRows(GrainId grainId)
        {
            try
            {
                var listReminders = new List<ReminderEntry>();
                var listArgs = new ListObjectsArgs().WithBucket(_storageName).WithPrefix(grainId.ToString()).WithRecursive(false);
                await foreach (var item in _minioClient.ListObjectsEnumAsync(listArgs).ConfigureAwait(false))
                {
                    await _minioClient.GetObjectAsync(
                        new GetObjectArgs().WithBucket(_storageName).WithObject(item.Key).WithCallbackStream(async (stream) =>
                        {
                            var g = await stream.ToObject<ReminderEntry>();
                            if (g != null)
                            {
                                listReminders.Add(g);
                            }
                        })
                    );
                }

                return new ReminderTableData(listReminders);
            }
            catch (Exception ex)
            {
                var notfound = ex as ObjectNotFoundException;
                if (notfound == null)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }
            return new ReminderTableData();
        }

        public async Task<ReminderTableData> ReadRows(uint begin, uint end)
        {
            try
            {
                var listReminders = new List<ReminderEntry>();
                var listArgs = new ListObjectsArgs().WithBucket(_storageName).WithRecursive(true);
                await foreach (var item in _minioClient.ListObjectsEnumAsync(listArgs).ConfigureAwait(false))
                {
                    try
                    {
                        var objectStatArgs = new StatObjectArgs().WithBucket(_storageName).WithObject(item.Key);
                        var statObject = await _minioClient.StatObjectAsync(objectStatArgs);
                        if (statObject != null && statObject.MetaData.Any())
                        {
                            var grainHash = statObject.MetaData.FirstOrDefault(t => t.Key.ToLower() == "grainHash").Value.ToInt();

                            if (begin < end)
                            {
                                if (grainHash > begin && grainHash <= end)
                                {
                                    await _minioClient.GetObjectAsync(
                                        new GetObjectArgs().WithBucket(_storageName).WithObject(item.Key).WithCallbackStream(async (stream) =>
                                        {
                                            var g = await stream.ToObject<ReminderEntry>();
                                            if (g != null)
                                            {
                                                listReminders.Add(g);
                                            }
                                        })
                                    );
                                }
                            }
                            else
                            {
                                if (grainHash > begin || grainHash <= end)
                                {
                                    await _minioClient.GetObjectAsync(
                                        new GetObjectArgs().WithBucket(_storageName).WithObject(item.Key).WithCallbackStream(async (stream) =>
                                        {
                                            var g = await stream.ToObject<ReminderEntry>();
                                            if (g != null)
                                            {
                                                listReminders.Add(g);
                                            }
                                        })
                                    );
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        var notfound = ex as ObjectNotFoundException;
                        if (notfound == null)
                        {
                            _logger.LogError(ex, ex.Message);
                        }
                    }
                }
                return new ReminderTableData(listReminders);
            }
            catch (Exception ex)
            {
                var notfound = ex as ObjectNotFoundException;
                if (notfound == null)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }
            return new ReminderTableData();
        }

        public async Task<bool> RemoveRow(GrainId grainId, string reminderName, string eTag)
        {
            try
            {
                var objectName = string.Join("-", grainId.ToString(), reminderName);
                var objectStatArgs = new StatObjectArgs().WithBucket(_storageName).WithObject(objectName);
                var statObject = await _minioClient.StatObjectAsync(objectStatArgs);
                if (statObject != null && statObject.MetaData.Any(p => p.Key.ToLower() == "etag" && p.Value == eTag))
                {
                    var args = new RemoveObjectArgs().WithBucket(_storageName).WithObject(objectName);
                    await _minioClient.RemoveObjectAsync(args).ConfigureAwait(false);
                    return true;
                }
            }
            catch (Exception ex)
            {
                var notfound = ex as ObjectNotFoundException;
                if (notfound == null)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }
            return false;
        }

        public async Task TestOnlyClearTable()
        {
            try
            {
                var listArgs = new ListObjectsArgs().WithBucket(_storageName).WithRecursive(false);
                await foreach (var item in _minioClient.ListObjectsEnumAsync(listArgs).ConfigureAwait(false))
                {
                    var args = new RemoveObjectArgs().WithBucket(_storageName).WithObject(item.Key);
                    await _minioClient.RemoveObjectAsync(args).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                var notfound = ex as ObjectNotFoundException;
                if (notfound == null)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }
        }

        public async Task<string> UpsertRow(ReminderEntry entry)
        {
            try
            {
                string etag = Guid.NewGuid().ToString();
                var grainHash = entry.GrainId.GetUniformHashCode();
                var reminderKey = string.Join("-", entry.GrainId.ToString(), entry.ReminderName);
                try
                {
                    var objectStatArgs = new StatObjectArgs()
                        .WithBucket(_storageName)
                        .WithObject(reminderKey);
                    var statObject = await _minioClient.StatObjectAsync(objectStatArgs);
                    if (statObject != null && statObject.MetaData.Any())
                    {
                        var etagMeta = statObject.MetaData.FirstOrDefault(t => t.Key.ToLower() == "etag");
                        if (etagMeta.Value != null && etagMeta.Value != entry.ETag)
                        {
                            throw new Exception("ETag mismatch.");
                        }
                        if (etagMeta.Value != null)
                        {

                            etag = etagMeta.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    var notfound = ex as ObjectNotFoundException;
                    if (notfound == null)
                    {
                        _logger.LogError(ex, ex.Message);
                    }
                }
                entry.ETag = etag;
                var data = entry.ToJson();
                var metaDics = new Dictionary<string, string>(){
                    { "Etag",string.IsNullOrEmpty( entry.ETag ) ? Guid.NewGuid().ToString() :  entry.ETag },
                    { "GrainHash",grainHash.ToString() }
                };
                var bs = Encoding.UTF8.GetBytes(data);
                var filestream = new MemoryStream(bs);
                var args = new PutObjectArgs()
                    .WithBucket(_storageName).WithObject(reminderKey)
                    .WithHeaders(metaDics)
                    .WithStreamData(filestream).WithObjectSize(filestream.Length)
                    .WithContentType("application/octet-stream");
                var rs = await _minioClient.PutObjectAsync(args).ConfigureAwait(false);
                return etag;
            }
            catch (Exception ex)
            {
                var notfound = ex as ObjectNotFoundException;
                if (notfound == null)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }
            return string.Empty;
        }
    }
}
