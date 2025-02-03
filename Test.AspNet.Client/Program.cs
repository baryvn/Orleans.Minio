using Orleans.Configuration;
using Minio;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseOrleansClient(client =>
{
    client.Services.AddMinio(configureClient => configureClient
                            .WithEndpoint("s3.minio.ifilemanager.intemi.vn")
                            .WithCredentials("V77bP7IJ48EQqAvBdeEW", "FUyioEZ5ZjHZjYq6YnGVfLNWhlhyZag9sSPJdBaS")
                            .WithSSL(false)
                            .Build());
    client.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "ORLEANS_TEST";
        options.ServiceId = "ORLEANS_TEST";
    });
    client.UseMinioClustering();
});

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();


await app.RunAsync("http://192.168.68.41:11001");
