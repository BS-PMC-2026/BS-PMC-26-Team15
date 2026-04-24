using Microsoft.Extensions.DependencyInjection;

namespace SamiSpot.Services
{
    public class RedAlertBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public RedAlertBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var service = scope.ServiceProvider.GetRequiredService<RedAlertService>();

                    await service.FetchAndSaveAlertsAsync();
                }

                await Task.Delay(4000, stoppingToken); // every 5 sec
            }
        }
    }
}