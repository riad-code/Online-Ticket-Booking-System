namespace ONLINE_TICKET_BOOKING_SYSTEM.Models
{
    public class ContactMessage
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        
        public bool IsRead { get; set; } = false;
    }
}
