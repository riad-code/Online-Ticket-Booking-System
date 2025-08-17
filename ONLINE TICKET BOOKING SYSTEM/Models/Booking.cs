using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;


namespace ONLINE_TICKET_BOOKING_SYSTEM.Models
{
    [Index(nameof(PaymentStatus))]
    [Index(nameof(Status))]
    [Index(nameof(PaymentAt))]
    [Index(nameof(CreatedAtUtc))]
    public class Booking
    {
        public int Id { get; set; }
        public string? UserId { get; set; }

        public int BusScheduleId { get; set; }
        [ForeignKey(nameof(BusScheduleId))]
        public BusSchedule BusSchedule { get; set; } = default!;

        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? Gender { get; set; }

        public decimal TotalFare { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow; // keep only one created-at

        public ICollection<BookingSeat> Seats { get; set; } = new List<BookingSeat>();

        public BookingStatus Status { get; set; } = BookingStatus.PendingPayment;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.None;

        public DateTime? PaymentAt { get; set; } // set when payment succeeds

        public string? CouponCode { get; set; }
        public decimal ProcessingFee { get; set; }
        public decimal InsuranceFee { get; set; }
        public decimal Discount { get; set; }
        public decimal GrandTotal { get; set; }

        public string? TicketPdfPath { get; set; }

    }
}
