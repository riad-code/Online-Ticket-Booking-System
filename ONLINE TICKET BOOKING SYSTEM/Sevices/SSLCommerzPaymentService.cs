using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

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
        /// Creates a payment session and returns the GatewayPageURL to redirect.
        /// </summary>
        public async Task<string?> InitiatePaymentAsync(Booking booking, string baseUrl)
        {
            var storeId = _configuration["SSLCommerz:StoreId"];
            var storePassword = _configuration["SSLCommerz:StorePassword"];

            // Build callback URLs from the running app base URL to ensure they match your routes.
            var successUrl = $"{baseUrl}/payment/success";
            var failUrl = $"{baseUrl}/payment/fail";
            var cancelUrl = $"{baseUrl}/payment/cancel";
            var ipnUrl = $"{baseUrl}/payment/ipn";

            var data = new Dictionary<string, string>
            {
                // required credentials
                { "store_id", storeId ?? "" },
                { "store_passwd", storePassword ?? "" },

                // required txn/money
                { "total_amount", booking.GrandTotal.ToString(CultureInfo.InvariantCulture) },
                { "currency", "BDT" },
                { "tran_id", booking.Id.ToString() },

                // required callbacks
                { "success_url", successUrl },
                { "fail_url", failUrl },
                { "cancel_url", cancelUrl },

                // optional but recommended IPN
                { "ipn_url", ipnUrl },

                // customer info
                { "cus_name",  booking.CustomerName ?? "Customer" },
                { "cus_email", booking.CustomerEmail ?? "no-reply@example.com" },
                { "cus_phone", booking.CustomerPhone ?? "00000000000" },
                { "cus_add1",  "N/A" },
                { "cus_city",  "Dhaka" },
                { "cus_postcode", "1200" },
                { "cus_country",  "Bangladesh" },

                // product/meta
                { "shipping_method", "NO" },
                { "product_name",    "Bus Ticket" },
                { "product_category","Travel" },
                { "product_profile", "general" },

                { "emi_option", "0" }
            };

            // v4 is fine; v3 also works. Use sandbox endpoint.
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
