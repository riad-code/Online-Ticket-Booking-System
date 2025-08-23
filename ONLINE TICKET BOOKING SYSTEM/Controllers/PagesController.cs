using Microsoft.AspNetCore.Identity.UI.Services; // IEmailSender
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ONLINE_TICKET_BOOKING_SYSTEM.Services;
using System.Threading.Tasks;


namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    public class PagesController : Controller
    {
        // -------- Static Pages --------
        [HttpGet] public IActionResult About() => View();

        [HttpGet] public IActionResult Deals() => View();

        [HttpGet] public IActionResult BusReservation() => View();

        [HttpGet] public IActionResult Blog() => View();


        // -------- Contact --------
        [HttpGet]
        public IActionResult Contact() => View();
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(
            string name,
            string email,
            string message,
            [FromServices] IEmailSender emailSender,
            [FromServices] IOptions<EmailSettings> opts,
            [FromServices] ONLINE_TICKET_BOOKING_SYSTEM.Data.ApplicationDbContext db)
        {
            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(message))
            {
                TempData["ok"] = null;
                TempData["err"] = "Please fill in all fields.";
                return RedirectToAction(nameof(Contact));
            }

            // 1) Save to DB (Admin can see in dashboard)
            var cm = new ONLINE_TICKET_BOOKING_SYSTEM.Models.ContactMessage
            {
                Name = name.Trim(),
                Email = email.Trim(),
                Message = message.Trim(),
                SentAt = DateTime.UtcNow
            };
            db.ContactMessages.Add(cm);
            await db.SaveChangesAsync();

            // 2) Email to Admin (existing behavior)
            var admin = opts.Value.AdminEmail;
            if (string.IsNullOrWhiteSpace(admin))
            {
                TempData["ok"] = null;
                TempData["err"] = "Admin email is not configured.";
                return RedirectToAction(nameof(Contact));
            }

            var subject = $"Contact form: {name}";
            var body = $@"
        <p><b>Name:</b> {System.Net.WebUtility.HtmlEncode(name)}</p>
        <p><b>Email:</b> {System.Net.WebUtility.HtmlEncode(email)}</p>
        <p><b>Message:</b><br/>{System.Net.WebUtility.HtmlEncode(message)}</p>
        <hr/>
        <p><i>Message ID:</i> {cm.Id}</p>";

            await emailSender.SendEmailAsync(admin, subject, body);

            TempData["err"] = null;
            TempData["ok"] = "Thanks! We received your message. Our team will get back to you soon.";
            return RedirectToAction(nameof(Contact));
        }


        // -------- Insurance Claim --------
        [HttpGet]
        public IActionResult InsuranceClaim() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult InsuranceClaim(string pnr, string fullName, string phone, string email, string issueType, string details)
        {
            // TODO: Save to DB and/or notify admin if desired
            TempData["ok"] = "Your claim request has been submitted. We’ll update you by email/SMS.";
            return RedirectToAction(nameof(InsuranceClaim));
        }


        // -------- Cancel Ticket --------
        [HttpGet]
        public IActionResult CancelTicket() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelTicket(
     string pnr,
     string phoneOrEmail,
     string? reason,
     [FromServices] ONLINE_TICKET_BOOKING_SYSTEM.Data.ApplicationDbContext db,
     [FromServices] Microsoft.AspNetCore.Identity.UI.Services.IEmailSender emailSender,
     [FromServices] Microsoft.Extensions.Options.IOptions<ONLINE_TICKET_BOOKING_SYSTEM.Services.EmailSettings> opts)
        {
            if (string.IsNullOrWhiteSpace(pnr) || string.IsNullOrWhiteSpace(phoneOrEmail))
            {
                TempData["err"] = "Please provide PNR and phone/email.";
                return RedirectToAction(nameof(CancelTicket));
            }

            if (!int.TryParse(pnr.Trim(), out var bookingId))
            {
                TempData["err"] = "Invalid PNR / Booking ID.";
                return RedirectToAction(nameof(CancelTicket));
            }

            var booking = await db.Bookings
                .Include(b => b.BusSchedule)
                .Include(b => b.Seats).ThenInclude(s => s.ScheduleSeat)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                TempData["err"] = "Booking not found.";
                return RedirectToAction(nameof(CancelTicket));
            }

            var key = phoneOrEmail.Trim().ToLowerInvariant();
            var phoneMatch = (booking.CustomerPhone ?? "").Trim().ToLowerInvariant() == key;
            var emailMatch = (booking.CustomerEmail ?? "").Trim().ToLowerInvariant() == key;
            if (!phoneMatch && !emailMatch)
            {
                TempData["err"] = "Phone/Email does not match this booking.";
                return RedirectToAction(nameof(CancelTicket));
            }

            // Mark as requested (DO NOT cancel yet)
            booking.Status = ONLINE_TICKET_BOOKING_SYSTEM.Models.BookingStatus.CancelRequested;

            // If you have these properties they will be set; if not, this is safely ignored.
            var piReason = booking.GetType().GetProperty("CancelReason");
            if (piReason != null && piReason.CanWrite) piReason.SetValue(booking, reason);
            var piUpdated = booking.GetType().GetProperty("UpdatedAt");
            if (piUpdated != null && piUpdated.CanWrite) piUpdated.SetValue(booking, DateTime.UtcNow);

            await db.SaveChangesAsync();

            // Notify admin
            try
            {
                var adminEmail = opts.Value.AdminEmail;
                if (!string.IsNullOrWhiteSpace(adminEmail))
                {
                    var subjectAdmin = $"[Cancel Request] PNR #{booking.Id}";
                    var bodyAdmin = $"A customer requested cancellation for PNR #{booking.Id}.<br/>Reason: {System.Net.WebUtility.HtmlEncode(reason)}";
                    await emailSender.SendEmailAsync(adminEmail, subjectAdmin, bodyAdmin);
                }
            }
            catch { /* ignore mail errors */ }

            TempData["ok"] = "Your cancel request has been submitted. We will notify you after admin review.";
            return RedirectToAction(nameof(CancelTicket));
        }


    }
}
