using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Bus> Buses { get; set; } = default!;
        public DbSet<BusSchedule> BusSchedules { get; set; } = default!;

        public DbSet<SeatLayout> SeatLayouts { get; set; } = default!;
        public DbSet<ScheduleSeat> ScheduleSeats { get; set; } = default!;
        public DbSet<Booking> Bookings { get; set; } = default!;
        public DbSet<BookingSeat> BookingSeats { get; set; } = default!;
        public DbSet<ONLINE_TICKET_BOOKING_SYSTEM.Models.ContactMessage> ContactMessages { get; set; }



        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // --- ApplicationUser default ---
            builder.Entity<ApplicationUser>()
                   .Property(u => u.RegisteredAtUtc)
                   .HasDefaultValueSql("SYSUTCDATETIME()");

            // --- Decimal precision (warnings বন্ধ) ---
            builder.Entity<Bus>().Property(b => b.Fare).HasPrecision(10, 2);
            builder.Entity<BusSchedule>().Property(s => s.Fare).HasPrecision(10, 2);
            builder.Entity<Booking>().Property(b => b.TotalFare).HasPrecision(12, 2);
            builder.Entity<BookingSeat>().Property(bs => bs.Fare).HasPrecision(10, 2);

            // --- BusSchedule <-> Bus ---
            builder.Entity<BusSchedule>()
                .HasOne(bs => bs.Bus)
                .WithMany()
                .HasForeignKey(bs => bs.BusId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<BusSchedule>()
                .HasOne(bs => bs.ReturnBus)
                .WithMany()
                .HasForeignKey(bs => bs.ReturnBusId)
                .OnDelete(DeleteBehavior.SetNull);

            // --- Useful indexes ---
            builder.Entity<BusSchedule>().HasIndex(bs => new { bs.From, bs.To, bs.JourneyDate });
            builder.Entity<BusSchedule>().HasIndex(bs => bs.OperatorName);

            // --- Big text length hints (optional, আপনার আগের মতো) ---
            builder.Entity<Bus>().Property(b => b.BoardingPointsString).HasMaxLength(2000);
            builder.Entity<Bus>().Property(b => b.DroppingPointsString).HasMaxLength(2000);
            builder.Entity<BusSchedule>().Property(b => b.BoardingPointsString).HasMaxLength(2000);
            builder.Entity<BusSchedule>().Property(b => b.DroppingPointsString).HasMaxLength(2000);

            // --- ScheduleSeat uniqueness + relation ---
            builder.Entity<ScheduleSeat>()
                .HasIndex(x => new { x.BusScheduleId, x.SeatNo })
                .IsUnique();

            builder.Entity<ScheduleSeat>()
                .HasOne(s => s.BusSchedule)
                .WithMany()
                .HasForeignKey(s => s.BusScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- BookingSeat relation ---
            builder.Entity<BookingSeat>()
                .HasOne(bs => bs.Booking)
                .WithMany(b => b.Seats)
                .HasForeignKey(bs => bs.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<BookingSeat>()
                .HasOne(bs => bs.ScheduleSeat)
                .WithMany()
                .HasForeignKey(bs => bs.ScheduleSeatId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
