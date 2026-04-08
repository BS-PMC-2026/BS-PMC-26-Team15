using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SamiSpot.Data;
using SamiSpot.Models;
using SamiSpot.Services;
using System.Text;
using System.Text.Json;

namespace SamiSpot.Controllers
{
    public class MapController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string GovMapUrl = "https://www.govmap.gov.il/api/layers-catalog/entitiesByPoint";
        private static readonly List<string> GovMapShelterLayerIds = new()
{
    "227335",
    "218406",
    "226583",
    "217781",
    "226487",
    "215420",
    "226377",
    "226639",
    "226353",
    "218045",
    "226275",
    "225479",
    "226585",
    "215448",
    "220929",
    "215628",
    "215022",
    "212872",
    "217352",
    "210766",
    "220346",
    "217208",
    "212140",
    "218373",
    "218221",
    "226453",
    "417"
};

        public MapController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var shelters = _context.Shelters
                .AsNoTracking()
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Address,
                    s.City,
                    s.Latitude,
                    s.Longitude,
                    s.ShelterType,
                    s.Capacity,
                    s.IsAccessible,
                    s.IsPublic,
                    s.Source
                })
                .ToList();

            return View(shelters);
        }

        private List<double[]> GenerateIsraelGridPoints()
        {
            var points = new List<double[]>();

            double minX = 3820000;
            double maxX = 3950000;
            double minY = 3450000;
            double maxY = 3765000;

            double step = 8000; // 🔥 much better (8km)

            for (double x = minX; x <= maxX; x += step)
            {
                for (double y = minY; y <= maxY; y += step)
                {
                    points.Add(new[] { x, y });
                }
            }

            return points;
        }

        [HttpGet("api/shelters")]
        public async Task<IActionResult> GetShelters()
        {
            var shelters = await _context.Shelters
                .Where(s => s.IsActive)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Address,
                    s.City,
                    s.Latitude,
                    s.Longitude,
                    s.Source
                })
                .ToListAsync();

            return Ok(shelters);
        }
        [HttpGet]
        public async Task<IActionResult> ScanGovMapSample()
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(20);

            var samplePoints = GenerateIsraelGridPoints();

            int added = 0;
            int updated = 0;
            int skipped = 0;
            int noCentroid = 0;
            int failedRequests = 0;
            int processedPoints = 0;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            foreach (var point in samplePoints)
            {
                try
                {
                    var requestBody = new GovMapRequest
                    {
                        Point = point,
                        Layers = GovMapShelterLayerIds
                            .Select(id => new GovMapLayerRequest { LayerId = id })
                            .ToList(),
                        Tolerance = 8000
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync(GovMapUrl, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        failedRequests++;
                        continue;
                    }

                    var responseText = await response.Content.ReadAsStringAsync();
                    var parsed = JsonSerializer.Deserialize<GovMapLayerResponse>(responseText, options);

                    if (parsed?.Data == null || parsed.Data.Count == 0)
                    {
                        skipped++;
                        continue;
                    }

                    foreach (var layer in parsed.Data)
                    {
                        if (layer.Entities == null || layer.Entities.Count == 0)
                            continue;

                        foreach (var entity in layer.Entities)
                        {
                            var miklatId = entity.Fields
                                .FirstOrDefault(f => f.FieldName == "מזהה מקלט")?.FieldValue?.ToString();

                            var address = entity.Fields
                                .FirstOrDefault(f => f.FieldName == "כתובת")?.FieldValue?.ToString();

                            var area = entity.Fields
                                .FirstOrDefault(f => f.FieldName == "אזור")?.FieldValue?.ToString();

                            var miklatNum = entity.Fields
                                .FirstOrDefault(f => f.FieldName == "מספר מקלט")?.FieldValue?.ToString();

                            var shelterType = GetFieldValue(entity.Fields, "סוג מקלט");
                            var locality = GetFieldValue(entity.Fields, "יישוב");
                            var name = GetFieldValue(entity.Fields, "שם");

                            double x, y;
                            if (!TryReadCentroid(entity.Centroid, out x, out y))
                            {
                                noCentroid++;
                                continue;
                            }

                            var (lat, lng) = CoordinateHelper.WebMercatorToLatLng(x, y);

                            Shelter? existing = null;

                            if (!string.IsNullOrWhiteSpace(miklatId))
                            {
                                existing = await _context.Shelters.FirstOrDefaultAsync(s =>
                                    s.Source == "GovMap" &&
                                    s.GovMapMiklatId == miklatId);
                            }
                            else
                            {
                                // avoid exact double comparison
                                existing = await _context.Shelters.FirstOrDefaultAsync(s =>
                                    s.Source == "GovMap" &&
                                    Math.Abs(s.Latitude - lat) < 0.0001 &&
                                    Math.Abs(s.Longitude - lng) < 0.0001);
                            }

                            if (existing == null)
                            {
                                existing = new Shelter
                                {
                                    Source = "GovMap",
                                    GovMapMiklatId = miklatId
                                };

                                _context.Shelters.Add(existing);
                                added++;
                            }
                            else
                            {
                                updated++;
                            }

                            existing.Name =
                                !string.IsNullOrWhiteSpace(name) ? name :
                                !string.IsNullOrWhiteSpace(miklatNum) ? $"מקלט {miklatNum}" :
                                !string.IsNullOrWhiteSpace(shelterType) ? $"מקלט מסוג {shelterType}" :
                                "GovMap Shelter";

                            existing.Address = address;
                            existing.City = !string.IsNullOrWhiteSpace(locality) ? locality : area;
                            existing.Latitude = lat;
                            existing.Longitude = lng;
                            existing.ShelterType = !string.IsNullOrWhiteSpace(shelterType) ? shelterType : "Public Shelter";
                            existing.IsAccessible = false;
                            existing.IsPublic = true;
                            existing.IsActive = true;
                            existing.Source = "GovMap";
                            existing.SourceUrl = GovMapUrl;
                            existing.LastSyncedAt = DateTime.UtcNow;
                        }
                    }

                    processedPoints++;

                    // save every 100 points
                    if (processedPoints % 100 == 0)
                    {
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    skipped++;
                    Console.WriteLine($"Error at point [{point[0]}, {point[1]}]: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                message = "GovMap Israel scan finished",
                added,
                updated,
                skipped,
                failedRequests,
                noCentroid,
                totalPoints = samplePoints.Count
            });
        }
        [HttpGet]
        public IActionResult GetCityRisks()
        {
            var now = DateTime.UtcNow;
            var last15Minutes = now.AddMinutes(-15);
            var last24Hours = now.AddHours(-24);

            var alerts = _context.Alerts
                .Where(a => a.AlertTimeUtc >= last24Hours)
                .ToList();

            var cities = _context.CityLocations.ToList();

            var normalizedAlerts = alerts
                .Select(a => new
                {
                    OriginalName = a.CityHebrew,
                    CanonicalName = CanonicalHebrewName(a.CityHebrew),
                    a.AlertTimeUtc
                })
                .ToList();

            var alertGroups = normalizedAlerts
                .GroupBy(a => a.CanonicalName)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new Dictionary<string, CityRiskDto>();

            foreach (var city in cities)
            {
                var canonicalCity = CanonicalHebrewName(city.HebrewName);

                bool hasLast15 = false;
                bool hasLast24 = false;

                if (alertGroups.TryGetValue(canonicalCity, out var cityAlerts))
                {
                    hasLast15 = cityAlerts.Any(a => a.AlertTimeUtc >= last15Minutes);
                    hasLast24 = true;
                }

                result[canonicalCity] = new CityRiskDto
                {
                    CityName = city.HebrewName,
                    Latitude = city.Latitude,
                    Longitude = city.Longitude,
                    Color = hasLast15 ? "red" : hasLast24 ? "orange" : "green"
                };
            }

            foreach (var kvp in alertGroups)
            {
                var canonicalAlert = kvp.Key;

                if (result.ContainsKey(canonicalAlert))
                    continue;

                if (!CoordinateOverrides.TryGetValue(canonicalAlert, out var coords))
                    continue;

                bool hasLast15 = kvp.Value.Any(a => a.AlertTimeUtc >= last15Minutes);
                bool hasLast24 = true;

                result[canonicalAlert] = new CityRiskDto
                {
                    CityName = canonicalAlert,
                    Latitude = coords.Latitude,
                    Longitude = coords.Longitude,
                    Color = hasLast15 ? "red" : hasLast24 ? "orange" : "green"
                };
            }

            return Json(result.Values.ToList());
        }

        private static bool NamesMatch(string alertName, string cityName)
        {
            if (string.IsNullOrWhiteSpace(alertName) || string.IsNullOrWhiteSpace(cityName))
                return false;

            var a = CanonicalHebrewName(alertName);
            var c = CanonicalHebrewName(cityName);

            return a == c || a.Contains(c) || c.Contains(a);
        }

        private static string NormalizeHebrewName(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            text = text.Trim().ToLower();

            text = text.Replace("\"", "")
                       .Replace("״", "")
                       .Replace("׳", "")
                       .Replace("'", "")
                       .Replace("-", " ")
                       .Replace("־", " ")
                       .Replace(".", " ");

            if (text.Contains(","))
                text = string.Join(" ", text.Split(',').Select(x => x.Trim()));

            while (text.Contains("  "))
                text = text.Replace("  ", " ");

            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 1)
            {
                var last = parts[^1];
                if (last is "דרום" or "צפון" or "מזרח" or "מערב")
                    text = string.Join(" ", parts.Take(parts.Length - 1));
            }

            text = text.Replace("כסיפה", "כסייפה")
                       .Replace("שומרייה", "שומריה")
                       .Replace("דביירה", "דבירה")
                       .Replace("הרצלייה", "הרצליה")
                       .Replace("נהרייה", "נהריה")
                       .Replace("נורדייה", "נורדיה")
                       .Replace("שגב שלום", "שגב שלום")
                       .Replace("שגב שלום", "שגב שלום")
                       .Replace("ערערה בנגב", "ערערה בנגב")
                       .Replace("ערערה בנגב", "ערערה בנגב");

            return text.Trim();
        }

        private static string CanonicalHebrewName(string text)
        {
            var name = NormalizeHebrewName(text);

            if (StartsWith(name, "באר שבע")) return "באר שבע";
            if (StartsWith(name, "אשדוד")) return "אשדוד";
            if (StartsWith(name, "אשקלון")) return "אשקלון";
            if (StartsWith(name, "ירושלים")) return "ירושלים";
            if (StartsWith(name, "הרצליה")) return "הרצליה";
            if (StartsWith(name, "חיפה")) return "חיפה";
            if (StartsWith(name, "נתניה")) return "נתניה";
            if (StartsWith(name, "ראשון לציון")) return "ראשון לציון";
            if (StartsWith(name, "רמת גן")) return "רמת גן";
            if (StartsWith(name, "תל אביב")) return "תל אביב";
            if (StartsWith(name, "צפת")) return "צפת";
            if (StartsWith(name, "עכו")) return "עכו";

            var aliases = new Dictionary<string, string>
    {
        { "חבר", "מעלה חבר" },
        { "פני חבר", "מעלה חבר" },

        { "גבים מכללת ספיר", "גבים" },
        { "זמרת שובה", "זמרת" },
        { "צוחר אוהד", "צוחר" },
        { "יעבץ יעף", "יעף" },
        { "מבטחים עמיעוז ישע", "מבטחים" },
        { "מעגלים גבעולים מלילות", "מעגלים" },

        { "בני עיש", "בני עי\"ש" },
        { "גבעת חן", "גבעת ח\"ן" },
        { "גבעת כח", "גבעת כ\"ח" },
        { "דובב", "דוב\"ב" },
        { "יד רמבהם", "יד רמב\"ם" },
        { "ייטב", "ייט\"ב" },
        { "כפר בילו", "כפר ביל\"ו" },
        { "כפר הריף", "כפר הרי\"ף" },
        { "כפר הריף וצומת ראם", "כפר הרי\"ף" },
        { "כפר חבד", "כפר חב\"ד" },
        { "כפר מלל", "כפר מל\"ל" },
        { "נווה אטיב", "נווה אטי\"ב" },
        { "פעמי תשז", "פעמי תש\"ז" },
        { "תלמי בילו", "תלמי ביל\"ו" },

        { "יהוד מונוסון", "יהוד" },
        { "כוכב יאיר צור יגאל", "כוכב יאיר" },
        { "נוף איילון שעלבים", "נוף איילון" },
        { "עלי זהב לשם", "עלי זהב" },
        { "קדימה צורן", "צורן" },
        { "מעלות תרשיחא", "מעלות-תרשיחא" },
        { "מודיעין מכבים רעות", "מודיעין-מכבים-רעות" },
        { "קרית ארבע", "קריית ארבע" },
        { "לוחמי הגטאות", "לוחמי הגיטאות" },
        { "בוקעתא", "בוקעאתא" },
        { "גדידה מכר", "ג'דיידה-מכר" },
        { "גוליס", "ג'ולס" },
        { "דלית אל כרמל", "דלייה" },
        { "עספיא", "עיר כרמל" },
        { "כסיפה", "כסייפה" },
        { "שומרייה", "שומריה" },
        { "דביירה", "דבירה" },
        { "גש גוש חלב", "ג'ש (גוש חלב)" },
        { "ינוח גת", "יאנוח-ג'ת" },
        { "תל אביב יפו", "תל אביב -יפו" },
        { "שדרות איבים", "שדרות" },
        { "אשדוד איזור תעשייה צפוני", "אשדוד" },
        { "אשדוד א ב ד ה", "אשדוד" },
        { "אשדוד ג ו ז", "אשדוד" },
        { "אשדוד ח ט י יג יד טז", "אשדוד" },
        { "אשדוד יא יב טו יז מרינה סיט", "אשדוד" },
        { "אשקלון דרום", "אשקלון" },
        { "אשקלון צפון", "אשקלון" },
        { "הרצליה מערב", "הרצליה" },
        { "הרצליה מרכז וגליל ים", "הרצליה" },
        { "נתניה מזרח", "נתניה" },
        { "נתניה מערב", "נתניה" },
        { "ראשון לציון מזרח", "ראשון לציון" },
        { "ראשון לציון מערב", "ראשון לציון" },
        { "רמת גן מזרח", "רמת גן" },
        { "רמת גן מערב", "רמת גן" },
        { "צפת עיר", "צפת" },
        { "צפת עכברה", "צפת" },
        { "עכו אזור תעשייה", "עכו" },
        { "עכו רמות ים", "עכו" }
    };

            return aliases.TryGetValue(name, out var canonical)
                ? canonical
                : name;
        }

        private static bool StartsWith(string text, string baseName)
        {
            return text == baseName || text.StartsWith(baseName + " ");
        }

        private static readonly Dictionary<string, (double Latitude, double Longitude)> CoordinateOverrides = new()
{
    { "אבו תלול", (31.195, 34.782) },
    { "אבו קרינאת", (31.220, 34.865) },
    { "אום בטין", (31.245, 34.938) },
    { "ביר הדאג", (31.245, 34.786) },
    { "שגב שלום", (31.1976411516052, 34.8390825304992) },
    { "חירן", (31.301, 34.932) },
    { "אל סייד", (31.276, 34.916) },
    { "אל עזי", (31.7208576687222, 34.8039265935459) },
    { "אל פורעה", (31.301, 35.028) },
    { "ואדי אל נעם", (31.250, 34.900) },
    { "תארבין", (31.300, 34.880) },
    { "קסר א סר", (31.240, 34.915) },
    { "ערערה בנגב", (31.1553589486213, 35.022268052232) },
    { "כסייפה", (31.2447193134764, 35.0800448077145) },
    { "לקיה", (31.3238443693905, 34.8647645020895) },
    { "רהט", (31.3927681500537, 34.7559551560021) },
    { "חורה", (31.2985131540456, 34.9346088118738) },
    { "תל שבע", (31.2480492657582, 34.8601763927084) },
    { "נמרוד", (33.250, 35.740) },
    { "נעמה", (31.790, 35.460) },
    { "נערן", (31.870, 35.450) },
    { "נצר חזני", (31.900, 35.030) },
    { "נריה", (31.930, 35.140) },
    { "איירפורט סיטי", (31.984, 34.915) },
    { "מיני ישראל נחשון", (31.832, 34.955) },
    { "מכון וינגייט", (32.268, 34.842) },
    { "תחנת רכבת ראש העין", (32.095, 34.956) },
    { "תחנת רכבת קריית מלאכי יואב", (31.735, 34.746) },
    { "חוף זיקים", (31.605, 34.517) },
    { "חוף קליה", (31.7507076719296, 35.4671301033167) },
    { "עין בוקק", (31.200, 35.362) },
    { "מרחצאות עין גדי", (31.4577416116035, 35.3905423383238) },
    { "מלונות ים המלח מרכז", (31.200, 35.360) },
    { "בתי מלון ים המלח", (31.200, 35.360) }
};


        [HttpGet]
        public IActionResult GetLatestRedAlerts()
        {
            var now = DateTime.UtcNow;
            var last15Minutes = now.AddMinutes(-15);

            var alerts = _context.Alerts
                .Where(a => a.AlertTimeUtc >= last15Minutes)
                .OrderByDescending(a => a.AlertTimeUtc)
                .Select(a => new
                {
                    cityName = a.CityHebrew,
                    alertTimeText = a.AlertTimeUtc.ToLocalTime().ToString("HH:mm:ss")
                })
                .Take(20)
                .ToList();

            return Json(alerts);
        }
        private bool TryReadCentroid(JsonElement centroid, out double x, out double y)
        {
            x = 0;
            y = 0;

            if (centroid.ValueKind == JsonValueKind.Object)
            {
                if (centroid.TryGetProperty("x", out var xProp) &&
                    centroid.TryGetProperty("y", out var yProp) &&
                    xProp.TryGetDouble(out x) &&
                    yProp.TryGetDouble(out y))
                {
                    return true;
                }
            }

            if (centroid.ValueKind == JsonValueKind.Array && centroid.GetArrayLength() >= 2)
            {
                var arr = centroid.EnumerateArray().ToArray();
                if (arr[0].TryGetDouble(out x) && arr[1].TryGetDouble(out y))
                {
                    return true;
                }
            }

            return false;
        }


        private static string? GetFieldValue(List<GovMapField> fields, string fieldName)
        {
            return fields.FirstOrDefault(f => f.FieldName == fieldName)?.FieldValue?.ToString();
        }


        public class LocationRequest
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public int Count { get; set; }
        }
        [HttpPost]
        public IActionResult GetClosestShelters([FromBody] LocationRequest req)
        {
            if (req == null)
                return BadRequest("Invalid request");

            var shelters = _context.Shelters
                .Where(s => s.IsActive && s.Latitude != 0 && s.Longitude != 0)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Latitude,
                    s.Longitude
                })
                .AsNoTracking()
                .ToList()
                .GroupBy(s => new
                {
                    Lat = Math.Round(s.Latitude, 4),
                    Lng = Math.Round(s.Longitude, 4)
                })
                .Select(g => g.First())
                .ToList();

            var result = shelters
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    lat = s.Latitude,
                    lng = s.Longitude,
                    distance = Haversine(req.Latitude, req.Longitude, s.Latitude, s.Longitude)
                })
                .OrderBy(x => x.distance)
                .Take(req.Count > 0 ? req.Count : 10)
                .ToList();

            return Json(result);
        }


        private double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000;

            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;

            lat1 = lat1 * Math.PI / 180;
            lat2 = lat2 * Math.PI / 180;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1) * Math.Cos(lat2) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        [HttpGet]
        public IActionResult GetFeedbacks(int shelterId)
        {
            var feedbacks = _context.Feedbacks
                .Where(f => f.ShelterId == shelterId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new
                {
                    f.UserName,
                    f.Comment,
                    CreatedAt = f.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                })
                .ToList();

            return Json(feedbacks);
        }

    }

}