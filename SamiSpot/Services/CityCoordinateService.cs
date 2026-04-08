using SamiSpot.Data;

namespace SamiSpot.Services
{
    public class CityCoordinateService
    {
        private readonly ApplicationDbContext _context;

        public CityCoordinateService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void UpdateCityLatLng()
        {
            var cities = _context.CityLocations
                .Where(c => c.Latitude == 0 || c.Longitude == 0)
                .ToList();

            foreach (var city in cities)
            {
                var (lat, lng) = ConvertWebMercatorToLatLng(city.X, city.Y);

                city.Latitude = lat;
                city.Longitude = lng;
            }

            _context.SaveChanges();
        }

        private (double lat, double lng) ConvertWebMercatorToLatLng(double x, double y)
        {
            const double R = 6378137.0;

            double lng = (x / R) * 180.0 / Math.PI;
            double lat = (2 * Math.Atan(Math.Exp(y / R)) - Math.PI / 2) * 180.0 / Math.PI;

            return (lat, lng);
        }
    }
}