using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity.UI.Services; // IEmailSender
using ONLINE_TICKET_BOOKING_SYSTEM.Services;

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
        public IActionResult CancelTicket(string pnr, string phoneOrEmail, string reason)
        {
            // TODO: Validate PNR and trigger cancellation workflow if desired
            TempData["ok"] = "Cancellation request submitted. Please check your email/SMS for confirmation.";
            return RedirectToAction(nameof(CancelTicket));
        }
    }
}
