using System.ComponentModel.DataAnnotations.Schema;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Models
{
    public class BookingSeat
    {
        public int Id { get; set; }

        public int BookingId { get; set; }
        [ForeignKey(nameof(BookingId))]
        public Booking Booking { get; set; } = default!;

        public int ScheduleSeatId { get; set; }
        [ForeignKey(nameof(ScheduleSeatId))]
        public ScheduleSeat ScheduleSeat { get; set; } = default!;

        public decimal Fare { get; set; }
    }
}
