using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using ONLINE_TICKET_BOOKING_SYSTEM.Services;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminTicketsController : Controller
    {
        private readonly ApplicationDbContext _ctx;
        private readonly ITicketPdfService _pdf;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailSender _email;

        public AdminTicketsController(ApplicationDbContext ctx, ITicketPdfService pdf, IWebHostEnvironment env, IEmailSender email)
        {
            _ctx = ctx; _pdf = pdf; _env = env; _email = email;
        }

        public async Task<IActionResult> Index()
     => View(await _ctx.Bookings
         .Include(b => b.BusSchedule)
         .Where(b => b.Status == BookingStatus.PendingApproval
                  || b.Status == BookingStatus.CancelRequested)   // ← include cancel requests
         .OrderByDescending(b => b.Id)
         .ToListAsync());


        public async Task<IActionResult> Details(int id)
        {
            var b = await _ctx.Bookings.Include(x => x.BusSchedule)
                .Include(x => x.Seats).ThenInclude(bs => bs.ScheduleSeat)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (b == null) return NotFound();
            return View(b);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var b = await _ctx.Bookings.Include(x => x.BusSchedule)
                .Include(x => x.Seats).ThenInclude(bs => bs.ScheduleSeat)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (b == null) return NotFound();
            if (b.PaymentStatus != PaymentStatus.Paid)
            {
                TempData["err"] = "Payment not completed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Generate PDF
            var pdfBytes = await _pdf.GenerateAsync(b);
            var dir = Path.Combine(_env.WebRootPath, "tickets");
            Directory.CreateDirectory(dir);
            var fileName = $"booking-{b.Id}.pdf";
            var fullPath = Path.Combine(dir, fileName);
            await System.IO.File.WriteAllBytesAsync(fullPath, pdfBytes);

            b.TicketPdfPath = "/tickets/" + fileName;
            b.Status = BookingStatus.Approved;
            await _ctx.SaveChangesAsync();

            // Email to user
            var userEmail = await _ctx.Users.Where(u => u.Id == b.UserId).Select(u => u.Email).FirstOrDefaultAsync();
            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                if (_email is EmailSender concrete)
                    await concrete.SendEmailWithAttachmentAsync(
                        userEmail!,
                        $"Your Ticket (#{b.Id})",
                        $"<p>Dear {b.CustomerName},</p><p>Your ticket is attached. You can also download it <a href=\"{b.TicketPdfPath}\">here</a>.</p>",
                        pdfBytes,
                        fileName);
                else
                    await _email.SendEmailAsync(userEmail!, $"Your Ticket (#{b.Id})", $"<p>Download: <a href=\"{b.TicketPdfPath}\">{b.TicketPdfPath}</a></p>");
            }

            TempData["ok"] = "Approved & ticket emailed.";
            return RedirectToAction(nameof(Details), new { id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveCancellation(int id,
    [FromServices] ONLINE_TICKET_BOOKING_SYSTEM.Data.ApplicationDbContext db,
    [FromServices] Microsoft.AspNetCore.Identity.UI.Services.IEmailSender emailSender,
    [FromServices] Microsoft.Extensions.Options.IOptions<ONLINE_TICKET_BOOKING_SYSTEM.Services.EmailSettings> opts)
        {
            var b = await db.Bookings
                .Include(x => x.BusSchedule)
                .Include(x => x.Seats).ThenInclude(s => s.ScheduleSeat)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (b == null)
            {
                TempData["err"] = "Booking not found.";
                return RedirectToAction("Index");
            }

            // Finalize cancel: change status and free seats
            b.Status = ONLINE_TICKET_BOOKING_SYSTEM.Models.BookingStatus.Cancelled;

            foreach (var bs in b.Seats)
            {
                var seat = bs?.ScheduleSeat;
                if (seat == null) continue;

                var piBooked = seat.GetType().GetProperty("IsBooked");
                if (piBooked != null && piBooked.PropertyType == typeof(bool) && piBooked.CanWrite)
                    piBooked.SetValue(seat, false);
                else
                {
                    var piAvail = seat.GetType().GetProperty("IsAvailable");
                    if (piAvail != null && piAvail.PropertyType == typeof(bool) && piAvail.CanWrite)
                        piAvail.SetValue(seat, true);
                }
            }

            await db.SaveChangesAsync();

            try
            {
                if (!string.IsNullOrWhiteSpace(b.CustomerEmail))
                    await emailSender.SendEmailAsync(b.CustomerEmail,
                        $"Cancellation approved for PNR #{b.Id}",
                        "Your booking has been cancelled. If a refund applies, it will be processed as per policy.");

                var adminEmail = opts.Value.AdminEmail;
                if (!string.IsNullOrWhiteSpace(adminEmail))
                    await emailSender.SendEmailAsync(adminEmail,
                        $"[Approved] Cancellation PNR #{b.Id}",
                        "The cancellation request has been approved.");
            }
            catch { }

            TempData["ok"] = "Cancellation approved.";
            return RedirectToAction("Details", new { id = b.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectCancellation(int id,
            [FromServices] ONLINE_TICKET_BOOKING_SYSTEM.Data.ApplicationDbContext db,
            [FromServices] Microsoft.AspNetCore.Identity.UI.Services.IEmailSender emailSender)
        {
            var b = await db.Bookings.FirstOrDefaultAsync(x => x.Id == id);
            if (b == null)
            {
                TempData["err"] = "Booking not found.";
                return RedirectToAction("Index");
            }

            // Send back to a normal approved/confirmed state (pick the right status for your app)
            b.Status = ONLINE_TICKET_BOOKING_SYSTEM.Models.BookingStatus.Approved;
            await db.SaveChangesAsync();

            try
            {
                if (!string.IsNullOrWhiteSpace(b.CustomerEmail))
                    await emailSender.SendEmailAsync(b.CustomerEmail,
                        $"Cancellation rejected for PNR #{b.Id}",
                        "Your cancellation request was rejected. Please contact support if you have questions.");
            }
            catch { }

            TempData["ok"] = "Cancellation request rejected.";
            return RedirectToAction("Details", new { id = b.Id });
        }


    }
}
