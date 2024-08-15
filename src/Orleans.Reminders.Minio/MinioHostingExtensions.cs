using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Hosting;

namespace Orleans.Reminders.Minio
{
    public static class MinioHostingExtensions
    {
    

        public static ISiloBuilder UseMinioReminder(this ISiloBuilder builder,
           Action<MinioReminderStorageOptions> configureOptions)
        {
            return builder.ConfigureServices(services => services.UseMinioReminder(configureOptions));
        }

        public static ISiloBuilder UseMinioReminder(this ISiloBuilder builder)
        {
            return builder.ConfigureServices(services =>
            {
                services.AddOptions<MinioReminderStorageOptions>();
                services.AddSingleton<IReminderTable, MinioReminderTable>();
            });
        }

        public static IServiceCollection UseMinioReminder(this IServiceCollection services,
            Action<MinioReminderStorageOptions> configureOptions)
        {
            return services.UseMinioReminder(ob => ob.Configure(configureOptions));
        }

        public static IServiceCollection UseMinioReminder(this IServiceCollection services,
            Action<OptionsBuilder<MinioReminderStorageOptions>> configureOptions)
        {
            configureOptions?.Invoke(services.AddOptions<MinioReminderStorageOptions>());
            return services.AddSingleton<IReminderTable, MinioReminderTable>();
        }

    }
}
