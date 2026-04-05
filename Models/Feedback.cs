using System;

namespace SamiSpot.Models
{
    public class Feedback
    {
        public int Id { get; set; }

        public int ShelterId { get; set; }   // which shelter

        public string UserName { get; set; }

        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}