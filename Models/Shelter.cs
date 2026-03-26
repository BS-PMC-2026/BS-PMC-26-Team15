namespace SamiSpot.Models
{
    public class Shelter
    {
        public int Id { get; set; }
        public string? ExternalId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? ShelterType { get; set; }
        public int? Capacity { get; set; }
        public bool IsAccessible { get; set; }
        public bool IsPublic { get; set; }
        public string Source { get; set; } = string.Empty;
        public string? SourceUrl { get; set; }
        public DateTime? LastSyncedAt { get; set; }
        public bool IsActive { get; set; }
        public string? RemoteOpen { get; set; }
        public string? GovMapMiklatId { get; set; }
    }
}