using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ONLINE_TICKET_BOOKING_SYSTEM.Models.Air;

namespace ONLINE_TICKET_BOOKING_SYSTEM.ViewModels
{
    public class PaxRow
    {
        [Required] public string FirstName { get; set; } = default!;
        [Required] public string LastName { get; set; } = default!;
        [Required] public string Dob { get; set; } = default!; // yyyy-MM-dd
        [Required] public PaxType Type { get; set; }
        public string? Gender { get; set; }
        public string? PassportNo { get; set; }
    }

    public class AirPassengersFormVm
    {
        public string Pnr { get; set; } = default!;
        public string From { get; set; } = default!;
        public string To { get; set; } = default!;
        public string TravelDate { get; set; } = default!;
        public string Flight { get; set; } = default!;
        public int Adults { get; set; }
        public int Children { get; set; }
        public int Infants { get; set; }
        public decimal AmountDue { get; set; }
        public string Currency { get; set; } = "BDT";
        public List<PaxRow> Passengers { get; set; } = new();
    }
}
