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
using System.Text.RegularExpressions;

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

        // ========= helpers =========

        // Split CSV safely: comma, pipe, arabic/bengali commas, or whitespace. Case/space tolerant.
        private static HashSet<string> ParseBlockedCsv(string? csv)
        {
            var parts = Regex.Split(csv ?? "", @"[,\|؛،\s]+")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToUpperInvariant());
            return new HashSet<string>(parts, StringComparer.OrdinalIgnoreCase);
        }

        // ========= actions =========

        // GET: /Booking/Seats?scheduleId=123
        // GET: /Booking/Seats?scheduleId=123
        [HttpGet]
        public async Task<IActionResult> Seats(int scheduleId)
        {
            if (scheduleId <= 0) return BadRequest("Invalid schedule id.");

            var schedule = await _context.BusSchedules
                .AsNoTracking()
                .Include(s => s.Bus)
                .FirstOrDefaultAsync(s => s.Id == scheduleId);

            if (schedule == null) return NotFound("Schedule not found.");

            // Ensure seats exist (safety)
            if (!await _context.ScheduleSeats.AnyAsync(x => x.BusScheduleId == scheduleId))
            {
                var cols = new[] { "A", "B", "C", "D" };
                var names = new List<string>();
                for (int r = 1; r <= 10; r++)
                    foreach (var c in cols) names.Add($"{c}{r}");

                var seatsSeed = names.Select(n => new ScheduleSeat
                {
                    BusScheduleId = scheduleId,
                    SeatNo = n,
                    Status = SeatStatus.Available
                }).ToList();

                _context.ScheduleSeats.AddRange(seatsSeed);
                await _context.SaveChangesAsync();
            }

            // ✅ Apply Admin blocked seats to this schedule (does not overwrite Booked)
            var layout = await _context.SeatLayouts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BusId == schedule.BusId);

            if (layout != null && !string.IsNullOrWhiteSpace(layout.BlockedSeatsCsv))
            {
                var blocked = layout.BlockedSeatsCsv
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => s.ToUpperInvariant())
                    .ToHashSet();

                var toBlock = await _context.ScheduleSeats
                    .Where(s => s.BusScheduleId == scheduleId &&
                                blocked.Contains(s.SeatNo.ToUpper()) &&
                                s.Status == SeatStatus.Available) // don't override booked
                    .ToListAsync();

                if (toBlock.Count > 0)
                {
                    toBlock.ForEach(s => s.Status = SeatStatus.Blocked);
                    _context.ScheduleSeats.UpdateRange(toBlock);
                    await _context.SaveChangesAsync();
                }
            }

            var seatList = await _context.ScheduleSeats
                .AsNoTracking()
                .Where(s => s.BusScheduleId == scheduleId)
                .OrderBy(s => s.SeatNo)
                .ToListAsync();

            // ✅ NEW: prefill passenger from profile (if signed in)
            if (User?.Identity?.IsAuthenticated == true)
            {
                var usr = await _userManager.GetUserAsync(User);
                ViewBag.PassengerName = (usr?.FullName ?? usr?.UserName) ?? "";
                ViewBag.PassengerPhone = usr?.PhoneNumber ?? "";
                ViewBag.PassengerEmail = usr?.Email ?? "";
            }

            ViewBag.Schedule = schedule;
            return View("SelectSeats", seatList);
        }


        // POST: /Booking/Confirm
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int scheduleId, string customerName, string customerPhone, [FromForm] string seatNos)
        {
            if (string.IsNullOrWhiteSpace(seatNos))
            {
                TempData["err"] = "Please select at least one seat.";
                return RedirectToAction(nameof(Seats), new { scheduleId });
            }

            var requested = seatNos.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                   .Select(s => s.ToUpperInvariant())
                                   .ToArray();

            // Re-fetch schedule for fare
            var schedule = await _context.BusSchedules.FirstOrDefaultAsync(s => s.Id == scheduleId);
            if (schedule == null)
            {
                TempData["err"] = "Schedule not found.";
                return RedirectToAction(nameof(Seats), new { scheduleId });
            }

            // Seats must be available (not blocked/booked)
            var seats = await _context.ScheduleSeats
                .Where(s => s.BusScheduleId == scheduleId
                            && requested.Contains(s.SeatNo.ToUpper())
                            && s.Status == SeatStatus.Available)
                .ToListAsync();

            if (seats.Count != requested.Length)
            {
                TempData["err"] = "One or more selected seats are no longer available.";
                return RedirectToAction(nameof(Seats), new { scheduleId });
            }

            // Use the same discount shown in the SelectSeats view
            var discountedFare = Math.Max(0, schedule.Fare - 30);

            using var tx = await _context.Database.BeginTransactionAsync();

            // Create booking
            var booking = new Booking
            {
                UserId = _userManager.GetUserId(User),
                BusScheduleId = scheduleId,
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                TotalFare = discountedFare * seats.Count
            };
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Link seats to booking with per-seat fare
            var links = seats.Select(s => new BookingSeat
            {
                BookingId = booking.Id,
                ScheduleSeatId = s.Id,
                Fare = discountedFare
            });
            _context.BookingSeats.AddRange(links);

            // Mark seats as booked
            seats.ForEach(s => s.Status = SeatStatus.Booked);
            _context.ScheduleSeats.UpdateRange(seats);

            // Update remaining availability on the schedule
            schedule.SeatsAvailable = Math.Max(0, schedule.SeatsAvailable - seats.Count);
            _context.BusSchedules.Update(schedule);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            // You can redirect to a payment page here if needed
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
