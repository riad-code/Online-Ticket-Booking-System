using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using ONLINE_TICKET_BOOKING_SYSTEM.Services; // EmailSender + ITicketPdfService
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

        // Email + PDF services
        private readonly IEmailSender _emailSender;
        private readonly ITicketPdfService _ticketPdf;

        public BookingController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ITicketPdfService ticketPdf)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _ticketPdf = ticketPdf;
        }

        // ===== helper (optional) =====
        // Split CSV safely: comma, pipe, arabic/bengali commas, or whitespace. Case/space tolerant.
        private static HashSet<string> ParseBlockedCsv(string? csv)
        {
            var parts = Regex.Split(csv ?? "", @"[,\|؛،\s]+")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToUpperInvariant());
            return new HashSet<string>(parts, StringComparer.OrdinalIgnoreCase);
        }

        // ===== SEATS =====
        [HttpGet]
        public async Task<IActionResult> Seats(int scheduleId)
        {
            if (scheduleId <= 0) return BadRequest("Invalid schedule id.");

            var schedule = await _context.BusSchedules
                .AsNoTracking()
                .Include(s => s.Bus)
                .FirstOrDefaultAsync(s => s.Id == scheduleId);

            if (schedule == null) return NotFound("Schedule not found.");

            // Seed seats if missing
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

            // Apply admin blocked seats (do not override Booked)
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
                                s.Status == SeatStatus.Available)
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

            // Prefill passenger from profile (if signed in)
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

        // ===== CONFIRM → redirect to PAYMENT =====
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

            var schedule = await _context.BusSchedules.FirstOrDefaultAsync(s => s.Id == scheduleId);
            if (schedule == null)
            {
                TempData["err"] = "Schedule not found.";
                return RedirectToAction(nameof(Seats), new { scheduleId });
            }

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

            var discountedFare = Math.Max(0, schedule.Fare - 30);

            using var tx = await _context.Database.BeginTransactionAsync();

            // Gender from ApplicationUser
            string? genderFromUser = null;
            if (User?.Identity?.IsAuthenticated == true)
            {
                var usr = await _userManager.GetUserAsync(User);
                genderFromUser = usr?.Gender;
            }

            var booking = new Booking
            {
                UserId = _userManager.GetUserId(User),
                BusScheduleId = scheduleId,
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                Gender = genderFromUser,
                TotalFare = discountedFare * seats.Count,
                Status = BookingStatus.PendingPayment,
                PaymentStatus = PaymentStatus.Unpaid
            };
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Link seats
            var links = seats.Select(s => new BookingSeat
            {
                BookingId = booking.Id,
                ScheduleSeatId = s.Id,
                Fare = discountedFare
            });
            _context.BookingSeats.AddRange(links);

            // Mark seats booked now so others see Booked
            seats.ForEach(s => s.Status = SeatStatus.Booked);
            _context.ScheduleSeats.UpdateRange(seats);

            schedule.SeatsAvailable = Math.Max(0, schedule.SeatsAvailable - seats.Count);
            _context.BusSchedules.Update(schedule);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            // Go to Payment
            return RedirectToAction(nameof(Details), new { id = booking.Id });
        }
        // ===== DETAILS =====
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
        // ===== PAYMENT (GET) =====
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Payment(int id, string? coupon = null)
        {
            var b = await _context.Bookings
                .Include(x => x.BusSchedule)
                .Include(x => x.Seats).ThenInclude(bs => bs.ScheduleSeat)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (b == null) return NotFound();

            // Preview totals (not persisted on GET)
            b.CouponCode = coupon;
            b.ProcessingFee = 30;
            b.InsuranceFee = 10;
            b.Discount = (!string.IsNullOrWhiteSpace(coupon) && coupon.Trim().ToUpper() == "SAVE30") ? 30 : 0;
            b.GrandTotal = b.TotalFare + b.ProcessingFee + b.InsuranceFee - b.Discount;

            return View(b); // Views/Booking/Payment.cshtml
        }
       
        // ===== PAYMENT (POST) =====
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Payment(int id, PaymentMethod method, bool withInsurance, string? couponCode)
        {
            var b = await _context.Bookings
                .Include(x => x.BusSchedule)
                .Include(x => x.Seats).ThenInclude(bs => bs.ScheduleSeat)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (b == null) return NotFound();

            b.ProcessingFee = 30;
            b.InsuranceFee = withInsurance ? 10 : 0;
            b.Discount = (!string.IsNullOrWhiteSpace(couponCode) && couponCode.Trim().ToUpper() == "SAVE30") ? 30 : 0;
            b.CouponCode = couponCode;
            b.PaymentMethod = method;
            b.GrandTotal = b.TotalFare + b.ProcessingFee + b.InsuranceFee - b.Discount;

            // TODO: Real gateway integration here
            b.PaymentStatus = PaymentStatus.Paid;
            b.Status = BookingStatus.PendingApproval;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ThankYou), new { id = b.Id });
        }

        // ===== THANK YOU =====
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ThankYou(int id)
        {
            var b = await _context.Bookings.Include(x => x.BusSchedule).FirstOrDefaultAsync(x => x.Id == id);
            if (b == null) return NotFound();
            return View(b); // Views/Booking/ThankYou.cshtml
        }

       

        // ===== EMAIL TICKET SENDER (after admin approval or payment success if instant) =====
        [HttpPost]
        public async Task<IActionResult> SendTicket(int bookingId, string userEmail)
        {
            var booking = await _context.Bookings
                .Include(b => b.BusSchedule)
                .Include(b => b.Seats)
                    .ThenInclude(bs => bs.ScheduleSeat)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound("Booking not found.");

            // Generate the PDF via ticket service
            byte[] pdfBytes = await _ticketPdf.GenerateAsync(booking);

            // If EmailSender concrete is available, use attachment API
            if (_emailSender is EmailSender concrete)
            {
                await concrete.SendEmailWithAttachmentAsync(
                    userEmail,
                    $"Your E-Ticket (#{booking.Id})",
                    $"<h3>Hello {booking.CustomerName},</h3><p>Your ticket is attached as PDF.</p>",
                    pdfBytes ?? Array.Empty<byte>(),
                    $"Ticket_{booking.Id}.pdf"
                );
            }
            else
            {
                // Fallback: no attachment API
                await _emailSender.SendEmailAsync(
                    userEmail,
                    $"Your E-Ticket (#{booking.Id})",
                    "Your ticket is confirmed. Please login to download the PDF."
                );
            }

            return Ok("Ticket email sent.");
        }
        [Authorize(Roles = "User")]
        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);

            var bookings = await _context.Bookings
                .Where(b => b.UserId == userId)
                .Include(b => b.BusSchedule)
                .Include(b => b.Seats)
                    .ThenInclude(bs => bs.ScheduleSeat)
                .OrderByDescending(b => b.Id)
                .ToListAsync();

            return View(bookings); // Views/Booking/MyBookings.cshtml
        }
    }
}
