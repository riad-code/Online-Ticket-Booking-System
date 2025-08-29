using System.Threading.Tasks;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using ONLINE_TICKET_BOOKING_SYSTEM.Models.Air;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Services
{
    public interface ITicketPdfService
    {
        
        Task<byte[]> GenerateAsync(Booking booking);

        // Air ticket
        Task<byte[]> GenerateAirTicketAsync(AirBooking booking);
    }
}
