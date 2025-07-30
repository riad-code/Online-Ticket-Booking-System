using System;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Models
{
    public class BusSchedule
    {
        public int Id { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public DateTime JourneyDate { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public TimeSpan ArrivalTime { get; set; }
        public string BusType { get; set; }
        public string OperatorName { get; set; }
        public decimal Fare { get; set; }
        public int SeatsAvailable { get; set; }
    }
}
