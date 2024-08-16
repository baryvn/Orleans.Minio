using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Reminders.Minio;
using System.Net;
using Orleans.Bary.Persistence.Minio.Hosting;

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
            option.Endpoint = "s3.minio.ecoit.vn";
            option.AccessKey = "nnaaSlzudLuXWVsnNkif";
            option.SecretKey = "tmfGzH5wz3ATfYJuxdfrFh8M9tOWNmiuBekPwKBk";
        });
        silo.AddMinioGrainStorage("test", options =>
        {
            options.Endpoint = "s3.minio.ecoit.vn";
            options.AccessKey = "nnaaSlzudLuXWVsnNkif";
            options.SecretKey = "tmfGzH5wz3ATfYJuxdfrFh8M9tOWNmiuBekPwKBk";
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
