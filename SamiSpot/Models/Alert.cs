
using System;

namespace SamiSpot.Models
{
    public class Alert
    {
        public int Id { get; set; }

        public string CityHebrew { get; set; } = "";

        public DateTime AlertTimeUtc { get; set; }

        public int Threat { get; set; }

        public bool IsDrill { get; set; }
    }
}
