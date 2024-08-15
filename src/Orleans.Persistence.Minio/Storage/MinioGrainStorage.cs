using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Orleans.Configuration;
using Orleans.Persistence.Minio.Providers;
using Orleans.Runtime;
using Orleans.Storage;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json;

namespace Orleans.Persistence.Minio.Storage;

public class MinioGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
{
    private readonly string _storageName;
    private readonly MinioGrainStorageOptions _options;
    private readonly ClusterOptions _clusterOptions;
    private readonly IMinioClient _minioClient;

    public MinioGrainStorage(string storageName, IMinioClient minioClient, MinioGrainStorageOptions options, IOptions<ClusterOptions> clusterOptions)
    {
        _options = options;
        _clusterOptions = clusterOptions.Value;
        _storageName = _clusterOptions.ServiceId.ToLower().Replace("_", "-") + "-" + storageName.ToLower().Replace("_", "-");
        _minioClient = minioClient;
    }

    private string GetKeyString(string stateName, GrainId grainId)
    {
        return $"{stateName}/{grainId}";
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
        var id = GetKeyString(stateName, grainId);

        var beArgs = new BucketExistsArgs().WithBucket(_storageName);
        bool found = await _minioClient.BucketExistsAsync(beArgs).ConfigureAwait(false);
        if (found)
        {
            try
            {
                var args = new RemoveObjectArgs().WithBucket(_storageName).WithObject(id);
                await _minioClient.RemoveObjectAsync(args).ConfigureAwait(false);
            }
            catch (Exception ex) { }
        }
        grainState.ETag = null;
        grainState.State = Activator.CreateInstance<T>()!;
        grainState.RecordExists = false;
    }

    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var id = GetKeyString(stateName, grainId);
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
                    }
                    else
                    {
                        grainState.State = Activator.CreateInstance<T>()!;
                    }
                })
            );
        }
        catch (Exception ex) { grainState.State = Activator.CreateInstance<T>()!; }
        grainState.RecordExists = true;
    }

    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var id = GetKeyString(stateName, grainId);
        try
        {
            await _minioClient.GetObjectAsync(
                new GetObjectArgs().WithBucket(_storageName).WithObject(id).WithCallbackStream(async (stream) =>
                {
                    var g = await stream.ToObject<GrainState<T>>();
                    if (g != null && g.ETag != grainState.ETag)
                    {
                        throw new InconsistentStateException("ETag mismatch.");
                    }
                })
           );
        }
        catch
        {

        }

        var bs = Encoding.UTF8.GetBytes(grainState.ToJson());
        var filestream = new MemoryStream(bs);
        var args = new PutObjectArgs()
            .WithBucket(_storageName).WithObject(id)
            .WithStreamData(filestream).WithObjectSize(filestream.Length)
            .WithContentType("application/octet-stream");
        _ = await _minioClient.PutObjectAsync(args).ConfigureAwait(false);
        grainState.RecordExists = true;
    }

}