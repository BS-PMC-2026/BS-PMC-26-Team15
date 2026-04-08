using SamiSpot.Data;
using SamiSpot.Models;
using System.Globalization;

namespace SamiSpot.Services
{
    public class CityImportService
    {
        private readonly ApplicationDbContext _context;

        public CityImportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void ImportCitiesFromCsv(string filePath)
        {
            if (_context.CityLocations.Any())
                return;

            if (!File.Exists(filePath))
                return;

            var lines = File.ReadAllLines(filePath);

            if (lines.Length <= 1)
                return;

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(',');

                if (parts.Length < 10)
                    continue;

                if (!double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double x))
                    continue;

                if (!double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double y))
                    continue;

                var hebrewName = parts[5].Trim();
                var englishName = parts[9].Trim();

                if (string.IsNullOrWhiteSpace(hebrewName))
                    continue;

                var city = new CityLocation
                {
                    HebrewName = hebrewName,
                    EnglishName = englishName,
                    X = x,
                    Y = y,
                    Latitude = 0,
                    Longitude = 0
                };

                _context.CityLocations.Add(city);
            }

            _context.SaveChanges();
        }
    }
}