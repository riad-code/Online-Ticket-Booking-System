using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.AspNetCore.Http;
namespace ONLINE_TICKET_BOOKING_SYSTEM.Models.Event
{
    [Table("EventItems")]
    public class EventItem
    {
        [Key] public int Id { get; set; }

        [Required, MaxLength(160)]
        public string Title { get; set; } = default!;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(64)]
        public string? Category { get; set; }

        [MaxLength(96)]
        public string? City { get; set; }

        [MaxLength(160)]
        public string? Venue { get; set; }

        [MaxLength(1024)]
        public string? CoverImageUrl { get; set; }
        [NotMapped]                                 
        public IFormFile? CoverImageFile { get; set; }

        // store as UTC
        public DateTime? StartDateUtc { get; set; }
        public DateTime? EndDateUtc { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PriceFrom { get; set; }

        [MaxLength(8)]
        public string? Currency { get; set; } = "৳";

        public int? AvailableTickets { get; set; }

        // ✅ non-nullable to avoid checkbox error
        public bool IsFeatured { get; set; } = false;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
