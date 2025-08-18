using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _ctx;

        public ReportController(ApplicationDbContext ctx)
        {
            _ctx = ctx;
        }

        // View: filters + table + totals
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? from = null, DateTime? to = null)
        {
            // treat incoming dates as local-day range -> convert to UTC range
            var fromLocal = (from ?? DateTime.Today).Date;
            var toLocalEnd = (to ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

            var startUtc = DateTime.SpecifyKind(fromLocal, DateTimeKind.Local).ToUniversalTime();
            var endUtc = DateTime.SpecifyKind(toLocalEnd, DateTimeKind.Local).ToUniversalTime();

            var bookings = await _ctx.Bookings
                .Include(b => b.BusSchedule)
                .Include(b => b.Seats).ThenInclude(bs => bs.ScheduleSeat)
                .Where(b =>
                    b.PaymentStatus == PaymentStatus.Paid &&
                    b.Status == BookingStatus.Approved &&
                    b.CreatedAtUtc >= startUtc && b.CreatedAtUtc <= endUtc)
                .OrderByDescending(b => b.CreatedAtUtc)
                .ToListAsync();

            var ticketsSold = await _ctx.BookingSeats
                .Include(bs => bs.Booking)
                .Where(bs =>
                    bs.Booking.PaymentStatus == PaymentStatus.Paid &&
                    bs.Booking.Status == BookingStatus.Approved &&
                    bs.Booking.CreatedAtUtc >= startUtc && bs.Booking.CreatedAtUtc <= endUtc)
                .CountAsync();

            decimal revenue = bookings.Sum(b => b.GrandTotal);

            // keep YYYY-MM-DD for inputs (local)
            ViewBag.From = fromLocal.ToString("yyyy-MM-dd");
            ViewBag.To = toLocalEnd.ToString("yyyy-MM-dd");
            ViewBag.TicketsSold = ticketsSold;
            ViewBag.Revenue = revenue;

            return View(bookings);
        }

        // Download report as PDF
        [HttpGet]
        public async Task<IActionResult> Download(DateTime? from = null, DateTime? to = null)
        {
            var fromLocal = (from ?? DateTime.Today).Date;
            var toLocalEnd = (to ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

            var startUtc = DateTime.SpecifyKind(fromLocal, DateTimeKind.Local).ToUniversalTime();
            var endUtc = DateTime.SpecifyKind(toLocalEnd, DateTimeKind.Local).ToUniversalTime();

            var bookings = await _ctx.Bookings
                .Include(b => b.BusSchedule)
                .Include(b => b.Seats).ThenInclude(bs => bs.ScheduleSeat)
                .Where(b =>
                    b.PaymentStatus == PaymentStatus.Paid &&
                    b.Status == BookingStatus.Approved &&
                    b.CreatedAtUtc >= startUtc && b.CreatedAtUtc <= endUtc)
                .OrderBy(b => b.CreatedAtUtc)
                .ToListAsync();

            var ticketsSold = await _ctx.BookingSeats
                .Include(bs => bs.Booking)
                .Where(bs =>
                    bs.Booking.PaymentStatus == PaymentStatus.Paid &&
                    bs.Booking.Status == BookingStatus.Approved &&
                    bs.Booking.CreatedAtUtc >= startUtc && bs.Booking.CreatedAtUtc <= endUtc)
                .CountAsync();

            decimal revenue = bookings.Sum(b => b.GrandTotal);

            // Pick QuestPDF license (also set once in Program.cs at startup)
            QuestPDF.Settings.License = LicenseType.Community;

            using var stream = new MemoryStream();
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    // Simple text header (no images / placeholders)
                    page.Header().Column(col =>
                    {
                        col.Item().Text("RiadTrip — Sales Report").SemiBold().FontSize(16);
                        col.Item().Text($"Period: {fromLocal:dd MMM yyyy} — {toLocalEnd:dd MMM yyyy}").FontSize(10);
                    });

                    page.Content().Column(col =>
                    {
                        // Summary line
                        col.Item().Text(txt =>
                        {
                            txt.Span("Tickets Sold: ").SemiBold();
                            txt.Span(ticketsSold.ToString());
                            txt.Span("   |   ");
                            txt.Span("Revenue: ").SemiBold();
                            txt.Span($"৳{revenue:0.##}");
                        });

                        // Table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1); // Date
                                columns.RelativeColumn(2); // Route
                                columns.RelativeColumn(2); // Customer
                                columns.RelativeColumn(1); // Seats
                                columns.RelativeColumn(1); // Total
                            });

                            // header
                            table.Header(h =>
                            {
                                h.Cell().Text("Date").SemiBold();
                                h.Cell().Text("Route").SemiBold();
                                h.Cell().Text("Customer").SemiBold();
                                h.Cell().Text("Seats").SemiBold();
                                h.Cell().Text("Total").SemiBold();
                            });

                            foreach (var b in bookings)
                            {
                                var route = $"{b.BusSchedule?.From} → {b.BusSchedule?.To}";
                                var seats = string.Join(", ", b.Seats.Select(s => s.ScheduleSeat.SeatNo));

                                table.Cell().Text(b.CreatedAtUtc.ToLocalTime().ToString("dd MMM yyyy"));
                                table.Cell().Text(route);
                                table.Cell().Text($"{b.CustomerName} ({b.CustomerPhone})");
                                table.Cell().Text(seats);
                                table.Cell().Text($"৳{b.GrandTotal:0.##}");
                            }
                        });
                    });

                    page.Footer().AlignRight().Text(txt =>
                    {
                        txt.Span("Generated on ").FontSize(9);
                        txt.Span($"{DateTime.Now:dd MMM yyyy HH:mm}").FontSize(9);
                    });

                });
            }).GeneratePdf(stream);

            var fileName = $"report_{fromLocal:yyyyMMdd}_{toLocalEnd:yyyyMMdd}.pdf";
            return File(stream.ToArray(), "application/pdf", fileName);
        }
    }
}
