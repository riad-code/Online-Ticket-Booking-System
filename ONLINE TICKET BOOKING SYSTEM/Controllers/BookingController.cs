using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using ONLINE_TICKET_BOOKING_SYSTEM.Services;
using System.Text.RegularExpressions;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
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

        private static HashSet<string> ParseBlockedCsv(string? csv)
        {
            var parts = Regex.Split(csv ?? "", @"[,\|؛،\s]+")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToUpperInvariant());
            return new HashSet<string>(parts, StringComparer.OrdinalIgnoreCase);
        }

        [HttpGet]
        public async Task<IActionResult> Seats(int scheduleId)
        {
            if (scheduleId <= 0) return BadRequest("Invalid schedule id.");

            var schedule = await _context.BusSchedules
                .AsNoTracking()
                .Include(s => s.Bus)
                .FirstOrDefaultAsync(s => s.Id == scheduleId);

            if (schedule == null) return NotFound("Schedule not found.");

            // Seed schedule seats if not exists
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

            // Apply blocked seats from SeatLayout
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

            // Prefill user info
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

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(
            int scheduleId,
            string customerName,
            string customerPhone,
            string? customerEmail,
            [FromForm] string seatNos)
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

            // --- Fare & totals (keep consistent with PaymentController) ---
            var discountedFare = Math.Max(0, schedule.Fare - 30);
            var totalFare = discountedFare * seats.Count;

            // Add your fixed/optional charges/discounts here if needed
            var processing = 30m;
            var insurance = 0m;      // set later if user chooses
            var discount = 0m;

            var grandTotal = totalFare + processing + insurance - discount;
            if (grandTotal <= 0)
            {
                TempData["err"] = "Grand total must be greater than zero.";
                return RedirectToAction(nameof(Seats), new { scheduleId });
            }

            using var tx = await _context.Database.BeginTransactionAsync();

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
                CustomerEmail = string.IsNullOrWhiteSpace(customerEmail)
                                    ? await _context.Users.Where(u => u.Id == _userManager.GetUserId(User))
                                                          .Select(u => u.Email)
                                                          .FirstOrDefaultAsync()
                                    : customerEmail,
                Gender = genderFromUser,

                TotalFare = totalFare,
                ProcessingFee = processing,
                InsuranceFee = insurance,
                Discount = discount,
                GrandTotal = grandTotal,

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

            // Mark schedule seats as booked
            seats.ForEach(s => s.Status = SeatStatus.Booked);
            _context.ScheduleSeats.UpdateRange(seats);

            // Update available count
            schedule.SeatsAvailable = Math.Max(0, schedule.SeatsAvailable - seats.Count);
            _context.BusSchedules.Update(schedule);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            // ✅ STEP 1: Redirect to Details (not Payment)
            return RedirectToAction(nameof(Details), new { id = booking.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.BusSchedule)
                .Include(b => b.Seats).ThenInclude(bs => bs.ScheduleSeat)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            ViewBag.UserEmail = await _context.Users
                .Where(u => u.Id == booking.UserId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            // From this view, your existing "Proceed to Payment" button should link to:
            // /Payment/Payment?id=@Model.Id
            return View(booking);
        }

        // Payment actions are in PaymentController.

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ThankYou(int id)
        {
            var b = await _context.Bookings
                .Include(x => x.BusSchedule)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (b == null) return NotFound();
            return View(b);
        }

        // Send E-Ticket to Customer
        [HttpPost]
        public async Task<IActionResult> SendTicket(int bookingId, string userEmail)
        {
            var booking = await _context.Bookings
                .Include(b => b.BusSchedule)
                .Include(b => b.Seats).ThenInclude(bs => bs.ScheduleSeat)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound("Booking not found.");

            // Use booking.CustomerEmail if userEmail is not provided
            userEmail = string.IsNullOrWhiteSpace(userEmail) ? booking.CustomerEmail : userEmail;
            if (string.IsNullOrWhiteSpace(userEmail))
                return BadRequest("No email address provided for the ticket.");

            byte[] pdfBytes = await _ticketPdf.GenerateAsync(booking);

            try
            {
                if (_emailSender is EmailSender concrete)
                {
                    await concrete.SendEmailWithAttachmentAsync(
                        userEmail!,
                        $"Your E-Ticket (#{booking.Id})",
                        $"<h3>Hello {booking.CustomerName},</h3><p>Your ticket is attached as PDF.</p>",
                        pdfBytes ?? Array.Empty<byte>(),
                        $"Ticket_{booking.Id}.pdf"
                    );
                }
                else
                {
                    await _emailSender.SendEmailAsync(
                        userEmail!,
                        $"Your E-Ticket (#{booking.Id})",
                        "Your ticket is confirmed. Please login to download the PDF."
                    );
                }

                return Ok("Ticket email sent.");
            }
            catch (Exception ex)
            {
                return BadRequest("Error sending ticket email: " + ex.Message);
            }
        }

        [Authorize(Roles = "User")]
        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);

            var bookings = await _context.Bookings
                .Where(b => b.UserId == userId)
                .Include(b => b.BusSchedule)
                .Include(b => b.Seats).ThenInclude(bs => bs.ScheduleSeat)
                .OrderByDescending(b => b.Id)
                .ToListAsync();

            return View(bookings);
        }
    }
}
