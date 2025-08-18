using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Linq;
using System.Threading.Tasks;

public class TicketPdfService : ITicketPdfService
{
    public Task<byte[]> GenerateAsync(Booking b)
    {
        var s = b.BusSchedule!;
        var seats = string.Join(", ", b.Seats.OrderBy(x => x.ScheduleSeat!.SeatNo).Select(x => x.ScheduleSeat!.SeatNo));

        var bytes = Document.Create(c =>
        {
            c.Page(p =>
            {
                p.Size(PageSizes.A4);
                p.Margin(36);
                p.DefaultTextStyle(x => x.FontFamily(Fonts.Arial).FontSize(12));

                p.Content().PaddingVertical(10).Column(col =>
                {
                    // --- Ticket Header ---
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(headerCol =>
                        {
                            headerCol.Item().Text("RiadTrip").SemiBold().FontSize(24).FontColor(Colors.Green.Darken2);
                            headerCol.Item().Text("e-Ticket").SemiBold().FontSize(18).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(100).AlignCenter().Column(qrCol =>
                        {
                            qrCol.Item().Text("QR Code").FontSize(8);
                            qrCol.Item().Placeholder();
                        });
                    });

                    col.Item().PaddingTop(10).Text($"Booking ID: {b.Id}").SemiBold().FontSize(14).FontColor(Colors.Blue.Darken2);
                    col.Item().PaddingVertical(15).LineHorizontal(1f).LineColor(Colors.Grey.Lighten1);

                    // --- Passenger Info ---
                    col.Item().Text("Passenger Details").SemiBold().FontSize(14).FontColor(Colors.Black);
                    col.Item().PaddingTop(5).Column(passengerCol =>
                    {
                        passengerCol.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Name: {b.CustomerName}").SemiBold();
                            row.RelativeItem().Text($"Phone: {b.CustomerPhone}").SemiBold();
                        });
                        passengerCol.Item().PaddingTop(5).Text($"Gender: {b.Gender}").SemiBold();
                    });

                    col.Item().PaddingVertical(15).LineHorizontal(1f).LineColor(Colors.Grey.Lighten1);

                    // --- Journey Info ---
                    col.Item().Text("Journey Information").SemiBold().FontSize(14);
                    col.Item().PaddingTop(5).Column(journeyCol =>
                    {
                        journeyCol.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Operator: {s.OperatorName}").SemiBold();
                            row.RelativeItem().Text($"Bus Type: {s.BusType}").SemiBold();
                        });
                        journeyCol.Item().Text($"Route: {s.From} → {s.To}").SemiBold();
                        journeyCol.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Date: {s.JourneyDate:dd MMM yyyy}").SemiBold();
                            row.RelativeItem().Text($"Time: {s.DepartureTime} - {s.ArrivalTime}").SemiBold();
                        });
                    });

                    col.Item().PaddingVertical(15).LineHorizontal(1f).LineColor(Colors.Grey.Lighten1);

                    // --- Payment Breakdown ---
                    col.Item().Text("Payment Details").SemiBold().FontSize(14);
                    col.Item().PaddingTop(5).Column(paymentCol =>
                    {
                        paymentCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Ticket Price");
                            r.AutoItem().Text($"৳{b.TotalFare:0}");
                        });
                        paymentCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Processing Fee");
                            r.AutoItem().Text($"৳{b.ProcessingFee:0}");
                        });
                        if (b.InsuranceFee > 0)
                        {
                            paymentCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Insurance");
                                r.AutoItem().Text($"৳{b.InsuranceFee:0}");
                            });
                        }
                        if (b.Discount > 0)
                        {
                            paymentCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Discount");
                                r.AutoItem().Text($"-৳{b.Discount:0}");
                            });
                        }

                        paymentCol.Item().PaddingTop(8).BorderTop(1).Row(r =>
                        {
                            r.RelativeItem().Text("Total Paid").SemiBold();
                            r.AutoItem().Text($"৳{(b.GrandTotal + b.InsuranceFee):0.##}").SemiBold().FontColor(Colors.Green.Darken2);
                        });
                    });

                    col.Item().PaddingTop(20).Text("Important Notes").FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Item().Text("• Arrive at terminal 30 min before departure.").FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().Text("• Show this ticket to bus conductor.").FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                p.Footer().AlignCenter().Text("Thank you for choosing RiadTrip").FontSize(10);
            });
        }).GeneratePdf();

        return Task.FromResult(bytes);
    }
}
