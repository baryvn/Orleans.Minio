using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using System.Net;
using Orleans.Bary.Persistence.Minio.Hosting;
using Minio;
using Orleans.Reminders.Minio;

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .UseOrleans(static silo =>
    {
        silo.Services.AddMinio(configureClient => configureClient
                                .WithEndpoint("s3.minio.ifilemanager.intemi.vn")
                                .WithCredentials("V77bP7IJ48EQqAvBdeEW", "FUyioEZ5ZjHZjYq6YnGVfLNWhlhyZag9sSPJdBaS")
                                .WithSSL(false)
                                .Build());

        silo.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "ORLEANS_TEST";
            options.ServiceId = "ORLEANS_TEST";

        });
        silo.UseMinioClustering();
        silo.AddMinioGrainStorage("test", option =>
        {
        });

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
        silo.UseDashboard(x =>
        {
            x.HostSelf = true;
            x.Port = 9992;
        });
    })
    .UseConsoleLifetime();

using IHost host = builder.Build();
await host.RunAsync();
