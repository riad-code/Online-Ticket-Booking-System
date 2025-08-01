﻿using ONLINE_TICKET_BOOKING_SYSTEM.Models;
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
    }



}
