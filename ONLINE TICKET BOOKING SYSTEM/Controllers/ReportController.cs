using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using ONLINE_TICKET_BOOKING_SYSTEM.Models.Air;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
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

        // =========================
        // BUS: index/list screen
        // =========================
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

        // =========================
        // BUS: PDF download
        // =========================
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
                    page.Margin(25);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(t => t.FontSize(10));

                    // ===== Header (Title + Company Box) =====
                    page.Header().PaddingBottom(8).Row(row =>
                    {
                        row.RelativeItem().AlignLeft().Column(c =>
                        {
                            c.Item().Text("Sale Report").FontSize(20).SemiBold();
                            c.Item().Text("RiadTrip").SemiBold().FontSize(11);
                            c.Item().Text("Street Address\nCity, State, Zip\nPhone • Website • Email")
                                .FontColor(Colors.Grey.Darken2).FontSize(9);
                        });

                        row.ConstantItem(180).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(c =>
                        {
                            c.Item().AlignCenter().Text("Company\nLogo Here").FontSize(11).FontColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(6).Row(r =>
                            {
                                r.RelativeItem().Text("Date:").Bold();
                                r.RelativeItem().AlignRight().Text($"{DateTime.Now:dd MMM yyyy}");
                            });
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("From:").Bold();
                                r.RelativeItem().AlignRight().Text($"{fromLocal:dd/MM/yyyy}");
                            });
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("To:").Bold();
                                r.RelativeItem().AlignRight().Text($"{toLocalEnd:dd/MM/yyyy}");
                            });
                        });
                    });

                    // ===== Content =====
                    page.Content().Column(col =>
                    {
                        // Summary band
                        col.Item().PaddingBottom(6).Border(1).BorderColor(Colors.Grey.Lighten1)
                           .Background(Colors.Grey.Lighten5).Padding(8).Row(r =>
                           {
                               r.ConstantItem(160).Text(t => { t.Span("Tickets Sold: ").SemiBold(); t.Span(ticketsSold.ToString()); });
                               r.ConstantItem(220).Text(t => { t.Span("Revenue: ").SemiBold(); t.Span($"৳{revenue:0.##}"); });

                           });

                        // Table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1); // Month
                                columns.RelativeColumn(1); // Date
                                columns.RelativeColumn(3); // Route
                                columns.RelativeColumn(3); // Customer
                                columns.RelativeColumn(2); // Seats
                                columns.RelativeColumn(1); // Total
                                columns.RelativeColumn(1); // Paid
                                columns.RelativeColumn(1); // Balance
                            });

                            // Header
                            table.Header(h =>
                            {
                                void H(string s) => h.Cell().Border(1).BorderColor(Colors.Grey.Lighten1)
                                    .Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(6)
                                    .Text(s).SemiBold();
                                H("Month");
                                H("Date");
                                H("Route");
                                H("Customer");
                                H("Seats");
                                H("Total");
                                H("Paid");
                                H("Balance Due");
                            });

                            foreach (var b in bookings)
                            {
                                var createdLocal = b.CreatedAtUtc.ToLocalTime();
                                var monthLabel = createdLocal.ToString("M/yyyy");
                                var dateLabel = createdLocal.ToString("d/M/yyyy");
                                var route = $"{b.BusSchedule?.From} → {b.BusSchedule?.To}";
                                var seatsCsv = string.Join(", ", b.Seats.Select(s => s.ScheduleSeat.SeatNo));

                                void Cell(string txt) => table.Cell()
                                    .BorderLeft(1).BorderRight(1).BorderColor(Colors.Grey.Lighten1)
                                    .PaddingVertical(4).PaddingHorizontal(6).Text(txt);

                                Cell(monthLabel);
                                Cell(dateLabel);
                                Cell(route);
                                Cell($"{b.CustomerName} ({b.CustomerPhone})");
                                Cell(seatsCsv);
                                Cell($"৳{b.GrandTotal:0.##}");
                                Cell(b.PaymentStatus == PaymentStatus.Paid ? $"৳{b.GrandTotal:0.##}" : "৳0");
                                Cell(b.PaymentStatus == PaymentStatus.Paid ? "৳0" : $"৳{b.GrandTotal:0.##}");
                            }

                            // bottom line
                            table.Cell().ColumnSpan(8).BorderTop(1).BorderColor(Colors.Grey.Lighten1).Padding(0).Text("");
                        });

                        // Totals band
                        col.Item().AlignRight().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem();
                            r.ConstantItem(220).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Row(rr =>
                            {
                                rr.RelativeItem().Text("Total").SemiBold();
                                rr.AutoItem().Text($"৳{revenue:0.##}").SemiBold();
                            });
                        });
                    });

                    // ===== Footer: signatures =====
                    page.Footer().PaddingTop(16).Row(r =>
                    {
                        r.RelativeItem().PaddingTop(10).Row(x =>
                        {
                            x.RelativeItem().Text("Signed By:").SemiBold();
                            x.RelativeItem().PaddingLeft(6).PaddingRight(40)
                                .BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                        });
                        r.RelativeItem().PaddingTop(10).Row(x =>
                        {
                            x.RelativeItem().Text("Submitted By:").SemiBold();
                            x.RelativeItem().PaddingLeft(6).PaddingRight(40)
                                .BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                        });
                    });
                });
            }).GeneratePdf(stream);

            var fileName = $"report_{fromLocal:yyyyMMdd}_{toLocalEnd:yyyyMMdd}.pdf";
            return File(stream.ToArray(), "application/pdf", fileName);
        }

        // ===============================
        // AIR: index/list screen
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
                    ((b.PaymentAtUtc != null && b.PaymentAtUtc >= startUtc && b.PaymentAtUtc <= endUtc)
                     || (b.PaymentAtUtc == null && b.CreatedAtUtc >= startUtc && b.CreatedAtUtc <= endUtc)))
                .OrderByDescending(b => b.PaymentAtUtc ?? b.CreatedAtUtc)
                .ToListAsync();

            var ticketsSold = airBookings.Sum(b => b.Passengers?.Count ?? (b.Adults + b.Children + b.Infants));
            decimal revenue = airBookings.Sum(b => (decimal)(b.AmountPaid > 0 ? b.AmountPaid : b.AmountDue));

            ViewBag.From = fromLocal.ToString("yyyy-MM-dd");
            ViewBag.To = toLocalEnd.ToString("yyyy-MM-dd");
            ViewBag.TicketsSold = ticketsSold;
            ViewBag.Revenue = revenue;

            return View(airBookings);
        }

        // ===============================
        // AIR: PDF download
        // ===============================
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
                    page.Margin(25);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(t => t.FontSize(10));

                    // ===== Header (Title + Company Box) =====
                    page.Header().PaddingBottom(8).Row(row =>
                    {
                        row.RelativeItem().AlignLeft().Column(c =>
                        {
                            c.Item().Text("Sale Report (Air)").FontSize(20).SemiBold();
                            c.Item().Text("RiadTrip").SemiBold().FontSize(11);
                            c.Item().Text("Street Address\nCity, State, Zip\nPhone • Website • Email")
                                .FontColor(Colors.Grey.Darken2).FontSize(9);
                        });

                        row.ConstantItem(180).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(c =>
                        {
                            c.Item().AlignCenter().Text("Company\nLogo Here").FontSize(11).FontColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(6).Row(r =>
                            {
                                r.RelativeItem().Text("Date:").Bold();
                                r.RelativeItem().AlignRight().Text($"{DateTime.Now:dd MMM yyyy}");
                            });
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("From:").Bold();
                                r.RelativeItem().AlignRight().Text($"{fromLocal:dd/MM/yyyy}");
                            });
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("To:").Bold();
                                r.RelativeItem().AlignRight().Text($"{toLocalEnd:dd/MM/yyyy}");
                            });
                        });
                    });

                    // ===== Content =====
                    page.Content().Column(col =>
                    {
                        // Summary band
                        col.Item().PaddingBottom(6).Border(1).BorderColor(Colors.Grey.Lighten1)
                           .Background(Colors.Grey.Lighten5).Padding(8).Row(r =>
                           {
                               r.ConstantItem(200).Text(t => { t.Span("Tickets Sold (pax): ").SemiBold(); t.Span(ticketsSold.ToString()); });
                               r.ConstantItem(220).Text(t => { t.Span("Revenue: ").SemiBold(); t.Span($"৳{revenue:0.##}"); });


                           });

                        // Table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1); // Month
                                columns.RelativeColumn(1); // Date
                                columns.RelativeColumn(1); // PNR
                                columns.RelativeColumn(2); // Route
                                columns.RelativeColumn(2); // Flight
                                columns.RelativeColumn(1); // Pax
                                columns.RelativeColumn(1); // Total
                                columns.RelativeColumn(1); // Paid
                                columns.RelativeColumn(1); // Balance
                            });

                            // Header
                            table.Header(h =>
                            {
                                void H(string s) => h.Cell().Border(1).BorderColor(Colors.Grey.Lighten1)
                                    .Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(6)
                                    .Text(s).SemiBold();
                                H("Month");
                                H("Date");
                                H("PNR");
                                H("Route");
                                H("Flight");
                                H("Pax");
                                H("Total");
                                H("Paid");
                                H("Balance Due");
                            });

                            foreach (var b in airBookings)
                            {
                                var when = (b.PaymentAtUtc ?? b.CreatedAtUtc).ToLocalTime();
                                var monthLabel = when.ToString("M/yyyy");
                                var dateLabel = when.ToString("d/M/yyyy");
                                var seg = b.Itinerary?.Segments?.FirstOrDefault()?.FlightSchedule;
                                var route = $"{seg?.FromAirport?.IataCode ?? "-"} → {seg?.ToAirport?.IataCode ?? "-"}";
                                var flight = $"{seg?.Airline?.IataCode ?? "-"} {seg?.FlightNumber ?? "-"}";
                                var pax = b.Passengers?.Count ?? (b.Adults + b.Children + b.Infants);
                                var total = (decimal)(b.AmountPaid > 0 ? b.AmountPaid : b.AmountDue);
                                var paid = b.PaymentStatus == AirPaymentStatus.Paid ? total : 0m;
                                var bal = total - paid;

                                void Cell(string txt) => table.Cell()
                                    .BorderLeft(1).BorderRight(1).BorderColor(Colors.Grey.Lighten1)
                                    .PaddingVertical(4).PaddingHorizontal(6).Text(txt);

                                Cell(monthLabel);
                                Cell(dateLabel);
                                Cell(b.Pnr ?? "-");
                                Cell(route);
                                Cell(flight);
                                Cell(pax.ToString());
                                Cell($"৳{total:0.##}");
                                Cell($"৳{paid:0.##}");
                                Cell($"৳{bal:0.##}");
                            }

                            // bottom line
                            table.Cell().ColumnSpan(9).BorderTop(1).BorderColor(Colors.Grey.Lighten1).Padding(0).Text("");
                        });

                        // Totals band
                        col.Item().AlignRight().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem();
                            r.ConstantItem(220).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Row(rr =>
                            {
                                rr.RelativeItem().Text("Total").SemiBold();
                                rr.AutoItem().Text($"৳{revenue:0.##}").SemiBold();
                            });
                        });
                    });

                    // ===== Footer: signatures =====
                    page.Footer().PaddingTop(16).Row(r =>
                    {
                        r.RelativeItem().PaddingTop(10).Row(x =>
                        {
                            x.RelativeItem().Text("Signed By:").SemiBold();
                            x.RelativeItem().PaddingLeft(6).PaddingRight(40)
                                .BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                        });
                        r.RelativeItem().PaddingTop(10).Row(x =>
                        {
                            x.RelativeItem().Text("Submitted By:").SemiBold();
                            x.RelativeItem().PaddingLeft(6).PaddingRight(40)
                                .BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                        });
                    });
                });
            }).GeneratePdf(stream);

            var fileName = $"air_report_{fromLocal:yyyyMMdd}_{toLocalEnd:yyyyMMdd}.pdf";
            return File(stream.ToArray(), "application/pdf", fileName);
        }
    }
}
