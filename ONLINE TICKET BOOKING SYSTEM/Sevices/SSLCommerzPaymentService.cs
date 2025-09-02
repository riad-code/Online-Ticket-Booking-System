using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using ONLINE_TICKET_BOOKING_SYSTEM.Models.Air;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq; // for FirstOrDefault()

namespace ONLINE_TICKET_BOOKING_SYSTEM.Services
{
    public class SSLCommerzPaymentService
    {
        private readonly IConfiguration _configuration;

        public SSLCommerzPaymentService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Creates a payment session for Bus Booking and returns the GatewayPageURL to redirect.
        /// </summary>
        public async Task<string?> InitiatePaymentAsync(Booking booking, string baseUrl)
        {
            var storeId = _configuration["SSLCommerz:StoreId"];
            var storePassword = _configuration["SSLCommerz:StorePassword"];

            var successUrl = $"{baseUrl}/payment/success";
            var failUrl = $"{baseUrl}/payment/fail";
            var cancelUrl = $"{baseUrl}/payment/cancel";
            var ipnUrl = $"{baseUrl}/payment/ipn";

            var data = new Dictionary<string, string>
            {
                { "store_id", storeId ?? "" },
                { "store_passwd", storePassword ?? "" },
                { "total_amount", booking.GrandTotal.ToString(CultureInfo.InvariantCulture) },
                { "currency", "BDT" },
                { "tran_id", booking.Id.ToString() },

                { "success_url", successUrl },
                { "fail_url", failUrl },
                { "cancel_url", cancelUrl },
                { "ipn_url", ipnUrl },

                { "cus_name",  booking.CustomerName ?? "Customer" },
                { "cus_email", booking.CustomerEmail ?? "no-reply@example.com" },
                { "cus_phone", booking.CustomerPhone ?? "00000000000" },
                { "cus_add1",  "N/A" },
                { "cus_city",  "Dhaka" },
                { "cus_postcode", "1200" },
                { "cus_country",  "Bangladesh" },

                { "shipping_method", "NO" },
                { "product_name",    "Bus Ticket" },
                { "product_category","Travel" },
                { "product_profile", "general" },

                { "emi_option", "0" }
            };

            const string initUrl = "https://sandbox.sslcommerz.com/gwprocess/v4/api.php";

            using var client = new HttpClient();
            var content = new FormUrlEncodedContent(data);
            var resp = await client.PostAsync(initUrl, content);
            var json = await resp.Content.ReadAsStringAsync();

            var parsed = JsonConvert.DeserializeObject<SslInitResponse>(json);
            if (parsed != null && parsed.status?.ToUpperInvariant() == "SUCCESS" && !string.IsNullOrEmpty(parsed.GatewayPageURL))
                return parsed.GatewayPageURL;

            return null;
        }

        /// <summary>
        /// Creates a payment session for Air Booking and returns the GatewayPageURL to redirect.
        /// </summary>
        public async Task<string?> InitiateAirPaymentAsync(AirBooking b, string baseUrl)
        {
            var storeId = _configuration["SSLCommerz:StoreId"];
            var storePassword = _configuration["SSLCommerz:StorePassword"];

            var successUrl = $"{baseUrl}/airpayment/success";
            var failUrl = $"{baseUrl}/airpayment/fail";
            var cancelUrl = $"{baseUrl}/airpayment/cancel";
            var ipnUrl = $"{baseUrl}/airpayment/ipn";

            // Derive customer info safely (no ContactName/Email/Phone on AirBooking)
            var pax = b.Passengers?.FirstOrDefault();
            var cusName = pax != null
                ? $"{(pax.FirstName ?? "").Trim()} {(pax.LastName ?? "").Trim()}".Trim()
                : "Customer";
            var cusEmail = "no-reply@example.com";   // replace later if you store email on booking/user
            var cusPhone = "00000000000";            // replace later if you store phone on booking/user

            var data = new Dictionary<string, string>
            {
                { "store_id", storeId ?? "" },
                { "store_passwd", storePassword ?? "" },
                { "total_amount", b.AmountDue.ToString(CultureInfo.InvariantCulture) },
                { "currency", "BDT" },
                { "tran_id", b.Pnr }, // use PNR as unique transaction id

                { "success_url", successUrl },
                { "fail_url", failUrl },
                { "cancel_url", cancelUrl },
                { "ipn_url", ipnUrl },

                { "cus_name",  string.IsNullOrWhiteSpace(cusName) ? "Customer" : cusName },
                { "cus_email", cusEmail },
                { "cus_phone", cusPhone },
                { "cus_add1",  "N/A" },
                { "cus_city",  "Dhaka" },
                { "cus_postcode", "1200" },
                { "cus_country",  "Bangladesh" },

                { "shipping_method", "NO" },
                { "product_name",    "Air Ticket" },
                { "product_category","Travel" },
                { "product_profile", "general" },

                { "emi_option", "0" }
            };

            const string initUrl = "https://sandbox.sslcommerz.com/gwprocess/v4/api.php";

            using var client = new HttpClient();
            var content = new FormUrlEncodedContent(data);
            var resp = await client.PostAsync(initUrl, content);
            var json = await resp.Content.ReadAsStringAsync();

            var parsed = JsonConvert.DeserializeObject<SslInitResponse>(json);
            if (parsed != null && parsed.status?.ToUpperInvariant() == "SUCCESS" && !string.IsNullOrEmpty(parsed.GatewayPageURL))
                return parsed.GatewayPageURL;

            return null;
        }

        private sealed class SslInitResponse
        {
            public string? status { get; set; }
            public string? sessionkey { get; set; }
            public string? GatewayPageURL { get; set; }
            public string? failedreason { get; set; }
        }
    }
}
