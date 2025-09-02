using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using ONLINE_TICKET_BOOKING_SYSTEM.Models.Air;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Services
{
    public interface IAirBookingService
    {
        Task<AirBooking> CreateHoldAsync(Itinerary itinerary, int adults, int children, int infants);
        Task<AirBooking> AttachPassengersAsync(string pnr, IEnumerable<Passenger> pax);
        Task<AirBooking> MarkPaidAsync(string pnr, decimal amountPaid);
    }

    public class AirBookingService : IAirBookingService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPnrService _pnr;
        private readonly IPricingService _pricing;

        public AirBookingService(ApplicationDbContext db, IPnrService pnr, IPricingService pricing)
        { _db = db; _pnr = pnr; _pricing = pricing; }

        public async Task<AirBooking> CreateHoldAsync(Itinerary itin, int adults, int children, int infants)
        {
            // persist itinerary + segments
            _db.Itineraries.Add(itin);
            await _db.SaveChangesAsync();

            var priced = _pricing.Price(itin, adults, children, infants);

            var booking = new AirBooking
            {
                Pnr = _pnr.GeneratePnr(),
                ItineraryId = itin.Id,
                Adults = adults,
                Children = children,
                Infants = infants,
                AmountDue = priced.grand,
                AmountPaid = 0,
                BookingStatus = AirBookingStatus.PendingPayment, // <-- updated
                Currency = itin.Currency,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.AirBookings.Add(booking);
            await _db.SaveChangesAsync();
            return booking;
        }

        public async Task<AirBooking> AttachPassengersAsync(string pnr, IEnumerable<Passenger> pax)
        {
            var booking = await _db.AirBookings.Include(b => b.Passengers)
                                               .SingleAsync(b => b.Pnr == pnr);
            booking.Passengers.AddRange(pax);
            await _db.SaveChangesAsync();
            return booking;
        }

        public async Task<AirBooking> MarkPaidAsync(string pnr, decimal amountPaid)
        {
            var booking = await _db.AirBookings.SingleAsync(b => b.Pnr == pnr);
            booking.AmountPaid += amountPaid;
            if (booking.AmountPaid >= booking.AmountDue)
            {
                booking.BookingStatus = AirBookingStatus.Approved; // <-- updated
                booking.PaidAtUtc = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();
            return booking;
        }
    }
}
