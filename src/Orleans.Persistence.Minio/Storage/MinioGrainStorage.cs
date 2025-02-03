using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Orleans.Bary.Persistence.Minio.Providers;
using Orleans.Persistence.Minio.Core;
using Orleans.Runtime;
using Orleans.Storage;
using System.Reflection;
using System.Text;

namespace Orleans.Bary.Persistence.Minio.Storage;

public class MinioGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
{
    private readonly string _storageName;
    private readonly ILogger _logger;
    private readonly IMinioClient _minioClient;

    public MinioGrainStorage(string storageName, ILogger<MinioGrainStorage> logger, IMinioClient minioClient)
    {
        _logger = logger;
        _storageName = storageName.ToLower().Replace("_", "-");
        _minioClient = minioClient;
    }

    private string GetKeyString(GrainId grainId)
    {
        return $"{grainId}";
    }

    public List<PropertyInfo> GetMetaProps(Type type)
    {
        var list = new List<PropertyInfo>();
        PropertyInfo[] properties = type.GetProperties();

        foreach (PropertyInfo property in properties)
        {
            bool hasKeyAttribute = property.GetCustomAttributes(typeof(EsIndexAttribute), false).Any();

            if (hasKeyAttribute)
            {
                list.Add(property);
            }
        }
        return list;
    }
    public void Participate(ISiloLifecycle observer)
    {
        observer.Subscribe(
        observerName: OptionFormattingUtilities.Name<MinioGrainStorageOptions>(_storageName),
        stage: ServiceLifecycleStage.ApplicationServices,
        onStart: async (ct) =>
        {
            var beArgs = new BucketExistsArgs().WithBucket(_storageName);
            bool found = await _minioClient.BucketExistsAsync(beArgs).ConfigureAwait(false);
            if (!found)
            {
                var mbArgs = new MakeBucketArgs().WithBucket(_storageName);
                await _minioClient.MakeBucketAsync(mbArgs).ConfigureAwait(false);
            }
        });
    }

    public async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var id = GetKeyString(grainId);

        var beArgs = new BucketExistsArgs().WithBucket(_storageName);
        bool found = await _minioClient.BucketExistsAsync(beArgs).ConfigureAwait(false);
        if (found)
        {
            try
            {
                var args = new RemoveObjectArgs().WithBucket(_storageName).WithObject(id);
                await _minioClient.RemoveObjectAsync(args).ConfigureAwait(false);
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
        grainState.ETag = null;
        grainState.State = Activator.CreateInstance<T>()!;
        grainState.RecordExists = false;
    }

    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var id = GetKeyString(grainId);
        try
        {
            await _minioClient.GetObjectAsync(
                new GetObjectArgs().WithBucket(_storageName).WithObject(id).WithCallbackStream(async (stream) =>
                {
                    var g = await stream.ToObject<GrainState<T>>();
                    if (g != null)
                    {
                        grainState.State = g.State;
                        grainState.ETag = g.ETag;
                        grainState.RecordExists = true;
                    }
                    else
                    {
                        grainState.State = Activator.CreateInstance<T>()!;
                        grainState.RecordExists = false;
                    }
                })
            );
        }
        catch (Exception ex)
        {
            grainState.State = Activator.CreateInstance<T>()!;
            grainState.RecordExists = false;
            var notfound = ex as ObjectNotFoundException;
            if (notfound == null)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }

    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        try
        {
            var id = GetKeyString(grainId);
            var etag = Guid.NewGuid().ToString();
            try
            {
                var objectStatArgs = new StatObjectArgs()
                    .WithBucket(_storageName)
                    .WithObject(id);
                var statObject = await _minioClient.StatObjectAsync(objectStatArgs);
                if (statObject != null && statObject.MetaData.Any())
                {
                    var etagMeta = statObject.MetaData.FirstOrDefault(t => t.Key.ToLower() == "grainetag");
                    if (etagMeta.Value != null)
                    {
                        etag = etagMeta.Value;
                        if (etagMeta.Value != grainState.ETag) throw new InconsistentStateException("ETag mismatch.");
                    }
                }
            }
            catch (Exception ex)
            {
                grainState.RecordExists = false;
                var notfound = ex as ObjectNotFoundException;
                if (notfound == null)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }
            grainState.ETag = etag;
            var metaDics = new Dictionary<string, string>(){
                { "GrainETag",grainState.ETag }
            };
            var bs = Encoding.UTF8.GetBytes(grainState.ToJson());
            var metadatas = GetMetaProps(typeof(T));
            foreach (var metadata in metadatas)
            {
                var val = metadata.GetValue(grainState.State);
                metaDics.Add(metadata.Name, val != null ? val.ToJson() : "");
            }
            var filestream = new MemoryStream(bs);
            var args = new PutObjectArgs()
                .WithBucket(_storageName).WithObject(id)
                .WithHeaders(metaDics)
                .WithStreamData(filestream).WithObjectSize(filestream.Length)
                .WithContentType("application/octet-stream");
            var rs = await _minioClient.PutObjectAsync(args).ConfigureAwait(false);
            grainState.RecordExists = true;
        }
        catch (Exception ex)
        {
            grainState.RecordExists = false;
            var notfound = ex as ObjectNotFoundException;
            if (notfound == null)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}