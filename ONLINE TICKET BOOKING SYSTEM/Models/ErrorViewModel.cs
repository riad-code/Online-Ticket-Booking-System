namespace ONLINE_TICKET_BOOKING_SYSTEM.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public string? ErrorMessage { get; set; }

        // read-only, computed
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    }
}
