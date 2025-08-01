using System;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Models
{
    public class Bus
    {
        public string BusType { get; set; }  // ✅ Required
        public string From { get; set; }
        public string To { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public TimeSpan ArrivalTime { get; set; }
        public string OperatorName { get; set; }
        public decimal Fare { get; set; }
        public int SeatsAvailable { get; set; }
        public string FullRoute { get; set; }
        public ICollection<BusSchedule> BusSchedules { get; set; }
        // Add other properties if needed
    }

}
