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

        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public DateTime JourneyDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public TimeSpan ArrivalTime { get; set; }
        public string BusType { get; set; } = string.Empty;
        public string OperatorName { get; set; } = string.Empty;
        public decimal Fare { get; set; }
        public int SeatsAvailable { get; set; }
        public string? FullRoute { get; set; }

   
        public string? BoardingPointsString { get; set; }
        public string? DroppingPointsString { get; set; }
        public bool IsBlocked { get; set; } = false;
 
        [NotMapped]
        public List<string>? BoardingPoints =>
            string.IsNullOrWhiteSpace(BoardingPointsString) ? null
            : BoardingPointsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

        [NotMapped]
        public List<string>? DroppingPoints =>
            string.IsNullOrWhiteSpace(DroppingPointsString) ? null
            : DroppingPointsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

        // For view-model transport only
        [NotMapped]
        public List<Bus>? AvailableBuses { get; set; }

        [NotMapped]
        public List<Bus>? ReturnBuses { get; set; }
    }
}
