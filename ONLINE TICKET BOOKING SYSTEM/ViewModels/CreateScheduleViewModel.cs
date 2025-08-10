using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ONLINE_TICKET_BOOKING_SYSTEM.ViewModels
{
    public class CreateScheduleViewModel
    {
        public int Id { get; set; } // For Edit, 0 or default for Create

        [Required(ErrorMessage = "Please select a bus.")]
        public int BusId { get; set; }

        public int? ReturnBusId { get; set; } // Optional return bus selection

        [Required(ErrorMessage = "Journey date is required.")]
        [DataType(DataType.Date)]
        public DateTime JourneyDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ReturnDate { get; set; } // Optional

        [Required(ErrorMessage = "Departure time is required.")]
        [DataType(DataType.Time)]
        public TimeSpan DepartureTime { get; set; }

        [Required(ErrorMessage = "Arrival time is required.")]
        [DataType(DataType.Time)]
        public TimeSpan ArrivalTime { get; set; }

        [Required(ErrorMessage = "Fare is required.")]
        [Range(0, 10000, ErrorMessage = "Fare must be between 0 and 10,000.")]
        public decimal Fare { get; set; }

        [Required(ErrorMessage = "Seats available is required.")]
        [Range(1, 100, ErrorMessage = "Seats available must be between 1 and 100.")]
        public int SeatsAvailable { get; set; }

        // Optional fields for displaying info in form (readonly)
        public string From { get; set; }
        public string To { get; set; }
        public string FullRoute { get; set; }

        // Dropdown list of all buses to populate selection in form
        public List<Bus> AllBuses { get; set; } = new List<Bus>();
    }
}
