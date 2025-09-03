using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using ONLINE_TICKET_BOOKING_SYSTEM.Models.Air;
using ONLINE_TICKET_BOOKING_SYSTEM.Models.Event;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ===== Bus (existing) =====
        public DbSet<Bus> Buses { get; set; } = default!;
        public DbSet<BusSchedule> BusSchedules { get; set; } = default!;
        public DbSet<SeatLayout> SeatLayouts { get; set; } = default!;
        public DbSet<ScheduleSeat> ScheduleSeats { get; set; } = default!;
        public DbSet<Booking> Bookings { get; set; } = default!;
        public DbSet<BookingSeat> BookingSeats { get; set; } = default!;
        public DbSet<ONLINE_TICKET_BOOKING_SYSTEM.Models.ContactMessage> ContactMessages { get; set; } = default!;

        // ===== Air =====
        public DbSet<Airport> Airports { get; set; } = default!;
        public DbSet<Airline> Airlines { get; set; } = default!;
        public DbSet<FlightSchedule> FlightSchedules { get; set; } = default!;
        public DbSet<FareClass> FareClasses { get; set; } = default!;
        public DbSet<Itinerary> Itineraries { get; set; } = default!;
        public DbSet<FlightSegment> FlightSegments { get; set; } = default!;
        public DbSet<AirBooking> AirBookings { get; set; } = default!;
        public DbSet<Passenger> Passengers { get; set; } = default!;

        //event
        public DbSet<EventItem> EventItems { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // --- ApplicationUser defaults ---
            builder.Entity<ApplicationUser>()
                   .Property(u => u.RegisteredAtUtc)
                   .HasDefaultValueSql("SYSUTCDATETIME()");

            // --- Bus money precision ---
            builder.Entity<Bus>().Property(b => b.Fare).HasPrecision(10, 2);
            builder.Entity<BusSchedule>().Property(s => s.Fare).HasPrecision(10, 2);
            builder.Entity<Booking>().Property(b => b.TotalFare).HasPrecision(12, 2);
            builder.Entity<BookingSeat>().Property(bs => bs.Fare).HasPrecision(10, 2);

            // Often missed:
            builder.Entity<Booking>().Property(b => b.Discount).HasPrecision(12, 2);
            builder.Entity<Booking>().Property(b => b.GrandTotal).HasPrecision(12, 2);
            builder.Entity<Booking>().Property(b => b.InsuranceFee).HasPrecision(12, 2);
            builder.Entity<Booking>().Property(b => b.ProcessingFee).HasPrecision(12, 2);

            // --- Bus relations ---
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

            builder.Entity<BusSchedule>().HasIndex(bs => new { bs.From, bs.To, bs.JourneyDate });
            builder.Entity<BusSchedule>().HasIndex(bs => bs.OperatorName);

            builder.Entity<Bus>().Property(b => b.BoardingPointsString).HasMaxLength(2000);
            builder.Entity<Bus>().Property(b => b.DroppingPointsString).HasMaxLength(2000);
            builder.Entity<BusSchedule>().Property(b => b.BoardingPointsString).HasMaxLength(2000);
            builder.Entity<BusSchedule>().Property(b => b.DroppingPointsString).HasMaxLength(2000);

            builder.Entity<ScheduleSeat>()
                .HasIndex(x => new { x.BusScheduleId, x.SeatNo })
                .IsUnique();

            builder.Entity<ScheduleSeat>()
                .HasOne(s => s.BusSchedule)
                .WithMany()
                .HasForeignKey(s => s.BusScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<BookingSeat>()
                .HasOne(bs => bs.Booking)
                .WithMany(b => b.Seats)
                .HasForeignKey(bs => bs.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // VERY IMPORTANT: Restrict here to avoid multiple cascade paths
            builder.Entity<BookingSeat>()
                .HasOne(bs => bs.ScheduleSeat)
                .WithMany()
                .HasForeignKey(bs => bs.ScheduleSeatId)
                .OnDelete(DeleteBehavior.Restrict);
               
            // ===== Air constraints =====
            builder.Entity<Airport>().HasIndex(a => a.IataCode).IsUnique();
            builder.Entity<Airline>().HasIndex(a => a.IataCode).IsUnique();

            builder.Entity<FlightSchedule>()
                .HasOne(fs => fs.Airline).WithMany()
                .HasForeignKey(fs => fs.AirlineId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FlightSchedule>()
                .HasOne(fs => fs.FromAirport).WithMany()
                .HasForeignKey(fs => fs.FromAirportId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FlightSchedule>()
                .HasOne(fs => fs.ToAirport).WithMany()
                .HasForeignKey(fs => fs.ToAirportId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FareClass>()
                .HasOne(f => f.FlightSchedule).WithMany()
                .HasForeignKey(f => f.FlightScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Money precision (Air)
            builder.Entity<FareClass>().Property(x => x.BaseFare).HasPrecision(12, 2);
            builder.Entity<FareClass>().Property(x => x.TaxesAndFees).HasPrecision(12, 2);
            builder.Entity<Itinerary>().Property(x => x.TotalBase).HasPrecision(12, 2);
            builder.Entity<Itinerary>().Property(x => x.TotalTax).HasPrecision(12, 2);
            builder.Entity<Itinerary>().Property(x => x.GrandTotal).HasPrecision(12, 2);
            builder.Entity<FlightSegment>().Property(x => x.PaxBase).HasPrecision(12, 2);
            builder.Entity<FlightSegment>().Property(x => x.PaxTax).HasPrecision(12, 2);
            builder.Entity<AirBooking>().Property(x => x.AmountDue).HasPrecision(12, 2);
            builder.Entity<AirBooking>().Property(x => x.AmountPaid).HasPrecision(12, 2);

            builder.Entity<Itinerary>().Property(x => x.Currency).HasMaxLength(3);
            builder.Entity<FlightSegment>().Property(x => x.Currency).HasMaxLength(3);

            builder.Entity<Itinerary>()
                .HasMany(i => i.Segments)
                .WithOne(s => s.Itinerary)
                .HasForeignKey(s => s.ItineraryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AirBooking>()
                .HasOne(b => b.Itinerary).WithMany()
                .HasForeignKey(b => b.ItineraryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AirBooking>().HasIndex(b => b.Pnr).IsUnique();
            builder.Entity<AirBooking>().Property(b => b.Currency).HasMaxLength(3);

            builder.Entity<Passenger>()
                .HasOne(p => p.AirBooking)
                .WithMany(b => b.Passengers)
                .HasForeignKey(p => p.AirBookingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Passenger>().Property(p => p.Gender).HasMaxLength(10);
            //event
            builder.Entity<EventItem>(e =>
            {
                e.HasIndex(x => x.StartDateUtc);
                e.HasIndex(x => x.City);
                e.HasIndex(x => x.Category);
                e.HasIndex(x => x.IsFeatured);
            });
        }
    }
}
