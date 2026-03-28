using System.Text.Json;
using SamiSpot.Data;
using SamiSpot.Models;

namespace SamiSpot.Services
{
    public class RedAlertService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;

        public RedAlertService(HttpClient httpClient, ApplicationDbContext context)
        {
            _httpClient = httpClient;
            _context = context;
        }

        public async Task FetchAndSaveAlertsAsync()
        {
            string url = "https://api.tzevaadom.co.il/ios/feed";

            var json = await _httpClient.GetStringAsync(url);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var feed = JsonSerializer.Deserialize<RedAlertFeedResponse>(json, options);

            if (feed?.AlertsHistory == null)
                return;

            foreach (var group in feed.AlertsHistory)
            {
                if (group.Alerts == null)
                    continue;

                foreach (var alert in group.Alerts)
                {
                    if (alert.IsDrill)
                        continue;

                    if (alert.Cities == null || !alert.Cities.Any())
                        continue;

                    var alertTime = DateTimeOffset.FromUnixTimeSeconds(alert.Time).UtcDateTime;

                    foreach (var city in alert.Cities)
                    {
                        bool exists = _context.Alerts.Any(a =>
                            a.CityHebrew == city &&
                            a.AlertTimeUtc == alertTime &&
                            a.Threat == alert.Threat);

                        if (!exists)
                        {
                            _context.Alerts.Add(new Alert
                            {
                                CityHebrew = city,
                                AlertTimeUtc = alertTime,
                                Threat = alert.Threat,
                                IsDrill = alert.IsDrill
                            });
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }

    public class RedAlertFeedResponse
    {
        public List<RedAlertHistoryGroup>? AlertsHistory { get; set; }
    }

    public class RedAlertHistoryGroup
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public List<RedAlertItem>? Alerts { get; set; }
    }

    public class RedAlertItem
    {
        public long Time { get; set; }
        public List<string>? Cities { get; set; }
        public int Threat { get; set; }
        public bool IsDrill { get; set; }
    }
}