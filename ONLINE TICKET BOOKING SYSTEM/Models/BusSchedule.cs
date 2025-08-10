using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Models
{
    public class BusSchedule
    {
        public int Id { get; set; }

        // FK to Bus (outbound)
        public int? BusId { get; set; }
        [ForeignKey("BusId")]
        public Bus? Bus { get; set; }

        // Optional FK to return bus
        public int? ReturnBusId { get; set; }
        [ForeignKey("ReturnBusId")]
        public Bus? ReturnBus { get; set; }

        public string From { get; set; }
        public string To { get; set; }
        public DateTime JourneyDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public TimeSpan ArrivalTime { get; set; }
        public string BusType { get; set; }
        public string OperatorName { get; set; }
        public decimal Fare { get; set; }
        public int SeatsAvailable { get; set; }
        public string? FullRoute { get; set; }

        // Not mapped lists used for search UI (not stored)
        [NotMapped]
        public List<Bus>? AvailableBuses { get; set; }
        [NotMapped]
        public List<Bus>? ReturnBuses { get; set; }
    }
}
