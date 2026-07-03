using Hangfire;

namespace personal_attendanse_system.Services
{
    public class HangfireInitializer : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public HangfireInitializer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
                var service = scope.ServiceProvider.GetRequiredService<ScheduledLinkService>();

                // Schedule the cleanup job to run once a day at 2:00 AM
                recurringJobManager.AddOrUpdate(
                    "cleanup-orphaned-jobs",
                    () => service.CleanupOrphanedJobs(),
                    Cron.Daily(2, 0)); // 2 AM in UTC time
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
