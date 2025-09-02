// Controllers/AdminAirTicketsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models.Air;
using ONLINE_TICKET_BOOKING_SYSTEM.Services;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminAirTicketsController : Controller
    {
        private readonly ApplicationDbContext _ctx;
        private readonly ITicketPdfService _pdf;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailSender _email;

        public AdminAirTicketsController(ApplicationDbContext ctx, ITicketPdfService pdf, IWebHostEnvironment env, IEmailSender email)
        { _ctx = ctx; _pdf = pdf; _env = env; _email = email; }

        // List only those waiting for approval
        public async Task<IActionResult> Index()
        {
            var rows = await _ctx.AirBookings
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.Airline)
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.FromAirport)
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.ToAirport)
                .Where(b => b.BookingStatus == AirBookingStatus.PendingApproval)
                .OrderByDescending(b => b.Id)
                .ToListAsync();

            return View(rows);
        }

        public async Task<IActionResult> Details(int id)
        {
            var b = await _ctx.AirBookings
                .Include(x => x.Passengers)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.Airline)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.FromAirport)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.ToAirport)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (b == null) return NotFound();
            return View(b);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var b = await _ctx.AirBookings
                .Include(x => x.Passengers)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.Airline)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.FromAirport)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.ToAirport)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (b == null) return NotFound();
            if (b.PaymentStatus != AirPaymentStatus.Paid)
            {
                TempData["err"] = "Payment not completed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // PDF
            var pdfBytes = await _pdf.GenerateAirTicketAsync(b);
            var dir = Path.Combine(_env.WebRootPath, "airtickets");
            Directory.CreateDirectory(dir);
            var fileName = $"air-{b.Pnr}.pdf";
            var fullPath = Path.Combine(dir, fileName);
            await System.IO.File.WriteAllBytesAsync(fullPath, pdfBytes);

            b.TicketPdfPath = "/airtickets/" + fileName;
            b.BookingStatus = AirBookingStatus.Approved;
            await _ctx.SaveChangesAsync();

            // Email user (ContactEmail preferred)
            var to = !string.IsNullOrWhiteSpace(b.ContactEmail)
                        ? b.ContactEmail
                        : await _ctx.Users.Where(u => u.Id == b.UserId).Select(u => u.Email).FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(to))
            {
                if (_email is EmailSender concrete)
                {
                    await concrete.SendEmailWithAttachmentAsync(
                        to!,
                        $"Your Air Ticket – PNR {b.Pnr}",
                        $"<p>Dear {(b.ContactName ?? "Customer")},</p><p>Your e-ticket is attached. You can also download it <a href=\"{b.TicketPdfPath}\">here</a>.</p>",
                        pdfBytes,
                        fileName
                    );
                }
                else
                {
                    await _email.SendEmailAsync(
                        to!,
                        $"Your Air Ticket – PNR {b.Pnr}",
                        $"Your ticket is ready. Download: {b.TicketPdfPath}"
                    );
                }
            }

            TempData["ok"] = "Approved & ticket emailed.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
