namespace ONLINE_TICKET_BOOKING_SYSTEM.ViewModels
{
    public class BookingSummaryVm
    {
        public int Id { get; set; }
        public string Route { get; set; } = "";
        public DateTime JourneyDate { get; set; }
        public string OperatorName { get; set; } = "";
        public string SeatsCsv { get; set; } = "";
        public decimal TotalFare { get; set; }
        public string Status { get; set; } = "";
        public string PaymentStatus { get; set; } = "";
    }
}
