namespace SamiSpot.Models
{
    public class ChatIntentResult
    {
        public string Intent { get; set; } = "";
        public string? CityName { get; set; }
        public bool NeedsUserLocation { get; set; }
        public bool WantsMap { get; set; }
    }
}