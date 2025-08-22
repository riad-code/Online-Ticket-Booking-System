using System;
using System.Linq;
using System.Threading.Tasks;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public class TicketPdfService : ITicketPdfService
{
    public Task<byte[]> GenerateAsync(Booking b)
    {
        // Be defensive against nulls
        var s = b.BusSchedule ?? new BusSchedule();
        var seats = string.Join(", ",
            (b.Seats ?? Array.Empty<BookingSeat>())
                .Where(x => x?.ScheduleSeat?.SeatNo != null)
                .OrderBy(x => x!.ScheduleSeat!.SeatNo)
                .Select(x => x!.ScheduleSeat!.SeatNo!));

        var bytes = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(t => t.FontSize(11));

                page.Content().Column(col =>
                {
                    // ===== Header =====
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(h =>
                        {
                            h.Item().Text("RiadTrip")
                                .SemiBold().FontSize(24).FontColor(Colors.Green.Darken2);
                            h.Item().Text("Electronic Ticket")
                                .SemiBold().FontSize(13).FontColor(Colors.Grey.Darken1);
                        });

                        row.ConstantItem(150).AlignRight().Column(r =>
                        {
                            r.Item().Text($"PNR / Booking ID: {b.Id}")
                                .SemiBold().FontSize(12).FontColor(Colors.Blue.Darken2);
                            r.Item().PaddingTop(4).Text($"Issued: {DateTime.Now:dd MMM yyyy, hh:mm tt}")
                                .FontSize(10).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    col.Item().PaddingVertical(10)
                        .LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    // ===== Passenger =====
                    col.Item().Container()
                        .Background(Colors.Grey.Lighten5)
                        .Padding(12)
                        .Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .Column(p =>
                        {
                            p.Item().Text("Passenger Details")
                                .SemiBold().FontSize(13);

                            p.Item().PaddingTop(6).Row(r =>
                            {
                                r.RelativeItem().Text(t =>
                                {
                                    t.Span("Name: ").SemiBold();
                                    t.Span(b.CustomerName ?? "-");
                                });

                                r.RelativeItem().Text(t =>
                                {
                                    t.Span("Phone: ").SemiBold();
                                    t.Span(b.CustomerPhone ?? "-");
                                });
                            });

                            p.Item().PaddingTop(4).Text(t =>
                            {
                                t.Span("Email: ").SemiBold();
                                t.Span(b.CustomerEmail ?? "-");
                            });

                            if (!string.IsNullOrWhiteSpace(b.Gender))
                            {
                                p.Item().PaddingTop(4).Text(t =>
                                {
                                    t.Span("Gender: ").SemiBold();
                                    t.Span(b.Gender!);
                                });
                            }
                        });

                    col.Item().PaddingVertical(10)
                        .LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    // ===== Journey =====
                    col.Item().Container()
                        .Padding(6)
                        .Column(j =>
                        {
                            j.Item().Text("Journey Information")
                                .SemiBold().FontSize(13);

                            j.Item().PaddingTop(6).Row(r =>
                            {
                                r.RelativeItem().Text(t =>
                                {
                                    t.Span("Operator: ").SemiBold();
                                    t.Span(s.OperatorName ?? "-");
                                });

                                r.RelativeItem().Text(t =>
                                {
                                    t.Span("Bus Type: ").SemiBold();
                                    t.Span(s.BusType ?? "-");
                                });
                            });

                            j.Item().PaddingTop(4).Text(t =>
                            {
                                t.Span("Route: ").SemiBold();
                                t.Span($"{s.From} → {s.To}");
                            });

                            j.Item().PaddingTop(4).Row(r =>
                            {
                                r.RelativeItem().Text(t =>
                                {
                                    t.Span("Date: ").SemiBold();
                                    t.Span($"{s.JourneyDate:dd MMM yyyy}");
                                });

                                r.RelativeItem().Text(t =>
                                {
                                    t.Span("Time: ").SemiBold();
                                    t.Span($"{s.DepartureTime} - {s.ArrivalTime}");
                                });
                            });

                            j.Item().PaddingTop(4).Text(t =>
                            {
                                t.Span("Seats: ").SemiBold();
                                t.Span(seats);
                            });
                        });

                    col.Item().PaddingVertical(10)
                        .LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    // ===== Payment (No Insurance in Total) =====
                    col.Item().Container()
                        .Background(Colors.Grey.Lighten5)
                        .Padding(12)
                        .Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .Column(pay =>
                        {
                            pay.Item().Text("Payment Details")
                                .SemiBold().FontSize(13);

                            pay.Item().PaddingTop(6).Row(r =>
                            {
                                r.RelativeItem().Text("Ticket Price");
                                r.AutoItem().Text($"৳{b.TotalFare:0}");
                            });

                            pay.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Processing Fee");
                                r.AutoItem().Text($"৳{b.ProcessingFee:0}");
                            });

                            if (b.Discount > 0)
                            {
                                pay.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Discount");
                                    r.AutoItem().Text($"-৳{b.Discount:0}");
                                });
                            }

                            // Total Paid = GrandTotal (as per your requirement)
                            var totalPaid = b.GrandTotal;

                            pay.Item().PaddingTop(8)
                               .BorderTop(1).BorderColor(Colors.Grey.Lighten2)
                               .Row(r =>
                               {
                                   r.RelativeItem().Text("Total Paid").SemiBold();
                                   r.AutoItem().Text($"৳{totalPaid:0.##}")
                                       .SemiBold().FontColor(Colors.Green.Darken2);
                               });
                        });

                    // ===== Notes =====
                    col.Item().PaddingTop(14).Column(n =>
                    {
                        n.Item().Text("Important Notes")
                            .SemiBold().FontSize(10).FontColor(Colors.Grey.Darken1);

                        n.Item().Text("• Arrive at terminal 30 minutes before departure.")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);

                        n.Item().Text("• Keep a digital or printed copy of this ticket.")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);

                        n.Item().Text("• Present this ticket to the bus conductor during boarding.")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);

                        n.Item().Text("• Tickets are subject to the operator’s cancellation policy.")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                });

                page.Footer()
                    .AlignCenter()
                    .Text("Thank you for choosing RiadTrip")
                    .FontSize(10).FontColor(Colors.Grey.Darken1);
            });
        }).GeneratePdf();

        return Task.FromResult(bytes);
    }
}
