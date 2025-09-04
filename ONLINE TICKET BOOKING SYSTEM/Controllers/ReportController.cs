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

            // Approved & Paid (list + tickets & revenue)
            var bookings = await _ctx.Bookings
                .Include(b => b.BusSchedule)
                .Include(b => b.Seats).ThenInclude(bs => bs.ScheduleSeat)
                .Where(b =>
                    b.PaymentStatus == PaymentStatus.Paid &&
                    b.Status == BookingStatus.Approved &&
                    b.CreatedAtUtc >= startUtc && b.CreatedAtUtc <= endUtc)
                .OrderByDescending(b => b.CreatedAtUtc)
                .ToListAsync();

            // Tickets sold (approved+paid)
            var ticketsSold = await _ctx.BookingSeats
                .Include(bs => bs.Booking)
                .Where(bs =>
                    bs.Booking.PaymentStatus == PaymentStatus.Paid &&
                    bs.Booking.Status == BookingStatus.Approved &&
                    bs.Booking.CreatedAtUtc >= startUtc && bs.Booking.CreatedAtUtc <= endUtc)
                .CountAsync();

            // Revenue (approved+paid)
            decimal revenue = bookings.Sum(b => b.GrandTotal);

            // 🔴 Cancelled count
            var cancelledCount = await _ctx.Bookings
                .Where(b =>
                    b.Status == BookingStatus.Cancelled &&
                    b.CreatedAtUtc >= startUtc && b.CreatedAtUtc <= endUtc)
                .CountAsync();

            // 🟡 Cancel Requested count
            var cancelReqCount = await _ctx.Bookings
                .Where(b =>
                    b.Status == BookingStatus.CancelRequested &&
                    b.CreatedAtUtc >= startUtc && b.CreatedAtUtc <= endUtc)
                .CountAsync();

            ViewBag.From = fromLocal.ToString("yyyy-MM-dd");
            ViewBag.To = toLocalEnd.ToString("yyyy-MM-dd");
            ViewBag.TicketsSold = ticketsSold;
            ViewBag.Revenue = revenue;
            ViewBag.CancelledCount = cancelledCount;
            ViewBag.CancelRequestCount = cancelReqCount;

            return View(bookings);
        }

        // =========================
        // BUS: PDF download  (includes cancelled section)
        // =========================
        [HttpGet]
        public async Task<IActionResult> Download(DateTime? from = null, DateTime? to = null)
        {
            var fromLocal = (from ?? DateTime.Today).Date;
            var toLocalEnd = (to ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

            var startUtc = DateTime.SpecifyKind(fromLocal, DateTimeKind.Local).ToUniversalTime();
            var endUtc = DateTime.SpecifyKind(toLocalEnd, DateTimeKind.Local).ToUniversalTime();

            // Approved & Paid
            var approvedPaid = await _ctx.Bookings
                .Include(b => b.BusSchedule)
                .Include(b => b.Seats).ThenInclude(bs => bs.ScheduleSeat)
                .Where(b =>
                    b.PaymentStatus == PaymentStatus.Paid &&
                    b.Status == BookingStatus.Approved &&
                    b.CreatedAtUtc >= startUtc && b.CreatedAtUtc <= endUtc)
                .OrderBy(b => b.CreatedAtUtc)
                .ToListAsync();

            // Cancelled / Cancel Requested
            var cancelled = await _ctx.Bookings
                .Include(b => b.BusSchedule)
                .Include(b => b.Seats).ThenInclude(bs => bs.ScheduleSeat)
                .Where(b =>
                    (b.Status == BookingStatus.Cancelled || b.Status == BookingStatus.CancelRequested) &&
                    b.CreatedAtUtc >= startUtc && b.CreatedAtUtc <= endUtc)
                .OrderBy(b => b.CreatedAtUtc)
                .ToListAsync();

            // Totals
            var ticketsSold = await _ctx.BookingSeats
                .Include(bs => bs.Booking)
                .Where(bs =>
                    bs.Booking.PaymentStatus == PaymentStatus.Paid &&
                    bs.Booking.Status == BookingStatus.Approved &&
                    bs.Booking.CreatedAtUtc >= startUtc && bs.Booking.CreatedAtUtc <= endUtc)
                .CountAsync();

            decimal revenue = approvedPaid.Sum(b => b.GrandTotal);
            int cancelledCount = cancelled.Count;
            decimal cancelledValue = cancelled.Sum(b => b.GrandTotal);
            decimal refundedAmount = cancelled
                .Where(b => b.PaymentStatus == PaymentStatus.Refunded)
                .Sum(b => b.GrandTotal);

            QuestPDF.Settings.License = LicenseType.Community;
            using var stream = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(25);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(t => t.FontSize(10));

                    // Header
                    page.Header().PaddingBottom(8).Row(row =>
                    {
                        row.RelativeItem().AlignLeft().Column(c =>
                        {
                            c.Item().Text("Sale Report (Bus)").FontSize(20).SemiBold();
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

                    // Content
                    page.Content().Column(col =>
                    {
                        // Summary band
                        col.Item().PaddingBottom(6).Border(1).BorderColor(Colors.Grey.Lighten1)
                           .Background(Colors.Grey.Lighten5).Padding(8)
                           .Column(sum =>
                           {
                               sum.Item().Row(r =>
                               {
                                   r.ConstantItem(160).Text(t => { t.Span("Tickets Sold: ").SemiBold(); t.Span(ticketsSold.ToString()); });
                                   r.ConstantItem(220).Text(t => { t.Span("Revenue (Approved Paid): ").SemiBold(); t.Span($"৳{revenue:0.##}"); });
                               });
                               sum.Item().Row(r =>
                               {
                                   r.ConstantItem(160).Text(t => { t.Span("Cancelled / Requests: ").SemiBold(); t.Span(cancelledCount.ToString()); });
                                   r.ConstantItem(220).Text(t => { t.Span("Cancelled Value: ").SemiBold(); t.Span($"৳{cancelledValue:0.##}"); });
                               });
                               sum.Item().Row(r =>
                               {
                                   r.ConstantItem(160).Text(t => { t.Span("Refunded: ").SemiBold(); t.Span($"৳{refundedAmount:0.##}"); });
                                   r.RelativeItem();
                               });
                           });

                        // Approved & Paid table
                        col.Item().PaddingTop(6).Text("Approved & Paid Bookings").SemiBold().FontSize(12);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                            });

                            table.Header(h =>
                            {
                                void H(string s) => h.Cell().Border(1).BorderColor(Colors.Grey.Lighten1)
                                    .Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(6)
                                    .Text(s).SemiBold();
                                H("Month"); H("Date"); H("Route"); H("Customer"); H("Seats"); H("Total");
                            });

                            foreach (var b in approvedPaid)
                            {
                                var createdLocal = b.CreatedAtUtc.ToLocalTime();
                                var route = $"{b.BusSchedule?.From} → {b.BusSchedule?.To}";
                                var seatsCsv = string.Join(", ", b.Seats.Select(s => s.ScheduleSeat.SeatNo));

                                void Cell(string txt) => table.Cell()
                                    .BorderLeft(1).BorderRight(1).BorderColor(Colors.Grey.Lighten1)
                                    .PaddingVertical(4).PaddingHorizontal(6).Text(txt);

                                Cell(createdLocal.ToString("M/yyyy"));
                                Cell(createdLocal.ToString("d/M/yyyy"));
                                Cell(route);
                                Cell($"{b.CustomerName} ({b.CustomerPhone})");
                                Cell(seatsCsv);
                                Cell($"৳{b.GrandTotal:0.##}");
                            }

                            table.Cell().ColumnSpan(6).BorderTop(1).BorderColor(Colors.Grey.Lighten1).Padding(0).Text("");
                        });

                        // Cancelled section
                        if (cancelled.Any())
                        {
                            col.Item().PaddingTop(10).Text("Cancelled / Cancel Requested").SemiBold().FontSize(12);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(h =>
                                {
                                    void H(string s) => h.Cell().Border(1).BorderColor(Colors.Grey.Lighten1)
                                        .Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(6)
                                        .Text(s).SemiBold();
                                    H("Date"); H("Route"); H("Customer"); H("Seats"); H("Status"); H("Value");
                                });

                                foreach (var b in cancelled)
                                {
                                    var dateLabel = b.CreatedAtUtc.ToLocalTime().ToString("d/M/yyyy");
                                    var route = $"{b.BusSchedule?.From} → {b.BusSchedule?.To}";
                                    var seatsCsv = string.Join(", ", b.Seats.Select(s => s.ScheduleSeat.SeatNo));
                                    var status = b.Status.ToString();

                                    void Cell(string txt) => table.Cell()
                                        .BorderLeft(1).BorderRight(1).BorderColor(Colors.Grey.Lighten1)
                                        .PaddingVertical(4).PaddingHorizontal(6).Text(txt);

                                    Cell(dateLabel);
                                    Cell(route);
                                    Cell($"{b.CustomerName} ({b.CustomerPhone})");
                                    Cell(seatsCsv);
                                    Cell(status);
                                    Cell($"৳{b.GrandTotal:0.##}");
                                }

                                table.Cell().ColumnSpan(6).BorderTop(1).BorderColor(Colors.Grey.Lighten1).Padding(0).Text("");
                            });
                        }

                        // Totals
                        col.Item().AlignRight().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem();
                            r.ConstantItem(260).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(cc =>
                            {
                                cc.Item().Row(rr => { rr.RelativeItem().Text("Revenue (Approved Paid)").SemiBold(); rr.AutoItem().Text($"৳{revenue:0.##}").SemiBold(); });
                                cc.Item().Row(rr => { rr.RelativeItem().Text("Cancelled Value"); rr.AutoItem().Text($"৳{cancelledValue:0.##}"); });
                                cc.Item().Row(rr => { rr.RelativeItem().Text("Refunded"); rr.AutoItem().Text($"৳{refundedAmount:0.##}"); });
                            });
                        });
                    });

                    // Footer
                    page.Footer().PaddingTop(16).Row(r =>
                    {
                        r.RelativeItem().PaddingTop(10).Row(x =>
                        {
                            x.RelativeItem().Text("Signed By:").SemiBold();
                            x.RelativeItem().PaddingLeft(6).PaddingRight(40).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                        });
                        r.RelativeItem().PaddingTop(10).Row(x =>
                        {
                            x.RelativeItem().Text("Submitted By:").SemiBold();
                            x.RelativeItem().PaddingLeft(6).PaddingRight(40).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
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
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.Airline)
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.FromAirport)
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.ToAirport)
                .Where(b =>
                    b.PaymentStatus == AirPaymentStatus.Paid &&
                    b.BookingStatus == AirBookingStatus.Approved &&
                    ((b.PaymentAtUtc != null && b.PaymentAtUtc >= startUtc && b.PaymentAtUtc <= endUtc)
                     || (b.PaymentAtUtc == null && b.CreatedAtUtc >= startUtc && b.CreatedAtUtc <= endUtc)))
                .OrderByDescending(b => b.PaymentAtUtc ?? b.CreatedAtUtc)
                .ToListAsync();

            var ticketsSold = airBookings.Sum(b => b.Passengers?.Count ?? (b.Adults + b.Children + b.Infants));
            decimal revenue = airBookings.Sum(b => (decimal)(b.AmountPaid > 0 ? b.AmountPaid : b.AmountDue));

            // counts for cards
            var airCancelled = await _ctx.AirBookings
                .Where(b =>
                    b.BookingStatus == AirBookingStatus.Cancelled &&
                    ((b.PaymentAtUtc != null && b.PaymentAtUtc >= startUtc && b.PaymentAtUtc <= endUtc)
                     || (b.PaymentAtUtc == null && b.CreatedAtUtc >= startUtc && b.CreatedAtUtc <= endUtc)))
                .CountAsync();

            var airCancelReq = await _ctx.AirBookings
                .Where(b =>
                    b.BookingStatus == AirBookingStatus.CancelRequested &&
                    ((b.PaymentAtUtc != null && b.PaymentAtUtc >= startUtc && b.PaymentAtUtc <= endUtc)
                     || (b.PaymentAtUtc == null && b.CreatedAtUtc >= startUtc && b.CreatedAtUtc <= endUtc)))
                .CountAsync();

            ViewBag.From = fromLocal.ToString("yyyy-MM-dd");
            ViewBag.To = toLocalEnd.ToString("yyyy-MM-dd");
            ViewBag.TicketsSold = ticketsSold;
            ViewBag.Revenue = revenue;
            ViewBag.CancelledCount = airCancelled;
            ViewBag.CancelRequestCount = airCancelReq;

            return View(airBookings);
        }

        // ===============================
        // AIR: PDF download (includes cancelled section)
        // ===============================
        [HttpGet]
        public async Task<IActionResult> DownloadAir(DateTime? from = null, DateTime? to = null)
        {
            var fromLocal = (from ?? DateTime.Today).Date;
            var toLocalEnd = (to ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

            var startUtc = DateTime.SpecifyKind(fromLocal, DateTimeKind.Local).ToUniversalTime();
            var endUtc = DateTime.SpecifyKind(toLocalEnd, DateTimeKind.Local).ToUniversalTime();

            // Approved & Paid
            var approvedPaid = await _ctx.AirBookings
                .Include(b => b.Passengers)
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.Airline)
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.FromAirport)
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.ToAirport)
                .Where(b =>
                    b.PaymentStatus == AirPaymentStatus.Paid &&
                    b.BookingStatus == AirBookingStatus.Approved &&
                    ((b.PaymentAtUtc != null && b.PaymentAtUtc >= startUtc && b.PaymentAtUtc <= endUtc)
                     || (b.PaymentAtUtc == null && b.CreatedAtUtc >= startUtc && b.CreatedAtUtc <= endUtc)))
                .OrderBy(b => b.PaymentAtUtc ?? b.CreatedAtUtc)
                .ToListAsync();

            // Cancelled / Cancel Requested
            var cancelled = await _ctx.AirBookings
                .Include(b => b.Passengers)
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.Airline)
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.FromAirport)
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.ToAirport)
                .Where(b =>
                    (b.BookingStatus == AirBookingStatus.Cancelled || b.BookingStatus == AirBookingStatus.CancelRequested) &&
                    ((b.PaymentAtUtc != null && b.PaymentAtUtc >= startUtc && b.PaymentAtUtc <= endUtc)
                     || (b.PaymentAtUtc == null && b.CreatedAtUtc >= startUtc && b.CreatedAtUtc <= endUtc)))
                .OrderBy(b => b.PaymentAtUtc ?? b.CreatedAtUtc)
                .ToListAsync();

            var ticketsSold = approvedPaid.Sum(b => b.Passengers?.Count ?? (b.Adults + b.Children + b.Infants));
            decimal revenue = approvedPaid.Sum(b => (decimal)(b.AmountPaid > 0 ? b.AmountPaid : b.AmountDue));

            int cancelledCount = cancelled.Count;
            decimal cancelledValue = cancelled.Sum(b => (decimal)(b.AmountPaid > 0 ? b.AmountPaid : b.AmountDue));
            decimal refundedAmount = cancelled
                .Where(b => b.PaymentStatus == AirPaymentStatus.Refunded)
                .Sum(b => (decimal)(b.AmountPaid > 0 ? b.AmountPaid : b.AmountDue));

            QuestPDF.Settings.License = LicenseType.Community;
            using var stream = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(25);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(t => t.FontSize(10));

                    // Header
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

                    // Content
                    page.Content().Column(col =>
                    {
                        // Summary
                        col.Item().PaddingBottom(6).Border(1).BorderColor(Colors.Grey.Lighten1)
                           .Background(Colors.Grey.Lighten5).Padding(8)
                           .Column(sum =>
                           {
                               sum.Item().Row(r =>
                               {
                                   r.ConstantItem(200).Text(t => { t.Span("Tickets Sold (pax): ").SemiBold(); t.Span(ticketsSold.ToString()); });
                                   r.ConstantItem(220).Text(t => { t.Span("Revenue (Approved Paid): ").SemiBold(); t.Span($"৳{revenue:0.##}"); });
                               });
                               sum.Item().Row(r =>
                               {
                                   r.ConstantItem(200).Text(t => { t.Span("Cancelled / Requests: ").SemiBold(); t.Span(cancelledCount.ToString()); });
                                   r.ConstantItem(220).Text(t => { t.Span("Cancelled Value: ").SemiBold(); t.Span($"৳{cancelledValue:0.##}"); });
                               });
                               sum.Item().Row(r =>
                               {
                                   r.ConstantItem(200).Text(t => { t.Span("Refunded: ").SemiBold(); t.Span($"৳{refundedAmount:0.##}"); });
                                   r.RelativeItem();
                               });
                           });

                        // Approved & Paid table
                        col.Item().PaddingTop(6).Text("Approved & Paid Bookings").SemiBold().FontSize(12);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            table.Header(h =>
                            {
                                void H(string s) => h.Cell().Border(1).BorderColor(Colors.Grey.Lighten1)
                                    .Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(6)
                                    .Text(s).SemiBold();
                                H("Month"); H("Date"); H("PNR"); H("Route"); H("Flight"); H("Pax"); H("Total");
                            });

                            foreach (var b in approvedPaid)
                            {
                                var when = (b.PaymentAtUtc ?? b.CreatedAtUtc).ToLocalTime();
                                var seg = b.Itinerary?.Segments?.FirstOrDefault()?.FlightSchedule;
                                var route = $"{seg?.FromAirport?.IataCode ?? "-"} → {seg?.ToAirport?.IataCode ?? "-"}";
                                var flight = $"{seg?.Airline?.IataCode ?? "-"} {seg?.FlightNumber ?? "-"}";
                                var pax = b.Passengers?.Count ?? (b.Adults + b.Children + b.Infants);
                                var total = (decimal)(b.AmountPaid > 0 ? b.AmountPaid : b.AmountDue);

                                void Cell(string txt) => table.Cell()
                                    .BorderLeft(1).BorderRight(1).BorderColor(Colors.Grey.Lighten1)
                                    .PaddingVertical(4).PaddingHorizontal(6).Text(txt);

                                Cell(when.ToString("M/yyyy"));
                                Cell(when.ToString("d/M/yyyy"));
                                Cell(b.Pnr ?? "-");
                                Cell(route);
                                Cell(flight);
                                Cell(pax.ToString());
                                Cell($"৳{total:0.##}");
                            }

                            table.Cell().ColumnSpan(7).BorderTop(1).BorderColor(Colors.Grey.Lighten1).Padding(0).Text("");
                        });

                        // Cancelled section
                        if (cancelled.Any())
                        {
                            col.Item().PaddingTop(10).Text("Cancelled / Cancel Requested").SemiBold().FontSize(12);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(h =>
                                {
                                    void H(string s) => h.Cell().Border(1).BorderColor(Colors.Grey.Lighten1)
                                        .Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(6)
                                        .Text(s).SemiBold();
                                    H("Date"); H("PNR"); H("Route"); H("Flight"); H("Pax"); H("Status"); H("Value");
                                });

                                foreach (var b in cancelled)
                                {
                                    var when = (b.PaymentAtUtc ?? b.CreatedAtUtc).ToLocalTime();
                                    var seg = b.Itinerary?.Segments?.FirstOrDefault()?.FlightSchedule;
                                    var route = $"{seg?.FromAirport?.IataCode ?? "-"} → {seg?.ToAirport?.IataCode ?? "-"}";
                                    var flight = $"{seg?.Airline?.IataCode ?? "-"} {seg?.FlightNumber ?? "-"}";
                                    var pax = b.Passengers?.Count ?? (b.Adults + b.Children + b.Infants);
                                    var val = (decimal)(b.AmountPaid > 0 ? b.AmountPaid : b.AmountDue);

                                    void Cell(string txt) => table.Cell()
                                        .BorderLeft(1).BorderRight(1).BorderColor(Colors.Grey.Lighten1)
                                        .PaddingVertical(4).PaddingHorizontal(6).Text(txt);

                                    Cell(when.ToString("d/M/yyyy"));
                                    Cell(b.Pnr ?? "-");
                                    Cell(route);
                                    Cell(flight);
                                    Cell(pax.ToString());
                                    Cell(b.BookingStatus.ToString());
                                    Cell($"৳{val:0.##}");
                                }

                                table.Cell().ColumnSpan(7).BorderTop(1).BorderColor(Colors.Grey.Lighten1).Padding(0).Text("");
                            });
                        }

                        // Totals
                        col.Item().AlignRight().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem();
                            r.ConstantItem(260).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(cc =>
                            {
                                cc.Item().Row(rr => { rr.RelativeItem().Text("Revenue (Approved Paid)").SemiBold(); rr.AutoItem().Text($"৳{revenue:0.##}").SemiBold(); });
                                cc.Item().Row(rr => { rr.RelativeItem().Text("Cancelled Value"); rr.AutoItem().Text($"৳{cancelledValue:0.##}"); });
                                cc.Item().Row(rr => { rr.RelativeItem().Text("Refunded"); rr.AutoItem().Text($"৳{refundedAmount:0.##}"); });
                            });
                        });
                    });

                    // Footer
                    page.Footer().PaddingTop(16).Row(r =>
                    {
                        r.RelativeItem().PaddingTop(10).Row(x =>
                        {
                            x.RelativeItem().Text("Signed By:").SemiBold();
                            x.RelativeItem().PaddingLeft(6).PaddingRight(40).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                        });
                        r.RelativeItem().PaddingTop(10).Row(x =>
                        {
                            x.RelativeItem().Text("Submitted By:").SemiBold();
                            x.RelativeItem().PaddingLeft(6).PaddingRight(40).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                        });
                    });
                });
            }).GeneratePdf(stream);

            var fileName = $"air_report_{fromLocal:yyyyMMdd}_{toLocalEnd:yyyyMMdd}.pdf";
            return File(stream.ToArray(), "application/pdf", fileName);
        }
    }
}
