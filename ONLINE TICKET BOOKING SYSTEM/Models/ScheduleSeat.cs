using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Models
{
    public enum SeatStatus { Available = 0, Booked = 1, Blocked = 2 }

    public class ScheduleSeat
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BusScheduleId { get; set; }

        [ForeignKey(nameof(BusScheduleId))]
        public BusSchedule BusSchedule { get; set; } = default!;

        [Required, MaxLength(10)]
        public string SeatNo { get; set; } = default!; 
        [Required]
        public SeatStatus Status { get; set; } = SeatStatus.Available;

        
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
