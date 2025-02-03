using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using Orleans.Hosting;

namespace Orleans.Reminders.Minio
{
    public static class MinioHostingExtensions
    {

        public static ISiloBuilder UseMinioReminder(this ISiloBuilder builder)
        {
            return builder.ConfigureServices(services =>
            {
                services.AddReminders();
                services.AddSingleton<IReminderTable, MinioReminderTable>();
            });
        }

        public static IServiceCollection UseMinioReminder(this IServiceCollection services)
        {
            services.AddReminders();
            services.AddSingleton<IReminderTable, MinioReminderTable>();
            return services;
        }

    }
}
