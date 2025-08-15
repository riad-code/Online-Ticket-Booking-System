using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Models
{
    public class Booking
    {
        public int Id { get; set; }

        public string? UserId { get; set; } 

        public int BusScheduleId { get; set; }
        [ForeignKey(nameof(BusScheduleId))]
        public BusSchedule BusSchedule { get; set; } = default!;

        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }

        public decimal TotalFare { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<BookingSeat> Seats { get; set; } = new List<BookingSeat>();
    }
}
