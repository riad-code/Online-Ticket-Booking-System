using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using ONLINE_TICKET_BOOKING_SYSTEM.Services;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly SSLCommerzPaymentService _paymentService;

        public PaymentController(
            IConfiguration configuration,
            ApplicationDbContext context,
            IEmailSender emailSender,
            SSLCommerzPaymentService paymentService)
        {
            _configuration = configuration;
            _context = context;
            _emailSender = emailSender;
            _paymentService = paymentService;
        }

        // Build base URL from config if provided, otherwise from the current request.
        private string GetBaseUrl() =>
            (_configuration["App:BaseUrl"]?.TrimEnd('/')) ??
            $"{Request.Scheme}://{Request.Host}";

        // Allow both GET (from "Proceed to payment" button/link) and POST.
        [HttpGet, HttpPost]
        public async Task<IActionResult> Payment(int id)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null)
                return View("Error", Error("Booking not found."));

            if (booking.GrandTotal <= 0)
                return View("Error", Error("Grand total must be greater than zero."));

            var baseUrl = GetBaseUrl();
            var redirectUrl = await _paymentService.InitiatePaymentAsync(booking, baseUrl);

            if (string.IsNullOrWhiteSpace(redirectUrl))
                return View("Error", Error("Payment initiation failed. Please try again."));

            return Redirect(redirectUrl);
        }

        // SUCCESS callback – SSLCommerz often POSTs form fields; accept both GET and POST.
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpGet("payment/success"), HttpPost("payment/success")]
        public async Task<IActionResult> PaymentSuccess()
        {
            var form = Request.HasFormContentType ? Request.Form : null;
            var query = Request.Query;

            var tranId = form?["tran_id"].ToString() ?? query["tran_id"].ToString();
            var valId = form?["val_id"].ToString() ?? query["val_id"].ToString();

            if (string.IsNullOrWhiteSpace(tranId) || string.IsNullOrWhiteSpace(valId))
                return View("Error", Error("Missing tran_id or val_id."));

            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id.ToString() == tranId);
            if (booking == null)
                return View("Error", Error("Booking not found."));

            // Validate against SSLCommerz Validator API
            var valid = await ValidateWithSslCommerz(valId);
            if (valid == null)
                return View("Error", Error("Unable to validate payment."));

            var ok = string.Equals(valid.status, "VALID", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(valid.status, "VALIDATED", StringComparison.OrdinalIgnoreCase);

            if (!ok)
                return View("Error", Error($"Validation failed: {valid.status}"));

            if (!string.Equals(valid.tran_id, tranId, StringComparison.Ordinal))
                return View("Error", Error("Validation mismatch (tran_id)."));

            if (!string.Equals(valid.currency, "BDT", StringComparison.OrdinalIgnoreCase))
                return View("Error", Error("Validation mismatch (currency)."));

            // Mark booking paid
            booking.PaymentStatus = PaymentStatus.Paid;
            booking.Status = BookingStatus.PendingApproval; // or Approved if you auto-approve
            booking.PaymentAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Best-effort email
            if (!string.IsNullOrWhiteSpace(booking.CustomerEmail))
            {
                try
                {
                    await _emailSender.SendEmailAsync(
                        booking.CustomerEmail,
                        "Payment Successful",
                        $"Your payment for booking #{booking.Id} was successful."
                    );
                }
                catch { /* ignore email errors */ }
            }

            return RedirectToAction("ThankYou", "Booking", new { id = booking.Id });
        }

        // FAIL callback
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpGet("payment/fail"), HttpPost("payment/fail")]
        public async Task<IActionResult> PaymentFail()
        {
            var form = Request.HasFormContentType ? Request.Form : null;
            var query = Request.Query;

            var tranId = form?["tran_id"].ToString() ?? query["tran_id"].ToString();

            if (int.TryParse(tranId, out var id))
            {
                var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);
                if (booking != null)
                {
                    booking.PaymentStatus = PaymentStatus.Unpaid;
                    booking.Status = BookingStatus.PendingPayment;
                    await _context.SaveChangesAsync();
                    return View("PaymentFailure", booking);
                }
            }

            return View("PaymentFailure");
        }

        // CANCEL callback
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpGet("payment/cancel"), HttpPost("payment/cancel")]
        public IActionResult PaymentCancel() => View("PaymentCancelled");

        // Optional IPN endpoint (can be expanded later)
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost("payment/ipn")]
        public IActionResult Ipn() => Ok();

        // ---------- Helpers ----------

        private async Task<SslValidateRes?> ValidateWithSslCommerz(string valId)
        {
            string storeId = _configuration["SSLCommerz:StoreId"];
            string storePassword = _configuration["SSLCommerz:StorePassword"];

            var url =
                $"https://sandbox.sslcommerz.com/validator/api/validationserverAPI.php?val_id={Uri.EscapeDataString(valId)}&store_id={Uri.EscapeDataString(storeId)}&store_passwd={Uri.EscapeDataString(storePassword)}&v=1&format=json";

            using var client = new HttpClient();
            var json = await client.GetStringAsync(url);
            return JsonConvert.DeserializeObject<SslValidateRes>(json);
        }

        private ErrorViewModel Error(string message) =>
            new ErrorViewModel
            {
                ErrorMessage = message,
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                // ShowRequestId is read-only in the model (computed).
            };

        // Minimal DTO for validator response
        private sealed class SslValidateRes
        {
            public string status { get; set; }
            public string tran_id { get; set; }
            public string val_id { get; set; }
            public string amount { get; set; }
            public string currency { get; set; }
        }
    }
}
