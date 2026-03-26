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
            return View();
        }

        private List<double[]> GenerateIsraelGridPoints()
        {
            var points = new List<double[]>();

            double minX = 3820000;
            double maxX = 3950000;
            double minY = 3450000;
            double maxY = 3765000;

            double step = 30000; // 10 km

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

            var samplePoints = GenerateIsraelGridPoints();

            int added = 0;
            int updated = 0;
            int skipped = 0;

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
                        skipped++;
                        continue;
                    }

                    var responseText = await response.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var parsed = JsonSerializer.Deserialize<GovMapLayerResponse>(responseText, options);

                    if (parsed?.Data == null || parsed.Data.Count == 0)
                    {
                        skipped++;
                        continue;
                    }

                    foreach (var layer in parsed.Data)
                    {
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

                            double x, y;

                            if (!TryReadCentroid(entity.Centroid, out x, out y))
                                continue;

                            var (lat, lng) = CoordinateHelper.WebMercatorToLatLng(x, y);

                            Shelter? existing;

                            if (!string.IsNullOrWhiteSpace(miklatId))
                            {
                                existing = await _context.Shelters.FirstOrDefaultAsync(s =>
                                    s.Source == "GovMap" &&
                                    s.GovMapMiklatId == miklatId);
                            }
                            else
                            {
                                existing = await _context.Shelters.FirstOrDefaultAsync(s =>
                                    s.Source == "GovMap" &&
                                    s.Latitude == lat &&
                                    s.Longitude == lng);
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

                            existing.Name = string.IsNullOrWhiteSpace(miklatNum)
                                ? "GovMap Shelter"
                                : $"מקלט {miklatNum}";

                            existing.Address = address;
                            existing.City = area;
                            existing.Latitude = lat;
                            existing.Longitude = lng;
                            existing.ShelterType = "Public Shelter";
                            existing.IsAccessible = false;
                            existing.IsPublic = true;
                            existing.IsActive = true;
                            existing.SourceUrl = GovMapUrl;
                            existing.LastSyncedAt = DateTime.UtcNow;
                        }
                    }

                  
                }
                catch
                {
                    skipped++;
                }
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                message = "GovMap Israel scan finished",
                added,
                updated,
                skipped,
                totalPoints = samplePoints.Count
            });
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

        
    }
}