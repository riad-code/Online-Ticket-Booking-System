using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Models.Air
{
    // ----- Enums -----
    public enum CabinClass { Economy, PremiumEconomy, Business, First }
    public enum PaxType { Adult, Child, Infant }

    // ----- Master Data -----
    public class Airport
    {
        public int Id { get; set; }

        [Required, StringLength(3)]
        public string IataCode { get; set; } = default!;   // e.g. DAC

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
        public string IataCode { get; set; } = default!;   // e.g. BG

        [Required, StringLength(120)]
        public string Name { get; set; } = default!;
    }

    // ----- Schedules (published daily pattern) -----
    public class FlightSchedule
    {
        public int Id { get; set; }

        [Required] public int AirlineId { get; set; }
        [ValidateNever] public Airline? Airline { get; set; }

        [Required] public int FromAirportId { get; set; }
        [ValidateNever] public Airport? FromAirport { get; set; }

        [Required] public int ToAirportId { get; set; }
        [ValidateNever] public Airport? ToAirport { get; set; }

        [Required, StringLength(12)]
        public string FlightNumber { get; set; } = "";

        [Required]
        public TimeOnly DepTimeLocal { get; set; }     // HH:mm

        [Required]
        public TimeOnly ArrTimeLocal { get; set; }     // HH:mm

        [Range(0, int.MaxValue)]
        public int DurationMinutes { get; set; }

        [Range(0, 127)]
        public int OperatingDaysMask { get; set; } = 127; // bitmask

        [StringLength(20)]
        public string? Equipment { get; set; }
    }

    // ----- Fare buckets -----
    public class FareClass
    {
        public int Id { get; set; }

        [Required]
        public int FlightScheduleId { get; set; }
        [ValidateNever] public FlightSchedule? FlightSchedule { get; set; }

        [Required, StringLength(2)]
        public string Rbd { get; set; } = "";

        [Required]
        public CabinClass Cabin { get; set; } = CabinClass.Economy;

        [Range(0, int.MaxValue)]
        public int SeatsAvailable { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal BaseFare { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TaxesAndFees { get; set; }

        [Required, StringLength(3)]
        public string Currency { get; set; } = "BDT";

        [StringLength(40)]
        public string? Baggage { get; set; }

        public bool Refundable { get; set; } = false;
    }

    // ----- Quote/Booking -----
    public class FlightSegment
    {
        public int Id { get; set; }

        public int ItineraryId { get; set; }
        [ValidateNever] public Itinerary Itinerary { get; set; } = default!;

        public int FlightScheduleId { get; set; }
        [ValidateNever] public FlightSchedule FlightSchedule { get; set; } = default!;

        public DateOnly TravelDate { get; set; }

        [StringLength(2)] public string? ChosenRbd { get; set; }
        public CabinClass Cabin { get; set; } = CabinClass.Economy;

        public decimal PaxBase { get; set; }
        public decimal PaxTax { get; set; }

        [StringLength(3)] public string Currency { get; set; } = "BDT";
    }

    public class Itinerary
    {
        public int Id { get; set; }

        public List<FlightSegment> Segments { get; set; } = new();

        public CabinClass Cabin { get; set; } = CabinClass.Economy;

        public decimal TotalBase { get; set; }
        public decimal TotalTax { get; set; }
        public decimal GrandTotal { get; set; }

        [StringLength(3)] public string Currency { get; set; } = "BDT";

        public DateTime ExpiresAtUtc { get; set; }
    }

    public class Passenger
    {
        public int Id { get; set; }

        public int AirBookingId { get; set; }
        [ValidateNever] public AirBooking AirBooking { get; set; } = default!;

        [Required, StringLength(60)] public string FirstName { get; set; } = default!;
        [Required, StringLength(60)] public string LastName { get; set; } = default!;

        [Required] public DateOnly Dob { get; set; }
        [Required] public PaxType Type { get; set; }

        [StringLength(10)] public string? Gender { get; set; }
        [StringLength(20)] public string? PassportNo { get; set; }
    }

    public class AirBooking
    {
        public int Id { get; set; }

        [Required, StringLength(6)] public string Pnr { get; set; } = default!;

        public int ItineraryId { get; set; }
        [ValidateNever] public Itinerary Itinerary { get; set; } = default!;

        public int Adults { get; set; }
        public int Children { get; set; }
        public int Infants { get; set; }

        public List<Passenger> Passengers { get; set; } = new();

        public decimal AmountDue { get; set; }
        public decimal AmountPaid { get; set; }

        // ✅ reference the GLOBAL BookingStatus (no ambiguity)
        public ONLINE_TICKET_BOOKING_SYSTEM.Models.BookingStatus Status { get; set; }
            = ONLINE_TICKET_BOOKING_SYSTEM.Models.BookingStatus.PendingPayment;

        [StringLength(3)] public string Currency { get; set; } = "BDT";

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? PaidAtUtc { get; set; }
    }
}