using OpenAI.Chat;
using SamiSpot.Data;
using SamiSpot.Models;
using Microsoft.EntityFrameworkCore;
using OpenAIChatMessage = OpenAI.Chat.ChatMessage;

namespace SamiSpot.Services
{
    public class OpenAiService
    {
        private readonly ApplicationDbContext _context;
        private readonly ChatClient _client;

        public OpenAiService(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _client = new ChatClient(
                model: "gpt-4o-mini",
                apiKey: config["OpenAI:ApiKey"]
            );
        }

        public async Task<string> GetReply(string question, double? lat, double? lng, List<ConversationTurn>? history = null)
        {
            var ctx = await BuildContext(question, lat, lng);
            return await CallGpt(question, ctx, history ?? []);
        }

        private async Task<SafetyContext> BuildContext(string question, double? lat, double? lng)
        {
            var ctx = new SafetyContext();
            var nowUtc = DateTime.UtcNow;

            ctx.MentionedCity = DetectMentionedCity(question);

            double? searchLat = lat, searchLng = lng;

            if (lat.HasValue && lng.HasValue)
            {
                ctx.HasUserLocation = true;
                ctx.NearestCityToUser = FindNearestCity(lat.Value, lng.Value);
            }

            if (!string.IsNullOrEmpty(ctx.MentionedCity))
            {
                var coords = GetCityCoordinates(ctx.MentionedCity);
                if (coords.HasValue)
                {
                    searchLat = coords.Value.lat;
                    searchLng = coords.Value.lng;
                    ctx.SearchCity = ctx.MentionedCity;
                }
            }
            else
            {
                ctx.SearchCity = ctx.NearestCityToUser;
            }

            if (WantsShelters(question) && searchLat.HasValue && searchLng.HasValue)
                ctx.NearestShelters = FindNearestShelters(searchLat.Value, searchLng.Value, 3);

            var alertCity = ctx.MentionedCity ?? ctx.NearestCityToUser;

            if (WantsSafetyStatus(question) && !string.IsNullOrEmpty(alertCity))
                ctx.AlertStatus = GetAlertStatus(alertCity, nowUtc);

            return ctx;
        }

        private AlertStatus GetAlertStatus(string cityName, DateTime nowUtc)
        {
            var canonical = CanonicalHebrewName(cityName);
            var last24 = nowUtc.AddHours(-24);
            var last15 = nowUtc.AddMinutes(-15);

            var alerts = _context.Alerts
                .AsNoTracking()
                .Where(a => a.AlertTimeUtc >= last24)
                .Select(a => new { a.CityHebrew, a.AlertTimeUtc })
                .ToList()
                .Where(a => CanonicalHebrewName(a.CityHebrew) == canonical)
                .ToList();

            return new AlertStatus
            {
                HasLast15Min = alerts.Any(a => a.AlertTimeUtc >= last15),
                HasLast24Hr = alerts.Count > 0,
                Count24Hr = alerts.Count
            };
        }

        private async Task<string> CallGpt(string question, SafetyContext ctx, List<ConversationTurn> history)
        {
            var messages = new List<OpenAIChatMessage>
            {
                OpenAIChatMessage.CreateSystemMessage(BuildSystemPrompt(ctx))
            };

            foreach (var turn in history.TakeLast(3))
            {
                messages.Add(OpenAIChatMessage.CreateUserMessage(turn.UserMessage));
                messages.Add(OpenAIChatMessage.CreateAssistantMessage(turn.AssistantMessage));
            }

            messages.Add(OpenAIChatMessage.CreateUserMessage(question));

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 220,
                Temperature = 0.2f
            };

            var response = await _client.CompleteChatAsync(messages, options);

            return response.Value.Content[0].Text?.Trim()
                ?? "⚠️ Something went wrong. Please try again.";
        }

        private string BuildSystemPrompt(SafetyContext ctx)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("""
You are an AI Safety Assistant inside an emergency shelter app in Israel.
Only answer about greetings, user location, alerts, shelters, map links, and emergency safety instructions.
Be short, calm, clear, and friendly.
If unrelated, say: "I can only help with safety, location, shelters, and alerts."

Rules:
- City question = answer about that city only.
- Current location question = use provided coordinates.
- Alerts last 15 min = 🚨 Active alerts.
- Alerts last 24h = ⚠️ Recent alerts.
- No alerts = ✅ No alerts.
- If shelters are listed, use provided SHELTER DATA only.
- Use light emojis only.
""");

            sb.AppendLine("## LOCATION DATA");
            if (ctx.HasUserLocation)
                sb.AppendLine($"User current location: {ctx.NearestCityToUser}");
            else
                sb.AppendLine("User location not shared.");

            if (!string.IsNullOrEmpty(ctx.MentionedCity))
                sb.AppendLine($"Mentioned city: {ctx.MentionedCity}");

            sb.AppendLine("## ALERT DATA");
            var alertCity = ctx.MentionedCity ?? ctx.NearestCityToUser;

            if (ctx.AlertStatus != null && !string.IsNullOrEmpty(alertCity))
            {
                if (ctx.AlertStatus.HasLast15Min)
                    sb.AppendLine($"🚨 Active alerts in {alertCity}. Count last 24h: {ctx.AlertStatus.Count24Hr}");
                else if (ctx.AlertStatus.HasLast24Hr)
                    sb.AppendLine($"⚠️ Recent alerts in {alertCity}. Count last 24h: {ctx.AlertStatus.Count24Hr}");
                else
                    sb.AppendLine($"✅ No alerts in {alertCity} in last 24h.");
            }
            else
            {
                sb.AppendLine("No alert data loaded.");
            }

            sb.AppendLine("## SHELTER DATA");
            if (ctx.NearestShelters.Count > 0)
            {
                int i = 1;
                foreach (var s in ctx.NearestShelters)
                {
                    var mapsUrl = $"https://www.google.com/maps/search/?api=1&query={s.Shelter.Latitude},{s.Shelter.Longitude}";
                    sb.AppendLine($"{i}. {s.Shelter.Name}, {Math.Round(s.Distance)}m, {s.Shelter.Address}, <a href=\"{mapsUrl}\" target=\"_blank\">Open in Maps</a>");
                    i++;
                }
            }
            else
            {
                sb.AppendLine("No shelter data loaded.");
            }

            sb.AppendLine("Map link: <a href=\"/Map/Index\" target=\"_blank\">Open Shelter Map</a>");

            return sb.ToString();
        }

        private string? DetectMentionedCity(string question)
        {
            var qLower = question.ToLower().Trim();

            foreach (var kv in EnglishAliases)
                if (qLower.Contains(kv.Key))
                    return kv.Value;

            var allCities = _context.CityLocations
                .AsNoTracking()
                .Where(c => !string.IsNullOrWhiteSpace(c.HebrewName))
                .Select(c => c.HebrewName)
                .ToList();

            foreach (var city in allCities)
                if (question.Contains(city))
                    return city;

            return null;
        }

        private (double lat, double lng)? GetCityCoordinates(string cityName)
        {
            var canonical = CanonicalHebrewName(cityName);

            var city = _context.CityLocations
                .AsNoTracking()
                .Where(c => c.Latitude != 0 && c.Longitude != 0)
                .Select(c => new { c.HebrewName, c.Latitude, c.Longitude })
                .ToList()
                .FirstOrDefault(c => CanonicalHebrewName(c.HebrewName) == canonical);

            if (city != null)
                return (city.Latitude, city.Longitude);

            if (CoordinateOverrides.TryGetValue(canonical, out var coords))
                return (coords.Latitude, coords.Longitude);

            return null;
        }

        private string FindNearestCity(double lat, double lng)
        {
            return _context.CityLocations
                .AsNoTracking()
                .Where(c => c.Latitude != 0 && c.Longitude != 0)
                .Select(c => new { c.HebrewName, c.Latitude, c.Longitude })
                .ToList()
                .Select(c => new { c.HebrewName, Dist = GetDistanceMeters(lat, lng, c.Latitude, c.Longitude) })
                .OrderBy(x => x.Dist)
                .FirstOrDefault()?.HebrewName ?? "your area";
        }

        private List<ShelterDistanceResult> FindNearestShelters(double lat, double lng, int count)
        {
            var shelters = _context.Shelters
                .AsNoTracking()
                .Where(s => s.IsActive && s.Latitude != 0 && s.Longitude != 0)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Address,
                    s.Latitude,
                    s.Longitude,
                    s.IsActive
                })
                .ToList();

            return shelters
                .Select(s => new ShelterDistanceResult
                {
                    Shelter = new Shelter
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Address = s.Address,
                        Latitude = s.Latitude,
                        Longitude = s.Longitude,
                        IsActive = s.IsActive
                    },
                    Distance = GetDistanceMeters(lat, lng, s.Latitude, s.Longitude)
                })
                .OrderBy(x => x.Distance)
                .Take(count)
                .ToList();
        }

        private double GetDistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000;
            double dLat = DegreesToRadians(lat2 - lat1);
            double dLon = DegreesToRadians(lon2 - lon1);
            double a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private double DegreesToRadians(double deg) => deg * Math.PI / 180.0;

        private bool WantsShelters(string q)
        {
            q = q.ToLower();
            return q.Contains("shelter") || q.Contains("nearest") || q.Contains("closest") ||
                   q.Contains("near me") || q.Contains("מקלט") || q.Contains("miklat");
        }

        private bool WantsSafetyStatus(string q)
        {
            q = q.ToLower();
            return q.Contains("safe") || q.Contains("alert") || q.Contains("danger") ||
                   q.Contains("rocket") || q.Contains("אזעקה") || q.Contains("בטוח") ||
                   q.Contains("מסוכן");
        }

        private static string NormalizeHebrewName(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            text = text.Trim().ToLower()
                .Replace("\"", "").Replace("״", "").Replace("׳", "")
                .Replace("'", "").Replace("-", " ").Replace("־", " ").Replace(".", " ");
            if (text.Contains(","))
                text = string.Join(" ", text.Split(',').Select(x => x.Trim()));
            while (text.Contains("  ")) text = text.Replace("  ", " ");
            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                var last = parts[^1];
                if (last is "דרום" or "צפון" or "מזרח" or "מערב")
                    text = string.Join(" ", parts.Take(parts.Length - 1));
            }
            return text.Trim();
        }

        private static string CanonicalHebrewName(string text)
        {
            var name = NormalizeHebrewName(text);
            if (string.IsNullOrEmpty(name)) return "";

            if (name.StartsWith("באר שבע")) return "באר שבע";
            if (name.StartsWith("אשדוד")) return "אשדוד";
            if (name.StartsWith("אשקלון")) return "אשקלון";
            if (name.StartsWith("ירושלים")) return "ירושלים";
            if (name.StartsWith("הרצליה")) return "הרצליה";
            if (name.StartsWith("חיפה")) return "חיפה";
            if (name.StartsWith("נתניה")) return "נתניה";
            if (name.StartsWith("ראשון לציון")) return "ראשון לציון";
            if (name.StartsWith("רמת גן")) return "רמת גן";
            if (name.StartsWith("תל אביב")) return "תל אביב";
            if (name.StartsWith("צפת")) return "צפת";
            if (name.StartsWith("עכו")) return "עכו";

            var aliases = new Dictionary<string, string>
            {
                { "תל אביב יפו", "תל אביב -יפו" },
                { "יהוד מונוסון", "יהוד" },
                { "מעלות תרשיחא", "מעלות-תרשיחא" },
                { "מודיעין מכבים רעות", "מודיעין-מכבים-רעות" },
                { "קרית ארבע", "קריית ארבע" },
                { "כסיפה", "כסייפה" },
                { "שומרייה", "שומריה" },
                { "גש גוש חלב", "ג'ש (גוש חלב)" },
                { "שדרות איבים", "שדרות" },
                { "אשדוד דרום", "אשדוד" }, { "אשדוד צפון", "אשדוד" },
                { "אשקלון דרום", "אשקלון" }, { "אשקלון צפון", "אשקלון" },
                { "הרצליה מערב", "הרצליה" }, { "הרצליה מרכז וגליל ים", "הרצליה" },
                { "נתניה מזרח", "נתניה" }, { "נתניה מערב", "נתניה" },
                { "ראשון לציון מזרח", "ראשון לציון" }, { "ראשון לציון מערב", "ראשון לציון" },
                { "רמת גן מזרח", "רמת גן" }, { "רמת גן מערב", "רמת גן" },
            };

            return aliases.TryGetValue(name, out var canonical) ? canonical : name;
        }

        private static readonly Dictionary<string, string> EnglishAliases = new()
        {
            { "lehavim", "להבים" }, { "lehvaim", "להבים" },
            { "beer sheva", "באר שבע" }, { "beersheba", "באר שבע" }, { "beersheva", "באר שבע" },
            { "tel aviv", "תל אביב -יפו" }, { "telaviv", "תל אביב -יפו" },
            { "jerusalem", "ירושלים" }, { "yerushalayim", "ירושלים" },
            { "haifa", "חיפה" }, { "eilat", "אילת" },
            { "netanya", "נתניה" }, { "ashdod", "אשדוד" },
            { "ashkelon", "אשקלון" }, { "rishon lezion", "ראשון לציון" },
            { "petah tikva", "פתח תקווה" }, { "bnei brak", "בני ברק" },
            { "ramat gan", "רמת גן" }, { "bat yam", "בת ים" },
            { "holon", "חולון" }, { "rehovot", "רחובות" },
            { "herzliya", "הרצליה" }, { "kfar saba", "כפר סבא" },
            { "sderot", "שדרות" }, { "ofakim", "אופקים" },
            { "dimona", "דימונה" }, { "arad", "ערד" },
            { "nazareth", "נצרת" }, { "safed", "צפת" },
            { "tiberias", "טבריה" }, { "acre", "עכו" },
            { "nahariya", "נהריה" }, { "kiryat shmona", "קריית שמונה" },
            { "modiin", "מודיעין-מכבים-רעות" }, { "rishon", "ראשון לציון" },
            { "sami shamoon", "באר שבע" },
            { "bgu", "באר שבע" },
        };

        private static readonly Dictionary<string, (double Latitude, double Longitude)> CoordinateOverrides = new()
        {
            { "שגב שלום", (31.1976, 34.8390) },
            { "ערערה בנגב", (31.1553, 35.0222) },
            { "כסייפה", (31.2447, 35.0800) },
            { "לקיה", (31.3238, 34.8647) },
            { "רהט", (31.3927, 34.7559) },
            { "חורה", (31.2985, 34.9346) },
            { "תל שבע", (31.2480, 34.8601) },
            { "אילת", (29.5577, 34.9519) },
        };

        private class SafetyContext
        {
            public bool HasUserLocation { get; set; }
            public string? NearestCityToUser { get; set; }
            public string? MentionedCity { get; set; }
            public string? SearchCity { get; set; }
            public List<ShelterDistanceResult> NearestShelters { get; set; } = [];
            public AlertStatus? AlertStatus { get; set; }
        }

        private class AlertStatus
        {
            public bool HasLast15Min { get; set; }
            public bool HasLast24Hr { get; set; }
            public int Count24Hr { get; set; }
        }

        private class ShelterDistanceResult
        {
            public Shelter Shelter { get; set; } = null!;
            public double Distance { get; set; }
        }
    }

    public class ConversationTurn
    {
        public string UserMessage { get; set; } = "";
        public string AssistantMessage { get; set; } = "";
    }
}