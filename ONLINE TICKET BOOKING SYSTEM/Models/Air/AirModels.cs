using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Models.Air
{
    // ----- Enums (only air-specific ones) -----
    public enum CabinClass { Economy, PremiumEconomy, Business, First }
    public enum PaxType { Adult, Child, Infant }

    // ----- Master Data -----
    public class Airport
    {
        public int Id { get; set; }

        [Required, StringLength(3)]
        public string IataCode { get; set; } = default!; // e.g., DAC

        [Required, StringLength(120)]
        public string Name { get; set; } = default!;

        [Required, StringLength(80)]
        public string City { get; set; } = default!;

        [Required, StringLength(80)]
        public string Country { get; set; } = default!;
    }

    public class Airline
    {
        public int Id { get; set; }

        [Required, StringLength(2)]
        public string IataCode { get; set; } = default!; // e.g., BG

        [Required, StringLength(120)]
        public string Name { get; set; } = default!;
    }

    // Published timetable (daily pattern)
    public class FlightSchedule
    {
        public int Id { get; set; }

        // FK
        public int AirlineId { get; set; }
        public Airline Airline { get; set; } = default!;

        public int FromAirportId { get; set; }
        public Airport FromAirport { get; set; } = default!;

        public int ToAirportId { get; set; }
        public Airport ToAirport { get; set; } = default!;

        [Required, StringLength(8)]
        public string FlightNumber { get; set; } = default!; // e.g., BG147

        // Local times
        public TimeOnly DepTimeLocal { get; set; }
        public TimeOnly ArrTimeLocal { get; set; }

        // duration in minutes (optional but handy)
        public int DurationMinutes { get; set; }

        // Bitmask 7 bits (Mon..Sun) => 127 means daily
        public int OperatingDaysMask { get; set; } = 127;

        [StringLength(12)]
        public string? Equipment { get; set; } // e.g., 738
    }

    // Pricing buckets published against a schedule
    public class FareClass
    {
        public int Id { get; set; }

        public int FlightScheduleId { get; set; }
        public FlightSchedule FlightSchedule { get; set; } = default!;

        [Required, StringLength(2)]
        public string Rbd { get; set; } = "Y"; // booking class: Y,J etc.

        [Required]
        public CabinClass Cabin { get; set; } = CabinClass.Economy;

        // inventory and price
        public int SeatsAvailable { get; set; }
        public decimal BaseFare { get; set; }       // per pax
        public decimal TaxesAndFees { get; set; }   // per pax

        [StringLength(40)]
        public string? Baggage { get; set; }

        public bool Refundable { get; set; } = false;
        public string Currency { get; set; } = "BDT";
    }

    // ----- Shopping / Quote -----
    // A selected operating leg on a specific date
    public class FlightSegment
    {
        public int Id { get; set; }

        // FK
        public int ItineraryId { get; set; }
        public Itinerary Itinerary { get; set; } = default!;

        public int FlightScheduleId { get; set; }
        public FlightSchedule FlightSchedule { get; set; } = default!;

        // travel date for this segment
        public DateOnly TravelDate { get; set; }

        // (optional) chosen fare info snapshot at quote time
        [StringLength(2)]
        public string? ChosenRbd { get; set; }
        public CabinClass Cabin { get; set; } = CabinClass.Economy;

        public decimal PaxBase { get; set; }     // per pax snapshot
        public decimal PaxTax { get; set; }      // per pax snapshot

        [StringLength(3)]
        public string Currency { get; set; } = "BDT";
    }

    // A priced offer (can be one-way, return, or multi-city)
    public class Itinerary
    {
        public int Id { get; set; }

        public List<FlightSegment> Segments { get; set; } = new();

        public CabinClass Cabin { get; set; } = CabinClass.Economy;

        // totals for all passengers (snapshot)
        public decimal TotalBase { get; set; }
        public decimal TotalTax { get; set; }
        public decimal GrandTotal { get; set; }

        [StringLength(3)]
        public string Currency { get; set; } = "BDT";

        // Quote validity
        public DateTime ExpiresAtUtc { get; set; }
    }

    // ----- Booking -----
    public class Passenger
    {
        public int Id { get; set; }

        public int AirBookingId { get; set; }
        public AirBooking AirBooking { get; set; } = default!;

        [Required, StringLength(60)]
        public string FirstName { get; set; } = default!;

        [Required, StringLength(60)]
        public string LastName { get; set; } = default!;

        [Required]
        public DateOnly Dob { get; set; }

        [Required]
        public PaxType Type { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        [StringLength(20)]
        public string? PassportNo { get; set; }
    }

    public class AirBooking
    {
        public int Id { get; set; }

        // 6-char airline-style record locator
        [Required, StringLength(6)]
        public string Pnr { get; set; } = default!;

        // Link to priced itinerary
        public int ItineraryId { get; set; }
        public Itinerary Itinerary { get; set; } = default!;

        // Pax counts
        public int Adults { get; set; }
        public int Children { get; set; }
        public int Infants { get; set; }

        public List<Passenger> Passengers { get; set; } = new();

        // Money
        public decimal AmountDue { get; set; }
        public decimal AmountPaid { get; set; }

        // ✅ Using central BookingStatus (Models/BookingStatus.cs)
        public BookingStatus Status { get; set; } = BookingStatus.PendingPayment;

        [StringLength(3)]
        public string Currency { get; set; } = "BDT";

        // Timestamps
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? PaidAtUtc { get; set; }
    }
}
