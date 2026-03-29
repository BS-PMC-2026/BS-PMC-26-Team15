namespace SamiSpot.Models
{
    public class CityLocation
    {
        public int Id { get; set; }

        public string HebrewName { get; set; } = "";

        public string EnglishName { get; set; } = "";

        public double X { get; set; }

        public double Y { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }
}