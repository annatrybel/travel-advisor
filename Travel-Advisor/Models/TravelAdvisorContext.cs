using Microsoft.EntityFrameworkCore;
using Travel_Advisor.Models;
using Microsoft.AspNetCore.Http;

namespace Travel_Advisor.Models
{
    public class TravelAdvisorContext : DbContext
    {
        public TravelAdvisorContext(DbContextOptions<TravelAdvisorContext> options) : base(options)
        {
        }

        public virtual DbSet<Destination> Destinations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}