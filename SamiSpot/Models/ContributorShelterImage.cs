using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SamiSpot.Models
{
    public class ContributorShelterImage
    {
        public int Id { get; set; }

        [Required]
        public int ContributorShelterId { get; set; }

        [ForeignKey("ContributorShelterId")]
        public ContributorShelter ContributorShelter { get; set; }

        [Required]
        public string ImageUrl { get; set; }
    }
}