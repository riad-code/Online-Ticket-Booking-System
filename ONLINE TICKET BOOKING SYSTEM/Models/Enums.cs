namespace ONLINE_TICKET_BOOKING_SYSTEM.Models
{
    public enum BookingStatus
    {
        PendingPayment = 0,
        PendingApproval = 1,
        Approved = 2,
        CancelRequested = 3,   
        Cancelled = 4,
        Refunded = 5
    }

    public enum PaymentMethod
    {
        None = 0,
        BKash = 1,
        Nagad = 2,
        Rocket = 3,
        Card = 4
    }

    public enum PaymentStatus
    {
        Unpaid = 0,
        Paid = 1,
        Failed = 2,
        Refunded = 3
    }
}
