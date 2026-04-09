using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SamiSpot.Models
{
    public class ContributorShelter
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public string? Description { get; set; }

        public int? Size { get; set; }

        public bool IsAvailable { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public List<ContributorShelterImage> Images { get; set; } = new List<ContributorShelterImage>();
    }
}