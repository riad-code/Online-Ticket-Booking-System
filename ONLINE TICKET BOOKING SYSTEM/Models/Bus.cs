using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Models
{
    public class Bus
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Bus Type is required.")]
        public string BusType { get; set; } = string.Empty;

        [Required(ErrorMessage = "From location is required.")]
        public string From { get; set; } = string.Empty;

        [Required(ErrorMessage = "To location is required.")]
        public string To { get; set; } = string.Empty;

        [Required(ErrorMessage = "Departure Time is required.")]
        public TimeSpan DepartureTime { get; set; }

        [Required(ErrorMessage = "Arrival Time is required.")]
        public TimeSpan ArrivalTime { get; set; }

        [Required(ErrorMessage = "Operator Name is required.")]
        public string OperatorName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Fare is required.")]
        [Range(0, 10000, ErrorMessage = "Fare must be between 0 and 10,000.")]
        public decimal Fare { get; set; }

        [Required(ErrorMessage = "Seats Available is required.")]
        [Range(1, 100, ErrorMessage = "Seats Available must be between 1 and 100.")]
        public int SeatsAvailable { get; set; }

        [Required(ErrorMessage = "Full Route is required.")]
        public string FullRoute { get; set; } = string.Empty;

        // DB columns (nullable)
        public string? BoardingPointsString { get; set; }
        public string? DroppingPointsString { get; set; }

        // Convenience for UI filtering (NotMapped)
        [NotMapped]
        public List<string>? BoardingPoints =>
            string.IsNullOrWhiteSpace(BoardingPointsString) ? null
            : BoardingPointsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

        [NotMapped]
        public List<string>? DroppingPoints =>
            string.IsNullOrWhiteSpace(DroppingPointsString) ? null
            : DroppingPointsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

        [NotMapped]
        [ValidateNever]
        public ICollection<BusSchedule> BusSchedules { get; set; } = new List<BusSchedule>();
    }
}
