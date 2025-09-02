using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using ONLINE_TICKET_BOOKING_SYSTEM.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            var today = DateTime.Today;

            // =========================
            // BUS: counters
            // =========================
            ViewData["MyBookingsCount"] = await _context.Bookings
                .Where(b => b.UserId == userId)
                .CountAsync();

            ViewData["UpcomingTrips"] = await _context.Bookings
                .Include(b => b.BusSchedule)
                .Where(b => b.UserId == userId && b.BusSchedule.JourneyDate >= today)
                .CountAsync();

            ViewData["PendingPayments"] = await _context.Bookings
                .Where(b => b.UserId == userId && b.PaymentStatus == PaymentStatus.Unpaid)
                .CountAsync();

            ViewData["CancelledTrips"] = await _context.Bookings
                .Where(b => b.UserId == userId && b.Status == BookingStatus.Cancelled)
                .CountAsync();

            // =========================
            // BUS: recent list (model)
            // =========================
            var recent = await _context.Bookings
                .Include(b => b.BusSchedule).ThenInclude(s => s.Bus)
                .Include(b => b.Seats).ThenInclude(bs => bs.ScheduleSeat)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAtUtc)
                .ThenByDescending(b => b.Id)
                .Take(5)
                .Select(b => new BookingSummaryVm
                {
                    Id = b.Id,
                    Route = b.BusSchedule.FullRoute,
                    JourneyDate = b.BusSchedule.JourneyDate,
                    OperatorName = b.BusSchedule.OperatorName,
                    SeatsCsv = string.Join(", ", b.Seats.Select(s => s.ScheduleSeat.SeatNo)),
                    TotalFare = b.TotalFare,
                    Status = b.Status.ToString(),
                    PaymentStatus = b.PaymentStatus.ToString()
                })
                .ToListAsync();

            // =========================
            // AIR: counters + recent list
            // =========================
            var userEmail = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            var airQuery = _context.AirBookings
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.Airline)
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.FromAirport)
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.ToAirport)
                .AsQueryable();

            // Prefer UserId match; fallback to contact email if needed
            if (!string.IsNullOrWhiteSpace(userId))
            {
                airQuery = airQuery.Where(b => b.UserId == userId);
            }
            else if (!string.IsNullOrWhiteSpace(userEmail))
            {
                airQuery = airQuery.Where(b => b.ContactEmail == userEmail);
            }

            // KPI counters (AIR)
            ViewData["MyAirBookingsCount"] = await airQuery.CountAsync();

            ViewData["UpcomingAirTrips"] = await airQuery
                .Where(b => b.Itinerary.Segments
                    .Select(s => s.TravelDate)
                    .Any(d => d >= DateOnly.FromDateTime(today)))
                .CountAsync();

            ViewData["PendingAirPayments"] = await airQuery
                .Where(b => b.PaymentStatus == ONLINE_TICKET_BOOKING_SYSTEM.Models.Air.AirPaymentStatus.Unpaid)
                .CountAsync();

            ViewData["CancelledAirTrips"] = await airQuery
                .Where(b => b.BookingStatus == ONLINE_TICKET_BOOKING_SYSTEM.Models.Air.AirBookingStatus.Cancelled)
                .CountAsync();

            // Recent Air list for the dashboard (ViewBag)
            ViewBag.AirBookings = await airQuery
                .OrderByDescending(b => b.Id)
                .Select(b => new ONLINE_TICKET_BOOKING_SYSTEM.ViewModels.AirBookingSummaryVm
                {
                    Id = b.Id,
                    Pnr = b.Pnr,
                    Route =
                        (b.Itinerary.Segments.Select(s => s.FlightSchedule.FromAirport.IataCode).FirstOrDefault() ?? "-")
                        + " → " +
                        (b.Itinerary.Segments.Select(s => s.FlightSchedule.ToAirport.IataCode).FirstOrDefault() ?? "-"),
                    Airline = b.Itinerary.Segments.Select(s => s.FlightSchedule.Airline.Name).FirstOrDefault() ?? "-",
                    JourneyDate = b.Itinerary.Segments
                        .Select(s => s.TravelDate.ToDateTime(TimeOnly.MinValue))
                        .FirstOrDefault(),
                    Pax = b.Adults + b.Children + b.Infants,
                    TotalFare = b.AmountDue,
                    Status = b.BookingStatus.ToString(),
                    PaymentStatus = b.PaymentStatus.ToString()
                })
                .Take(5)
                .ToListAsync();

            // Final return (model = BUS recent list)
            return View(recent);
        }


    }
}
