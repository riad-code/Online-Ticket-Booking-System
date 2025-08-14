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

            // ApplicationUser default
            builder.Entity<ApplicationUser>()
                   .Property(u => u.RegisteredAtUtc)
                   .HasDefaultValueSql("SYSUTCDATETIME()");

            // ---- BusSchedule <-> Bus relationships ----
            builder.Entity<BusSchedule>()
                .HasOne(bs => bs.Bus)
                .WithMany() // schedules are managed separately
                .HasForeignKey(bs => bs.BusId)
                .OnDelete(DeleteBehavior.SetNull); // keep schedule if bus removed

            builder.Entity<BusSchedule>()
                .HasOne(bs => bs.ReturnBus)
                .WithMany()
                .HasForeignKey(bs => bs.ReturnBusId)
                .OnDelete(DeleteBehavior.SetNull);

            // ---- Indexes to speed up search ----
            builder.Entity<BusSchedule>()
                .HasIndex(bs => new { bs.From, bs.To, bs.JourneyDate });

            // (Optional) useful single-column indexes
            builder.Entity<BusSchedule>()
                .HasIndex(bs => bs.OperatorName);

            // ---- Optional: max length hints for big text columns ----
            builder.Entity<Bus>()
                .Property(b => b.BoardingPointsString)
                .HasMaxLength(2000);

            builder.Entity<Bus>()
                .Property(b => b.DroppingPointsString)
                .HasMaxLength(2000);

            builder.Entity<BusSchedule>()
                .Property(b => b.BoardingPointsString)
                .HasMaxLength(2000);

            builder.Entity<BusSchedule>()
                .Property(b => b.DroppingPointsString)
                .HasMaxLength(2000);
        }

    }
}
