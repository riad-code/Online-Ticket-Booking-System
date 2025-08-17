using System.Threading.Tasks;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;

public interface ITicketPdfService
{
    Task<byte[]> GenerateAsync(Booking booking);
}
