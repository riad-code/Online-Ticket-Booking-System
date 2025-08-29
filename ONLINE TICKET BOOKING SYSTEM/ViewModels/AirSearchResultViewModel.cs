using System;
using System.Collections.Generic;

namespace ONLINE_TICKET_BOOKING_SYSTEM.ViewModels
{
    public class AirSearchResultViewModel
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public DateTime JourneyDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string TripType { get; set; } = "oneway";
        public string Cabin { get; set; } = "Economy";
        public int Travellers { get; set; } = 1;

        // ✅ Initialize to empty lists to avoid null refs
        public List<FlightCardVm> AvailableFlights { get; set; } = new();
        public List<FlightCardVm> ReturnFlights { get; set; } = new();

        // sidebar filters
        public List<string> AllAirlines { get; set; } = new();
    }

    public class FlightCardVm
    {
        public int ScheduleId { get; set; }
        public string AirlineCode { get; set; } = string.Empty;
        public string AirlineName { get; set; } = string.Empty;
        public string FlightNumber { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string DepTime { get; set; } = string.Empty;
        public string ArrTime { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string Cabin { get; set; } = string.Empty;
        public string Currency { get; set; } = "BDT";
        public decimal Price { get; set; }
        public int SeatsAvailable { get; set; }
        public string TravelDate { get; set; } = string.Empty;
    }
}
