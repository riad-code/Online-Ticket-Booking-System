using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Models.Park
{
    public class ParkItem
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = "";

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [StringLength(120)]
        public string? OpeningHours { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? PriceFrom { get; set; }

        [StringLength(16)]
        public string? Currency { get; set; }

        public int? AvailableTickets { get; set; }

        public bool IsFeatured { get; set; }

        [StringLength(500)]
        public string? CoverImageUrl { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        // For file upload
        [NotMapped]
        public IFormFile? CoverImageFile { get; set; }

    }
}
