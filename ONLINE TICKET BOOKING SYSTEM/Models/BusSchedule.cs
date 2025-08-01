using System;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Models
{
    using ONLINE_TICKET_BOOKING_SYSTEM.Models;

    using System.ComponentModel.DataAnnotations.Schema;

    public class BusSchedule
    {
        public int Id { get; set; }
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

        [NotMapped]
        public List<Bus> AvailableBuses { get; set; }

        [NotMapped]
        public List<Bus> ReturnBuses { get; set; }
    }


}
