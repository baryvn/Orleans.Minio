using Orleans.Configuration;
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
        option.Endpoint = "s3.minio.ecoit.vn";
        option.AccessKey = "nnaaSlzudLuXWVsnNkif";
        option.SecretKey = "tmfGzH5wz3ATfYJuxdfrFh8M9tOWNmiuBekPwKBk";
    });
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
