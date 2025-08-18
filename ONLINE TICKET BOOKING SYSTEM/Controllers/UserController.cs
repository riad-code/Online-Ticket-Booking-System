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

           
            return View(recent);
        }
    }
}
