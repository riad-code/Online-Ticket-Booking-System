using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Models
{
    using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
    using System.ComponentModel.DataAnnotations;

    public class Bus
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Bus Type is required.")]
        public string BusType { get; set; }

        [Required(ErrorMessage = "From location is required.")]
        public string From { get; set; }

        [Required(ErrorMessage = "To location is required.")]
        public string To { get; set; }

        [Required(ErrorMessage = "Departure Time is required.")]
        public TimeSpan DepartureTime { get; set; }

        [Required(ErrorMessage = "Arrival Time is required.")]
        public TimeSpan ArrivalTime { get; set; }

        [Required(ErrorMessage = "Operator Name is required.")]
        public string OperatorName { get; set; }

        [Required(ErrorMessage = "Fare is required.")]
        [Range(0, 10000, ErrorMessage = "Fare must be between 0 and 10,000.")]
        public decimal Fare { get; set; }

        [Required(ErrorMessage = "Seats Available is required.")]
        [Range(1, 100, ErrorMessage = "Seats Available must be between 1 and 100.")]
        public int SeatsAvailable { get; set; }

        [Required(ErrorMessage = "Full Route is required.")]
        public string FullRoute { get; set; }



        [NotMapped]
        [ValidateNever]
        public ICollection<BusSchedule> BusSchedules { get; set; }
    }

}
