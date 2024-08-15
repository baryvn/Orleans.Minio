
# Orleans Minio Providers
[Orleans](https://github.com/dotnet/orleans) is a framework that provides a straight-forward approach to building distributed high-scale computing applications, without the need to learn and apply complex concurrency or other scaling patterns. 


## **Orleans.Minio** 
is a package that use Minio as a backend for Orleans providers like Cluster Membership, Grain State storage and Reminders. 

# Installation 
Nuget Packages are provided:
- Orleans.Persistence.Minio
- Orleans.Clustering.Minio

## Coming soon
- Orleans.Reminder.Minio
  
## Silo
```
IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .UseOrleans(silo =>
    {
        silo.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "DEV";
            options.ServiceId = "DEV";

        });
        silo.UseMinioClustering(option =>
        {
          option.Endpoint = "s3.minio.example";
          option.AccessKey = "access key";
          option.SecretKey = "secret key";
        });
        silo.AddMinioGrainStorage("test", options =>
        {
          option.Endpoint = "s3.minio.example";
          option.AccessKey = "access key";
          option.SecretKey = "secret key";
        });
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
    client.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "DEV";
        options.ServiceId = "DEV";

    });
    client.UseMinioClustering(option =>
    {
        option.Endpoint = "s3.minio.example";
        option.AccessKey = "access key";
        option.SecretKey = "secret key";
    });
});

```
