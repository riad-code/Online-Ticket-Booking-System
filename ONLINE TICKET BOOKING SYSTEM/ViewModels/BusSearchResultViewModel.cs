using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using System;
using System.Collections.Generic;

namespace ONLINE_TICKET_BOOKING_SYSTEM.ViewModels
{
    public class BusSearchResultViewModel
    {
        public string? From { get; set; }
        public string? To { get; set; }
        public DateTime JourneyDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string? TripType { get; set; }
        public List<BusSchedule> AvailableBuses { get; set; } = new List<BusSchedule>();
        public List<BusSchedule>? ReturnBuses { get; set; }
        // ✅ Left sidebar universes (must exist and be initialized)
        public List<string> AllOperators { get; set; } = new List<string>();
        public List<string> AllBoardingPoints { get; set; } = new List<string>();
        public List<string> AllDroppingPoints { get; set; } = new List<string>();
    }



}
