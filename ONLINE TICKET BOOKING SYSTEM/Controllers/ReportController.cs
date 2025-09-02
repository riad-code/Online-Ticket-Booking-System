using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ONLINE_TICKET_BOOKING_SYSTEM.Models.Air;

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

      
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? from = null, DateTime? to = null)
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

            
            ViewBag.From = fromLocal.ToString("yyyy-MM-dd");
            ViewBag.To = toLocalEnd.ToString("yyyy-MM-dd");
            ViewBag.TicketsSold = ticketsSold;
            ViewBag.Revenue = revenue;

            return View(bookings);
        }

       
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

            
            QuestPDF.Settings.License = LicenseType.Community;

            using var stream = new MemoryStream();
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                   
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
        // ===============================
        // AIR REPORT (index/list screen)
        // ===============================
        [HttpGet]
        public async Task<IActionResult> Air(DateTime? from = null, DateTime? to = null)
        {
            var fromLocal = (from ?? DateTime.Today).Date;
            var toLocalEnd = (to ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

            var startUtc = DateTime.SpecifyKind(fromLocal, DateTimeKind.Local).ToUniversalTime();
            var endUtc = DateTime.SpecifyKind(toLocalEnd, DateTimeKind.Local).ToUniversalTime();

            var airBookings = await _ctx.AirBookings
                .Include(b => b.Passengers)
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.Airline)
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.FromAirport)
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.ToAirport)
                .Where(b =>
                    b.PaymentStatus == AirPaymentStatus.Paid &&
                    b.BookingStatus == AirBookingStatus.Approved &&
                    // consider either payment date (preferred) or created date if you also store it
                    ((b.PaymentAtUtc != null && b.PaymentAtUtc >= startUtc && b.PaymentAtUtc <= endUtc)
                     || (b.PaymentAtUtc == null && b.CreatedAtUtc >= startUtc && b.CreatedAtUtc <= endUtc)))
                .OrderByDescending(b => b.PaymentAtUtc ?? b.CreatedAtUtc)
                .ToListAsync();

            // Tickets sold = total passengers ticketed in period
            var ticketsSold = airBookings.Sum(b => b.Passengers?.Count ?? (b.Adults + b.Children + b.Infants));

            // Revenue = sum of AmountPaid (fallback to AmountDue if AmountPaid not set)
            decimal revenue = airBookings.Sum(b => (decimal)(b.AmountPaid > 0 ? b.AmountPaid : b.AmountDue));

            ViewBag.From = fromLocal.ToString("yyyy-MM-dd");
            ViewBag.To = toLocalEnd.ToString("yyyy-MM-dd");
            ViewBag.TicketsSold = ticketsSold;
            ViewBag.Revenue = revenue;

            return View(airBookings);
        }

        // ==================================
        // AIR REPORT (PDF download)
        // ==================================
        [HttpGet]
        public async Task<IActionResult> DownloadAir(DateTime? from = null, DateTime? to = null)
        {
            var fromLocal = (from ?? DateTime.Today).Date;
            var toLocalEnd = (to ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

            var startUtc = DateTime.SpecifyKind(fromLocal, DateTimeKind.Local).ToUniversalTime();
            var endUtc = DateTime.SpecifyKind(toLocalEnd, DateTimeKind.Local).ToUniversalTime();

            var airBookings = await _ctx.AirBookings
                .Include(b => b.Passengers)
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.Airline)
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.FromAirport)
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.ToAirport)
                .Where(b =>
                    b.PaymentStatus == AirPaymentStatus.Paid &&
                    b.BookingStatus == AirBookingStatus.Approved &&
                    ((b.PaymentAtUtc != null && b.PaymentAtUtc >= startUtc && b.PaymentAtUtc <= endUtc)
                     || (b.PaymentAtUtc == null && b.CreatedAtUtc >= startUtc && b.CreatedAtUtc <= endUtc)))
                .OrderBy(b => b.PaymentAtUtc ?? b.CreatedAtUtc)
                .ToListAsync();

            var ticketsSold = airBookings.Sum(b => b.Passengers?.Count ?? (b.Adults + b.Children + b.Infants));
            decimal revenue = airBookings.Sum(b => (decimal)(b.AmountPaid > 0 ? b.AmountPaid : b.AmountDue));

            QuestPDF.Settings.License = LicenseType.Community;

            using var stream = new MemoryStream();
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header().Column(col =>
                    {
                        col.Item().Text("RiadTrip — Air Sales Report").SemiBold().FontSize(16);
                        col.Item().Text($"Period: {fromLocal:dd MMM yyyy} — {toLocalEnd:dd MMM yyyy}").FontSize(10);
                    });

                    page.Content().Column(col =>
                    {
                        // Summary
                        col.Item().Text(txt =>
                        {
                            txt.Span("Tickets Sold (pax): ").SemiBold();
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
                                columns.RelativeColumn(1); // PNR
                                columns.RelativeColumn(2); // Route
                                columns.RelativeColumn(2); // Flight
                                columns.RelativeColumn(1); // Pax
                                columns.RelativeColumn(1); // Total
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("Date").SemiBold();
                                h.Cell().Text("PNR").SemiBold();
                                h.Cell().Text("Route").SemiBold();
                                h.Cell().Text("Flight").SemiBold();
                                h.Cell().Text("Pax").SemiBold();
                                h.Cell().Text("Total").SemiBold();
                            });

                            foreach (var b in airBookings)
                            {
                                var seg = b.Itinerary?.Segments?.FirstOrDefault()?.FlightSchedule;
                                var route = $"{seg?.FromAirport?.IataCode ?? "-"} → {seg?.ToAirport?.IataCode ?? "-"}";
                                var flight = $"{seg?.Airline?.IataCode ?? "-"} {seg?.FlightNumber ?? "-"}";
                                var pax = b.Passengers?.Count ?? (b.Adults + b.Children + b.Infants);
                                var dt = (b.PaymentAtUtc ?? b.CreatedAtUtc).ToLocalTime().ToString("dd MMM yyyy");

                                table.Cell().Text(dt);
                                table.Cell().Text(b.Pnr ?? "-");
                                table.Cell().Text(route);
                                table.Cell().Text(flight);
                                table.Cell().Text(pax.ToString());
                                table.Cell().Text($"৳{(b.AmountPaid > 0 ? b.AmountPaid : b.AmountDue):0.##}");
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

            var fileName = $"air_report_{fromLocal:yyyyMMdd}_{toLocalEnd:yyyyMMdd}.pdf";
            return File(stream.ToArray(), "application/pdf", fileName);
        }

    }
}
