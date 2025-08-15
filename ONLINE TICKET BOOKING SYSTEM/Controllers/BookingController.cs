using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Booking/Seats?scheduleId=123
        [HttpGet]
        public async Task<IActionResult> Seats(int scheduleId)
        {
            var schedule = await _context.BusSchedules.Include(s => s.Bus).FirstOrDefaultAsync(s => s.Id == scheduleId);
            if (schedule == null) return NotFound();

            // নিশ্চিত করা: seats seeded আছে
            if (!await _context.ScheduleSeats.AnyAsync(x => x.BusScheduleId == scheduleId))
            {
                // ফfallback auto-seed (যদি Step 4 মিস হয়ে যায়)
                var cols = new[] { "A", "B", "C", "D" };
                var seats = new List<ScheduleSeat>();
                for (int r = 1; r <= 10; r++)
                    foreach (var c in cols)
                        seats.Add(new ScheduleSeat { BusScheduleId = scheduleId, SeatNo = $"{c}{r}", Status = SeatStatus.Available });
                _context.ScheduleSeats.AddRange(seats);
                schedule.SeatsAvailable = seats.Count;
                _context.BusSchedules.Update(schedule);
                await _context.SaveChangesAsync();
            }

            var allSeats = await _context.ScheduleSeats
                .Where(s => s.BusScheduleId == scheduleId)
                .OrderBy(s => s.SeatNo)
                .ToListAsync();

            ViewBag.Schedule = schedule;
            return View(allSeats);
        }

        // POST: /Booking/Confirm
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int scheduleId, string customerName, string customerPhone, [FromForm] string[] seatNos)
        {
            if (seatNos == null || seatNos.Length == 0)
            {
                TempData["err"] = "Please select at least one seat.";
                return RedirectToAction(nameof(Seats), new { scheduleId });
            }

            using var tx = await _context.Database.BeginTransactionAsync();

            var schedule = await _context.BusSchedules.FirstOrDefaultAsync(s => s.Id == scheduleId);
            if (schedule == null) return NotFound();

            var seats = await _context.ScheduleSeats
                .Where(s => s.BusScheduleId == scheduleId && seatNos.Contains(s.SeatNo))
                .ToListAsync();

            // availability re-check
            if (seats.Count != seatNos.Length || seats.Any(s => s.Status != SeatStatus.Available))
            {
                TempData["err"] = "One or more selected seats are no longer available.";
                return RedirectToAction(nameof(Seats), new { scheduleId });
            }

            // mark booked
            seats.ForEach(s => s.Status = SeatStatus.Booked);
            _context.ScheduleSeats.UpdateRange(seats);

            var booking = new Booking
            {
                UserId = _userManager.GetUserId(User),
                BusScheduleId = scheduleId,
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                TotalFare = schedule.Fare * seats.Count
            };
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            var links = seats.Select(s => new BookingSeat
            {
                BookingId = booking.Id,
                ScheduleSeatId = s.Id,
                Fare = schedule.Fare
            });
            _context.BookingSeats.AddRange(links);

            // defensive: SeatsAvailable
            schedule.SeatsAvailable = Math.Max(0, schedule.SeatsAvailable - seats.Count);
            _context.BusSchedules.Update(schedule);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return RedirectToAction(nameof(Details), new { id = booking.Id });
        }

        // GET: /Booking/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.BusSchedule)
                .Include(b => b.Seats)
                    .ThenInclude(bs => bs.ScheduleSeat)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();
            return View(booking);
        }
    }
}
