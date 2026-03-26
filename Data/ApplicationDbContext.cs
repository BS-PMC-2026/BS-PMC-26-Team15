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
    }
}