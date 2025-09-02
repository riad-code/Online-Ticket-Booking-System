using System;

namespace ONLINE_TICKET_BOOKING_SYSTEM.ViewModels
{
    public class AirBookingSummaryVm
    {
        public int Id { get; set; }                // Internal numeric Id (if you want)
        public string Pnr { get; set; } = "";      // PNR shown in table
        public string Route { get; set; } = "";    // e.g. DAC → CGP
        public string Airline { get; set; } = "";  // Airline name
        public DateTime JourneyDate { get; set; }  // First segment date
        public int Pax { get; set; }               // Passenger count
        public decimal TotalFare { get; set; }     // AmountDue
        public string Status { get; set; } = "";   // BookingStatus
        public string PaymentStatus { get; set; } = ""; // AirPaymentStatus
    }
}