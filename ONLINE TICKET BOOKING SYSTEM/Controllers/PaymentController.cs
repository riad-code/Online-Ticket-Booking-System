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
using ONLINE_TICKET_BOOKING_SYSTEM.Models.Air;

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

        private string GetBaseUrl() =>
            (_configuration["App:BaseUrl"]?.TrimEnd('/')) ??
            $"{Request.Scheme}://{Request.Host}";

        // ---------------- BUS ----------------

        // Create SSLCommerz session for Bus booking
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

        // BUS: SUCCESS callback
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

            booking.PaymentStatus = PaymentStatus.Paid;
            booking.Status = BookingStatus.PendingApproval;
            booking.PaymentAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

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
                catch { }
            }

            return RedirectToAction("ThankYou", "Booking", new { id = booking.Id });
        }

        // BUS: FAIL callback
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

        // BUS: CANCEL callback
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpGet("payment/cancel"), HttpPost("payment/cancel")]
        public IActionResult PaymentCancel() => View("PaymentCancelled");

        // ---------------- AIR ----------------

        // Create SSLCommerz session for Air booking (via PNR)
        [HttpPost]
        public async Task<IActionResult> AirPayment(string pnr)
        {
            var b = await _context.AirBookings
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments)
                .FirstOrDefaultAsync(x => x.Pnr == pnr);

            if (b == null)
                return View("Error", Error("Air booking not found."));

            if (b.AmountDue <= 0)
                return View("Error", Error("Amount must be greater than zero."));

            var baseUrl = GetBaseUrl();
            var redirectUrl = await _paymentService.InitiateAirPaymentAsync(b, baseUrl);
            if (string.IsNullOrWhiteSpace(redirectUrl))
                return View("Error", Error("Payment initiation failed. Please try again."));

            return Redirect(redirectUrl);
        }

        // ---------- AIR: SUCCESS callback (uses tran_id as PNR) ----------
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpGet("airpayment/success"), HttpPost("airpayment/success")]
        public async Task<IActionResult> AirPaymentSuccess()
        {
            var form = Request.HasFormContentType ? Request.Form : null;
            var query = Request.Query;

            // SSLCommerz posts back tran_id + val_id. tran_id == your PNR
            var tranId = form?["tran_id"].ToString() ?? query["tran_id"].ToString();
            var valId = form?["val_id"].ToString() ?? query["val_id"].ToString();

            if (string.IsNullOrWhiteSpace(tranId) || string.IsNullOrWhiteSpace(valId))
                return View("Error", Error("Missing tran_id (PNR) or val_id."));

            var b = await _context.AirBookings.FirstOrDefaultAsync(x => x.Pnr == tranId);
            if (b == null)
                return View("Error", Error("Air booking not found."));

            var valid = await ValidateWithSslCommerz(valId);
            if (valid == null)
                return View("Error", Error("Unable to validate payment."));

            var ok = string.Equals(valid.status, "VALID", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(valid.status, "VALIDATED", StringComparison.OrdinalIgnoreCase);
            if (!ok)
                return View("Error", Error($"Validation failed: {valid.status}"));

            // sanity checks
            if (!string.Equals(valid.tran_id, tranId, StringComparison.Ordinal))
                return View("Error", Error("Validation mismatch (tran_id)."));
            if (!string.Equals(valid.currency, "BDT", StringComparison.OrdinalIgnoreCase))
                return View("Error", Error("Validation mismatch (currency)."));

            b.PaymentStatus = AirPaymentStatus.Paid;
            b.BookingStatus = AirBookingStatus.PendingApproval;
            b.PaymentAtUtc = DateTime.UtcNow;
            b.AmountPaid = b.AmountDue;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(b.ContactEmail))
            {
                try
                {
                    await _emailSender.SendEmailAsync(
                        b.ContactEmail,
                        "Payment Successful",
                        $"Your payment for PNR {b.Pnr} was successful. You'll receive your e-ticket after admin approval."
                    );
                }
                catch { /* ignore */ }
            }

            return RedirectToAction("ThankYou", "AirBooking", new { pnr = b.Pnr });
        }

        // ---------- AIR: FAIL callback (uses tran_id as PNR) ----------
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpGet("airpayment/fail"), HttpPost("airpayment/fail")]
        public async Task<IActionResult> AirPaymentFail()
        {
            var form = Request.HasFormContentType ? Request.Form : null;
            var query = Request.Query;

            var tranId = form?["tran_id"].ToString() ?? query["tran_id"].ToString(); // equals PNR

            if (!string.IsNullOrWhiteSpace(tranId))
            {
                var b = await _context.AirBookings.FirstOrDefaultAsync(x => x.Pnr == tranId);
                if (b != null)
                {
                    b.PaymentStatus = AirPaymentStatus.Unpaid;
                    b.BookingStatus = AirBookingStatus.PendingPayment;
                    await _context.SaveChangesAsync();
                }
            }

            return View("PaymentFailure");
        }


        // AIR: CANCEL callback
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpGet("airpayment/cancel"), HttpPost("airpayment/cancel")]
        public IActionResult AirPaymentCancel() => View("PaymentCancelled");

        // ---------------- Shared Helpers ----------------

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
            };

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
