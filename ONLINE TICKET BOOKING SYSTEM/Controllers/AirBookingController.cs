using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models.Air;

using ONLINE_TICKET_BOOKING_SYSTEM.Services;
using ONLINE_TICKET_BOOKING_SYSTEM.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    public class AirBookingController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IAirBookingService _booking;
        private readonly ITicketPdfService _pdf;

        public AirBookingController(ApplicationDbContext db, IAirBookingService booking, ITicketPdfService pdf)
        { _db = db; _booking = booking; _pdf = pdf; }

        // --------- HOLD (called from SearchResults: /Air/Hold?sid=..&date=..&cabin=..&pax=..)
        [HttpGet]
        public async Task<IActionResult> Hold(int sid, string date, string cabin, int pax = 1)
        {
            var schedule = await _db.FlightSchedules
                .Include(s => s.FromAirport).Include(s => s.ToAirport).Include(s => s.Airline)
                .FirstOrDefaultAsync(s => s.Id == sid);
            if (schedule == null) return NotFound();

            var travelDate = DateOnly.Parse(date);

            // build itinerary + 1 segment
            var itin = new Itinerary
            {
                Cabin = Enum.TryParse<CabinClass>(cabin, true, out var c) ? c : CabinClass.Economy,
                Currency = "BDT",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15),
                Segments = new()
                {
                    new FlightSegment { FlightScheduleId = schedule.Id, TravelDate = travelDate }
                }
            };

            // split pax: all adults for now (users later can adjust in passengers page)
            var booking = await _booking.CreateHoldAsync(itin, pax, 0, 0);
            return RedirectToAction(nameof(Passengers), new { pnr = booking.Pnr });
        }

        // --------- PASSENGERS (GET)
        [HttpGet]
        public async Task<IActionResult> Passengers(string pnr)
        {
            var b = await _db.AirBookings
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule)
                    .ThenInclude(fs => fs.Airline)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule)
                    .ThenInclude(fs => fs.FromAirport)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule)
                    .ThenInclude(fs => fs.ToAirport)
                .FirstOrDefaultAsync(x => x.Pnr == pnr);

            if (b == null) return NotFound();

            var fs = b.Itinerary.Segments.First().FlightSchedule;
            var vm = new AirPassengersFormVm
            {
                Pnr = b.Pnr,
                From = fs.FromAirport.IataCode,
                To = fs.ToAirport.IataCode,
                TravelDate = b.Itinerary.Segments.First().TravelDate.ToString("yyyy-MM-dd"),
                Flight = $"{fs.Airline.IataCode} {fs.FlightNumber}",
                Adults = b.Adults,
                Children = b.Children,
                Infants = b.Infants,
                AmountDue = b.AmountDue,
                Currency = b.Currency
            };

            // pre-create rows = Adults
            for (int i = 0; i < b.Adults; i++)
                vm.Passengers.Add(new PaxRow { Type = PaxType.Adult });

            return View(vm);
        }

        // --------- PASSENGERS (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Passengers(AirPassengersFormVm form)
        {
            if (!ModelState.IsValid)
                return View(form);

            var pax = new List<Passenger>();
            foreach (var r in form.Passengers)
            {
                pax.Add(new Passenger
                {
                    FirstName = r.FirstName,
                    LastName = r.LastName,
                    Dob = DateOnly.Parse(r.Dob),
                    Type = r.Type,
                    Gender = r.Gender,
                    PassportNo = r.PassportNo
                });
            }

            await _booking.AttachPassengersAsync(form.Pnr, pax);
            return RedirectToAction(nameof(Pay), new { pnr = form.Pnr });
        }

        // --------- PAY (GET)
        [HttpGet]
        public async Task<IActionResult> Pay(string pnr)
        {
            var b = await _db.AirBookings
                .Include(x => x.Passengers)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule)
                    .ThenInclude(fs => fs.Airline)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule)
                    .ThenInclude(fs => fs.FromAirport)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule)
                    .ThenInclude(fs => fs.ToAirport)
                .FirstOrDefaultAsync(x => x.Pnr == pnr);

            if (b == null) return NotFound();
            return View(b);
        }

        // --------- PAY (POST -> confirm)  [এখানে demo: full due mark paid + ticket]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPay(string pnr)
        {
            var b = await _db.AirBookings.FirstOrDefaultAsync(x => x.Pnr == pnr);
            if (b == null) return NotFound();

            var due = Math.Max(0, b.AmountDue - b.AmountPaid);
            await _booking.MarkPaidAsync(pnr, due);

            return RedirectToAction(nameof(Ticket), new { pnr });
        }

        // --------- Ticket PDF
        [HttpGet]
        public async Task<IActionResult> Ticket(string pnr)
        {
            var b = await _db.AirBookings
                .Include(x => x.Passengers)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule)
                    .ThenInclude(fs => fs.Airline)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule)
                    .ThenInclude(fs => fs.FromAirport)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule)
                    .ThenInclude(fs => fs.ToAirport)
                .FirstOrDefaultAsync(x => x.Pnr == pnr);

            if (b == null) return NotFound();

            var bytes = await _pdf.GenerateAirTicketAsync(b);
            return File(bytes, "application/pdf", $"AirTicket_{pnr}.pdf");
        }
    }
}
