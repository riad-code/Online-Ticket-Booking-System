using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<BusSchedule> BusSchedules { get; set; }
        public DbSet<Bus> Buses { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            
            builder.Entity<ApplicationUser>()
                   .Property(u => u.RegisteredAtUtc)
                   .HasDefaultValueSql("SYSUTCDATETIME()");

           
        }
    }
}
