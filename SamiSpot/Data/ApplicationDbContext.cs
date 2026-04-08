using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SamiSpot.Models;

namespace SamiSpot.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Shelter> Shelters { get; set; }

        public DbSet<Alert> Alerts { get; set; }

        public DbSet<CityLocation> CityLocations { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<FeedbackReply> FeedbackReplies { get; set; }
       
    }
}