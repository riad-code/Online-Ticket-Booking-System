using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
                p.DefaultTextStyle(x => x.FontSize(12));

                p.Header().Text("RiadTrip e-Ticket")
                    .SemiBold().FontSize(18).FontColor(Colors.Green.Darken2);

                p.Content().Column(col =>
                {
                    col.Item().Text($"Booking ID: {b.Id}");
                    col.Item().Text($"Passenger: {b.CustomerName} ({b.CustomerPhone}) | Gender: {b.Gender}");
                    col.Item().Text($"Operator: {s.OperatorName} | Bus: {s.BusType}");
                    col.Item().Text($"Route: {s.From} → {s.To}");
                    col.Item().Text($"Date: {s.JourneyDate:dd MMM yyyy} | Time: {s.DepartureTime} - {s.ArrivalTime}");
                    col.Item().Text($"Seats: {seats}");
                    col.Item().PaddingTop(10).LineHorizontal(0.5f);
                    col.Item().Text($"Paid: ৳{b.GrandTotal:0}");
                });

                p.Footer().AlignCenter().Text("Thanks for choosing RiadTrip").FontSize(10);
            });
        }).GeneratePdf();

        return Task.FromResult(bytes);
    }
}
