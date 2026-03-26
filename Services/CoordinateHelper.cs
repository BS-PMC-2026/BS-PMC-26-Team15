namespace SamiSpot.Services
{
    public static class CoordinateHelper
    {
        public static (double Latitude, double Longitude) WebMercatorToLatLng(double x, double y)
        {
            double originShift = 2 * Math.PI * 6378137 / 2.0;

            double lon = (x / originShift) * 180.0;
            double lat = (y / originShift) * 180.0;

            lat = 180.0 / Math.PI *
                  (2 * Math.Atan(Math.Exp(lat * Math.PI / 180.0)) - Math.PI / 2.0);

            return (lat, lon);
        }
    }
}