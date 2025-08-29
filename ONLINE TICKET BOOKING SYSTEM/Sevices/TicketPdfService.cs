using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using ONLINE_TICKET_BOOKING_SYSTEM.Models.Air;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Services
{
    public class TicketPdfService : ITicketPdfService
    {
        // ---------------- BUS TICKET (তোমার কোড + ছোটখাটো util গুলো ভিতরে রাখা) ----------------
        public Task<byte[]> GenerateAsync(Booking b)
        {
            var s = b.BusSchedule ?? new BusSchedule();
            var issuedOn = DateTime.Now;

            // Seats
            var seats = string.Join(", ",
                (b.Seats ?? Array.Empty<BookingSeat>())
                    .Where(x => x?.ScheduleSeat?.SeatNo != null)
                    .OrderBy(x => x!.ScheduleSeat!.SeatNo)
                    .Select(x => x!.ScheduleSeat!.SeatNo!));

            // Money
            decimal ticketPrice = b.TotalFare;
            decimal fee = b.ProcessingFee;
            decimal discount = Math.Max(0, b.Discount);
            decimal grandTotal = b.GrandTotal;
            decimal gatewayFee = Math.Max(0, grandTotal - ticketPrice - fee + discount);

            // Operator name / initials
            var opName = s.OperatorName ?? "Operator";
            var opInitials = new string(opName.Where(char.IsLetter).Take(2).DefaultIfEmpty('O').ToArray())
                                .ToUpperInvariant();

            // ---------- helpers ----------
            static object? GetProp(object source, params string[] names)
            {
                foreach (var n in names)
                {
                    var pi = source.GetType().GetProperty(n);
                    if (pi != null)
                    {
                        var val = pi.GetValue(source);
                        if (val != null) return val;
                    }
                }
                return null;
            }

            static string WebToPhysical(string path)
            {
                var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var p = path.Replace("\\", "/").Trim();
                if (p.StartsWith("~/")) p = p[2..];
                if (p.StartsWith("/")) p = p[1..];
                return Path.Combine(root, p);
            }

            static object? NormalizeLogo(object? src)
            {
                if (src == null) return null;
                if (src is Stream) return src;
                if (src is byte[] bytes) return new MemoryStream(bytes);

                if (src is string s)
                {
                    if (string.IsNullOrWhiteSpace(s)) return null;
                    var m = Regex.Match(s, @"^data:image\/[a-zA-Z0-9.+-]+;base64,(.*)$");
                    if (m.Success)
                        return new MemoryStream(Convert.FromBase64String(m.Groups[1].Value));

                    var tryPath = File.Exists(s) ? s : WebToPhysical(s);
                    return File.Exists(tryPath) ? tryPath : null;
                }
                return null;
            }

            static string FormatTimeLike(object? val)
            {
                if (val == null) return "-";
                return val switch
                {
                    string str when !string.IsNullOrWhiteSpace(str) => str,
                    DateTime dt => dt.ToString("hh:mm tt"),
                    TimeSpan ts => DateTime.Today.Add(ts).ToString("hh:mm tt"),
                    _ => val.ToString() ?? "-"
                };
            }
            // ---------- end helpers ----------

            // Optional operator logo
            var rawLogo =
                GetProp(s, "LogoPath", "OperatorLogoPath", "OperatorLogo", "Logo") ??
                GetProp(b, "OperatorLogoPath", "OperatorLogo", "Logo", "LogoPath");
            var filePathOrStream = NormalizeLogo(rawLogo);

            string boardingPlace = s.From ?? "-";
            string droppingPlace = s.To ?? "-";

            var rawArrival =
                GetProp(s, "ArrivalTime", "Arrival") ??
                GetProp(b, "ArrivalTime", "Arrival");
            string arrivalLabel = FormatTimeLike(rawArrival);

            string departLabel = $"{s.JourneyDate:dd MMM yyyy} {s.DepartureTime}";
            string coachLabel = $"{s.BusType ?? "-"} / {s.From} to {s.To}";

            var bytes = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Grey.Darken3));

                    page.Content().Column(root =>
                    {
                        // ===== MAIN TICKET CARD =====
                        root.Item().Padding(10).Border(1).BorderColor(Colors.Grey.Lighten1)
                            .Background(Colors.White)
                            .Column(main =>
                            {
                                // Header (operator + payment)
                                main.Item().Padding(6).Row(row =>
                                {
                                    // Left side
                                    row.RelativeItem().Column(left =>
                                    {
                                        left.Item().Row(r =>
                                        {
                                            if (filePathOrStream is string logoPath && !string.IsNullOrWhiteSpace(logoPath))
                                                r.ConstantItem(56).Height(56).Image(logoPath).FitArea();
                                            else if (filePathOrStream is Stream logoStream)
                                                r.ConstantItem(56).Height(56).Image(logoStream).FitArea();
                                            else
                                                r.ConstantItem(56).Height(56).Border(1).BorderColor(Colors.Grey.Lighten2)
                                                 .Background(Colors.Grey.Lighten4).AlignCenter().AlignMiddle()
                                                 .Text(opInitials).SemiBold().FontSize(16).FontColor(Colors.Blue.Darken2);

                                            r.Spacing(10);
                                            r.RelativeItem().Column(op =>
                                            {
                                                op.Item().Text(opName).SemiBold().FontSize(14).FontColor(Colors.Blue.Darken2);
                                                op.Item().Text($"{boardingPlace}, {s.From}").FontSize(9).FontColor(Colors.Grey.Darken1);
                                            });
                                        });

                                        left.Item().PaddingTop(6).Row(g1 =>
                                        {
                                            g1.RelativeItem().Text(t => { t.Span("PNR: ").SemiBold(); t.Span($"{b.Id}"); });
                                            g1.RelativeItem().Text(t => { t.Span("Trip Date: ").SemiBold(); t.Span($"{s.JourneyDate:dd MMM yyyy}"); });
                                        });

                                        left.Item().PaddingTop(2).Row(g2 =>
                                        {
                                            g2.RelativeItem().Text(t => { t.Span("From: ").SemiBold(); t.Span(s.From ?? "-"); });
                                            g2.RelativeItem().Text(t => { t.Span("To: ").SemiBold(); t.Span(s.To ?? "-"); });
                                        });

                                        left.Item().PaddingTop(2).Row(g3 =>
                                        {
                                            g3.RelativeItem().Text(t => { t.Span("Boarding: ").SemiBold(); t.Span(boardingPlace); });
                                            g3.RelativeItem().Text(t => { t.Span("Dropping: ").SemiBold(); t.Span(droppingPlace); });
                                        });

                                        left.Item().PaddingTop(2).Row(g4 =>
                                        {
                                            g4.RelativeItem().Text(t => { t.Span("Departure: ").SemiBold(); t.Span(departLabel); });
                                            g4.RelativeItem().Text(t => { t.Span("Arrival: ").SemiBold(); t.Span(arrivalLabel); });
                                        });

                                        left.Item().PaddingTop(2).Row(g5 =>
                                        {
                                            g5.RelativeItem().Text(t => { t.Span("Booked On: ").SemiBold(); t.Span($"{issuedOn:dd MMM yyyy}"); });
                                            g5.RelativeItem().Text(t => { t.Span("Booked By: ").SemiBold(); t.Span(string.IsNullOrWhiteSpace(b.CustomerName) ? "Web User" : b.CustomerName); });
                                        });
                                    });

                                    // Right payment panel
                                    row.ConstantItem(170).Column(pay =>
                                    {
                                        pay.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(p =>
                                        {
                                            p.Item().Text("Payment Details").SemiBold().FontSize(11);
                                            p.Item().PaddingTop(4).Row(x => { x.RelativeItem().Text("Ticket Price"); x.AutoItem().Text($"BDT {ticketPrice:0}"); });
                                            p.Item().Row(x => { x.RelativeItem().Text("+ Fee Charged"); x.AutoItem().Text($"BDT {fee:0}"); });
                                            p.Item().Row(x => { x.RelativeItem().Text("+ Gateway Fee"); x.AutoItem().Text($"BDT {gatewayFee:0}"); });
                                            if (discount > 0)
                                                p.Item().Row(x => { x.RelativeItem().Text("- Discount"); x.AutoItem().Text($"BDT {discount:0}"); });
                                            p.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                                            p.Item().PaddingTop(4).Row(x => { x.RelativeItem().Text("Total Paid").SemiBold(); x.AutoItem().Text($"BDT {grandTotal:0}").SemiBold(); });
                                        });
                                    });
                                });

                                main.Item().PaddingVertical(6).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

                                // Passenger panel
                                main.Item().Container().Background(Colors.Grey.Lighten5).Padding(10)
                                    .Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Column(pan =>
                                    {
                                        pan.Item().Text("Passenger Details").SemiBold().FontSize(11);
                                        pan.Item().PaddingTop(6).Row(r =>
                                        {
                                            r.RelativeItem().Text(t => { t.Span("Name: ").SemiBold(); t.Span(b.CustomerName ?? "-"); });
                                            r.RelativeItem().Text(t => { t.Span("Phone: ").SemiBold(); t.Span(b.CustomerPhone ?? "-"); });
                                        });
                                        pan.Item().PaddingTop(2).Row(r =>
                                        {
                                            r.RelativeItem().Text(t => { t.Span("Email: ").SemiBold(); t.Span(b.CustomerEmail ?? "-"); });
                                            r.RelativeItem().Text(t => { t.Span("Gender: ").SemiBold(); t.Span(string.IsNullOrWhiteSpace(b.Gender) ? "-" : b.Gender!); });
                                        });
                                        pan.Item().PaddingTop(2).Row(r =>
                                        {
                                            r.RelativeItem().Text(t => { t.Span("Journey Date: ").SemiBold(); t.Span($"{s.JourneyDate:dd MMM yyyy}"); });
                                            r.RelativeItem().Text(t => { t.Span("Buy Date: ").SemiBold(); t.Span($"{issuedOn:dd MMM yyyy}"); });
                                        });
                                    });

                                // Compact row
                                main.Item().PaddingTop(8).PaddingHorizontal(6).Row(r =>
                                {
                                    r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                        .Column(p => { p.Item().Text("Passenger").SemiBold(); p.Item().Text(b.CustomerName ?? "-"); });
                                    r.Spacing(8);
                                    r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                        .Column(p => { p.Item().Text("Seat(s)").SemiBold(); p.Item().Text(string.IsNullOrWhiteSpace(seats) ? "-" : seats); });
                                });

                                main.Item().PaddingTop(8).AlignCenter()
                                    .Text("ঘরে বসে বাসের টিকিট কিনুন সহজে, কল করুন ১৬৩৭৪")
                                    .FontSize(12).SemiBold().FontColor(Colors.Green.Darken2);

                                // Terms
                                main.Item().PaddingTop(6).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                                    .Column(t =>
                                    {
                                        t.Item().Text("Terms & Conditions").SemiBold().FontSize(11);
                                        t.Item().PaddingTop(4).Text("1. The ticket has been issued by RiadTrip.com on behalf of the bus operator.").FontSize(9);
                                        t.Item().Text("2. Passengers are requested to arrive at the boarding point 20 minutes before reporting time.").FontSize(9);
                                        t.Item().Text("3. Without a paper or digital copy of this ticket, boarding may be refused by the operator.").FontSize(9);
                                        t.Item().Text("4. Each passenger may carry 10 kg luggage free; excess may incur operator charges.").FontSize(9);
                                        t.Item().Text("5. Cancellation and refund follow the bus operator policy.").FontSize(9);
                                    });
                            });

                        // Stubs (copy kept)
                        root.Item().PaddingTop(8).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8)
                            .Column(stub =>
                            {
                                stub.Item().Row(r =>
                                {
                                    r.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text("Payment Details").SemiBold();
                                        c.Item().Text($"Original Ticket Price :  BDT {ticketPrice:0}");
                                        c.Item().Text($"+ Fee Charged        :  BDT {fee:0}");
                                        if (discount > 0) c.Item().Text($"- Discount           :  BDT {discount:0}");
                                        c.Item().Text($"+ Gateway Fee        :  BDT {gatewayFee:0}");
                                    });

                                    r.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text($"PNR :  {b.Id}  ({opName})");
                                        c.Item().Text($"Issued On :  {issuedOn:dd MMM yyyy}  (By: {(b.CustomerName ?? "Web User")})");
                                        c.Item().Text($"Trip Date :  {s.JourneyDate:dd MMM yyyy}");
                                        c.Item().Text($"Coach :  {coachLabel}");
                                        c.Item().Text($"Boarding :  {boardingPlace}");
                                        c.Item().Text($"Departure :  {departLabel}  (Arrival: {arrivalLabel})");
                                        c.Item().Text($"Passenger / Seat(s) :  {(b.CustomerName ?? "-")}    {(string.IsNullOrWhiteSpace(seats) ? "-" : seats)}");
                                    });
                                });
                            });

                        root.Item().PaddingTop(6).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8)
                            .Column(stub =>
                            {
                                stub.Item().Row(r =>
                                {
                                    r.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text("Payment Details").SemiBold();
                                        c.Item().Text($"Original Ticket Price :  BDT {ticketPrice:0}");
                                        c.Item().Text($"+ Fee Charged        :  BDT {fee:0}");
                                        if (discount > 0) c.Item().Text($"- Discount           :  BDT {discount:0}");
                                        c.Item().Text($"+ Gateway Fee        :  BDT {gatewayFee:0}");
                                    });

                                    r.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text($"PNR :  {b.Id}  ({opName})");
                                        c.Item().Text($"Issued On :  {issuedOn:dd MMM yyyy}  (By: {(b.CustomerName ?? "Web User")})");
                                        c.Item().Text($"Trip Date :  {s.JourneyDate:dd MMM yyyy}");
                                        c.Item().Text($"Coach :  {coachLabel}");
                                        c.Item().Text($"Boarding :  {boardingPlace}");
                                        c.Item().Text($"Departure :  {departLabel}  (Arrival: {arrivalLabel})");
                                        c.Item().Text($"Passenger / Seat(s) :  {(b.CustomerName ?? "-")}    {(string.IsNullOrWhiteSpace(seats) ? "-" : seats)}");
                                        if (!string.IsNullOrWhiteSpace(b.CustomerPhone))
                                            c.Item().Text($"Mobile #:  {b.CustomerPhone}");
                                    });
                                });
                            });
                    });

                    page.Footer().AlignCenter().Text("Thank you for choosing RiadTrip")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            }).GeneratePdf();

            return Task.FromResult(bytes);
        }

        // ---------------- AIR TICKET  ----------------
        public Task<byte[]> GenerateAirTicketAsync(AirBooking b)
        {
            var seg = b.Itinerary.Segments.First().FlightSchedule;
            var travelDate = b.Itinerary.Segments.First().TravelDate;

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(11));
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Text("RiadTrip • Air E-Ticket").SemiBold().FontSize(18).FontColor(Colors.Green.Darken2);
                        row.ConstantItem(120).AlignRight().Text($"PNR: {b.Pnr}").SemiBold();
                    });
                    page.Content().Column(col =>
                    {
                        col.Item().PaddingBottom(10).Text("Booking Details").SemiBold().FontSize(14);
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                            t.Cell().Text($"Route: {seg.FromAirport.IataCode} → {seg.ToAirport.IataCode}");
                            t.Cell().Text($"Flight: {seg.Airline.IataCode} {seg.FlightNumber}");
                            t.Cell().Text($"Date: {travelDate:yyyy-MM-dd}");
                            t.Cell().Text($"Times: {seg.DepTimeLocal:hh\\:mm} → {seg.ArrTimeLocal:hh\\:mm}");
                            t.Cell().Text($"Cabin: {b.Itinerary.Cabin}");
                            t.Cell().Text($"Status: {b.Status}");
                        });

                        col.Item().PaddingTop(12).Text("Passengers").SemiBold().FontSize(14);
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.ConstantColumn(28); c.RelativeColumn(); c.ConstantColumn(80); c.ConstantColumn(100); });
                            t.Header(h =>
                            {
                                h.Cell().Text("#").Bold();
                                h.Cell().Text("Name").Bold();
                                h.Cell().Text("Type").Bold();
                                h.Cell().Text("Passport").Bold();
                            });
                            int i = 1;
                            foreach (var p in b.Passengers)
                            {
                                t.Cell().Text(i++.ToString());
                                t.Cell().Text($"{p.FirstName} {p.LastName}");
                                t.Cell().Text(p.Type.ToString());
                                t.Cell().Text(p.PassportNo ?? "-");
                            }
                        });

                        col.Item().PaddingTop(12).Text("Payment").SemiBold().FontSize(14);
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(150); });
                            t.Cell().Text("Grand Total");
                            t.Cell().Text($"{b.Currency} {b.AmountDue:0.##}").AlignRight();
                            t.Cell().Text("Paid");
                            t.Cell().Text($"{b.Currency} {b.AmountPaid:0.##}").AlignRight();
                            t.Cell().Text("Balance");
                            t.Cell().Text($"{b.Currency} {(b.AmountDue - b.AmountPaid):0.##}").AlignRight();
                        });

                        col.Item().PaddingTop(16).Text("Note: Please present a valid photo ID at check-in.").FontColor(Colors.Grey.Darken2);
                    });
                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("Generated by RiadTrip • ").Light();
                        txt.Span($"{System.DateTime.Now:dd MMM yyyy HH:mm}");
                    });
                });
            });

            var bytes = doc.GeneratePdf();
            return Task.FromResult(bytes);
        }
    }
}
