using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SamiSpot.Models
{
    public class ContributorShelterFormViewModel
    {
        [Required(ErrorMessage = "Shelter name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Shelter name must be between 3 and 100 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Please choose a location from the map or search box.")]
        [StringLength(250, ErrorMessage = "Address cannot be longer than 250 characters.")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Please choose a location on the map.")]
        [Range(-90, 90, ErrorMessage = "Latitude is invalid.")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Please choose a location on the map.")]
        [Range(-180, 180, ErrorMessage = "Longitude is invalid.")]
        public double Longitude { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please enter the shelter size.")]
        [Range(1, 5000, ErrorMessage = "Shelter size must be between 1 and 5000 people.")]
        public int? Size { get; set; }
        public string? DeletedImageIds { get; set; }
        public bool IsAvailable { get; set; }
        public List<IFormFile>? Images { get; set; }
    }
}