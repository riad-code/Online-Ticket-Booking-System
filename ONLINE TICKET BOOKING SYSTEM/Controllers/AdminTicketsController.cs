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
            => View(await _ctx.Bookings.Include(b => b.BusSchedule)
                .Where(b => b.Status == BookingStatus.PendingApproval)
                .OrderByDescending(b => b.Id).ToListAsync());

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

    }
}
