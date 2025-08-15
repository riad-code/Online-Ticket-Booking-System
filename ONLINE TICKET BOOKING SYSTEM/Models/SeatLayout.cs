using System;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Models
{
    public class SeatLayout
    {
        public int Id { get; set; }

       
        public int BusId { get; set; }

   
        public int TotalSeats { get; set; } = 40;

        
        public string? LayoutJson { get; set; }

      
        public string? BlockedSeatsCsv { get; set; }
    }
}
