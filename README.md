
# Orleans Minio Providers
[Orleans](https://github.com/dotnet/orleans) is a framework that provides a straight-forward approach to building distributed high-scale computing applications, without the need to learn and apply complex concurrency or other scaling patterns. 


## **Orleans.Minio** 
is a package that use Minio as a backend for Orleans providers like Cluster Membership, Grain State storage and Reminders. 

# Installation 
Nuget Packages are provided:
- Orleans.Persistence.Minio.Core
- Orleans.Bary.Persistence.Minio
- Orleans.Clustering.Minio
- Orleans.Reminders.Minio
  
## Silo
```
IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .UseOrleans(silo =>
    {
        silo.Services.AddMinio(configureClient => configureClient
                                .WithEndpoint("enpoint")
                                .WithCredentials("accesskey", "secretkey")
                                .WithSSL(false)
                                .Build());

        silo.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "DEV";
            options.ServiceId = "DEV";

        });
        silo.UseMinioClustering();
        silo.AddMinioGrainStorage("test", options =>{});
        silo.UseMinioReminder();
        silo.ConfigureLogging(logging => logging.AddConsole());

        silo.ConfigureEndpoints(
            siloPort: 11111,
            gatewayPort: 30001,
            advertisedIP: IPAddress.Parse("192.168.68.41"),
            listenOnAnyHostAddress: true
            );

        silo.Configure<ClusterMembershipOptions>(options =>
        {
            options.EnableIndirectProbes = true;
            options.UseLivenessGossip = true;
        });
    })
    .UseConsoleLifetime();

using IHost host = builder.Build();
await host.RunAsync();
```

## Client 
```
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseOrleansClient(client =>
{
    client.Services.AddMinio(configureClient => configureClient
                            .WithEndpoint("enpoint")
                            .WithCredentials("accesskey", "secretkey")
                            .WithSSL(false)
                            .Build());
    client.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "DEV";
        options.ServiceId = "DEV";

    });
    client.UseMinioClustering( );
});

```
## Grain example
```
public interface IHelloGrain : IGrainWithGuidKey
{
    Task<string> GetCount();

    Task AddItem(TestModel model);
    Task RegisterRemider();
}

public class HelloGrain : Grain, IHelloGrain, IRemindable
{
    private readonly ILogger _logger;

    private readonly IReminderRegistry _reminderRegistry;


    private IGrainReminder? _rTest;
    private bool _taskDone = false;

    private readonly IPersistentState<TestModel> _test;
    public HelloGrain(ILogger<HelloGrain> logger, IReminderRegistry reminderRegistry, [PersistentState("test", "test")] IPersistentState<TestModel> test)
    {
        _logger = logger;
        _test = test;
        _reminderRegistry = reminderRegistry;
    }

    public async Task<string> GetCount()
    {
        await _test.ReadStateAsync();
        return _test.State + "";
    }

    public async Task AddItem(TestModel model)
    {
        _test.State = model;
        await _test.WriteStateAsync();
    }
    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        try
        {
            if (reminderName == "TEST_REMIDER")
            {
                // Excute task
                if (_taskDone)
                {
                    if (_rTest == null)
                    {
                        _rTest = await _reminderRegistry.GetReminder(GrainContext.GrainId, "TEST_REMIDER");
                    }
                    if (_rTest != null)
                        await _reminderRegistry.UnregisterReminder(GrainContext.GrainId, _rTest);
                }
                _taskDone = true;
            }
        }
        catch (Exception ex)
        {
            //log
        }
    }
    public async Task RegisterRemider()
    {
        if (_rTest == null)
        {
            _rTest = await _reminderRegistry.GetReminder(GrainContext.GrainId, "TEST_REMIDER");
        }
        if (_rTest == null)
        {
            _rTest = await _reminderRegistry.RegisterOrUpdateReminder(
            callingGrainId: GrainContext.GrainId,
            reminderName: "TEST_REMIDER",
            dueTime: TimeSpan.Zero,
            period: TimeSpan.FromMinutes(1));
        }
    }

}

```
