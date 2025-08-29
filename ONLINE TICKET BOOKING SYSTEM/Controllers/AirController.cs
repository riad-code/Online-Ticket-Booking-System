using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models.Air;
using ONLINE_TICKET_BOOKING_SYSTEM.ViewModels;
using ONLINE_TICKET_BOOKING_SYSTEM.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    public class AirController : Controller
    {
        private readonly ApplicationDbContext _db;
        public AirController(ApplicationDbContext db) => _db = db;

        // ========== Index ==========
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var airports = await _db.Airports.AsNoTracking()
                                             .OrderBy(a => a.City)
                                             .ToListAsync();
            return View(airports);
        }

        // ========== Results ==========
        [HttpGet]
        public async Task<IActionResult> Results(
            string from,
            string to,
            DateTime journeyDate,
            string? returnDate,
            string tripType = "oneway",
            int travellers = 1,
            string cabin = "Economy")
        {
            // normalize + parse IATA like "Dhaka (DAC)"
            static string ExtractIata(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return "";
                var i1 = s.IndexOf('(');
                var i2 = s.IndexOf(')');
                if (i1 >= 0 && i2 > i1) return s.Substring(i1 + 1, i2 - i1 - 1).Trim().ToUpper();
                return s.Trim().ToUpper();
            }

            var fromIata = ExtractIata(from);
            var toIata = ExtractIata(to);

            if (string.IsNullOrEmpty(fromIata) || string.IsNullOrEmpty(toIata))
            {
                var emptyVm = new AirSearchResultViewModel
                {
                    AvailableFlights = new List<FlightCardVm>(),
                    TripType = tripType,
                    From = from ?? "",
                    To = to ?? "",
                    JourneyDate = journeyDate.Date
                };
                await FillSidebarAsync(emptyVm);
                return View("SearchResults", emptyVm);
            }

            var fromAirport = await _db.Airports.AsNoTracking().FirstOrDefaultAsync(a => a.IataCode == fromIata);
            var toAirport = await _db.Airports.AsNoTracking().FirstOrDefaultAsync(a => a.IataCode == toIata);

            if (fromAirport == null || toAirport == null)
            {
                var emptyVm = new AirSearchResultViewModel
                {
                    AvailableFlights = new List<FlightCardVm>(),
                    TripType = tripType,
                    From = from,
                    To = to,
                    JourneyDate = journeyDate.Date
                };
                await FillSidebarAsync(emptyVm);
                return View("SearchResults", emptyVm);
            }

            var depDOW = (int)journeyDate.DayOfWeek; // Sun=0 .. Sat=6

            // ❌ local function বাদ
            // ✅ inline bitmask check ব্যবহার করা হলো
            var outboundSchedules = await _db.FlightSchedules
                .AsNoTracking()
                .Include(s => s.Airline)
                .Include(s => s.FromAirport)
                .Include(s => s.ToAirport)
                .Where(s =>
                    s.FromAirport.IataCode == fromIata &&
                    s.ToAirport.IataCode == toIata &&
                    ((s.OperatingDaysMask & (1 << depDOW)) != 0 || s.OperatingDaysMask == 127)
                 )
                .OrderBy(s => s.Airline.Name).ThenBy(s => s.DepTimeLocal)
                .ToListAsync();

            var availableOutbound = await MapToCardsAsync(outboundSchedules, cabin, travellers, journeyDate);

            // Return (if applicable)
            List<FlightCardVm>? availableReturn = null;
            var trip = (tripType ?? "oneway").ToLower().Replace(" ", "");
            if (trip == "roundway" && !string.IsNullOrWhiteSpace(returnDate))
            {
                var rdate = DateTime.Parse(returnDate).Date;
                var retDOW = (int)rdate.DayOfWeek;

                var returnSchedules = await _db.FlightSchedules
                    .AsNoTracking()
                    .Include(s => s.Airline)
                    .Include(s => s.FromAirport)
                    .Include(s => s.ToAirport)
                    .Where(s =>
                        s.FromAirport.IataCode == toIata &&
                        s.ToAirport.IataCode == fromIata &&
                        ((s.OperatingDaysMask & (1 << retDOW)) != 0 || s.OperatingDaysMask == 127)
                     )
                    .OrderBy(s => s.Airline.Name).ThenBy(s => s.DepTimeLocal)
                    .ToListAsync();

                availableReturn = await MapToCardsAsync(returnSchedules, cabin, travellers, rdate);
            }

            var vm = new AirSearchResultViewModel
            {
                From = $"{fromAirport.City} ({fromAirport.IataCode})",
                To = $"{toAirport.City} ({toAirport.IataCode})",
                JourneyDate = journeyDate.Date,
                ReturnDate = string.IsNullOrEmpty(returnDate) ? (DateTime?)null : DateTime.Parse(returnDate).Date,
                TripType = tripType,
                Cabin = cabin,
                Travellers = travellers,
                AvailableFlights = availableOutbound,
                ReturnFlights = availableReturn
            };

            await FillSidebarAsync(vm);
            return View("SearchResults", vm);
        }

        // ========== Autocomplete ==========
        [HttpGet]
        [Produces("application/json")]
        public async Task<IActionResult> AirportSuggestions(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return Ok(new List<string>());
            term = term.Trim();

            var list = await _db.Airports.AsNoTracking()
                .Where(a => a.City.Contains(term) || a.IataCode.Contains(term) || a.Name.Contains(term))
                .OrderBy(a => a.City)
                .Select(a => a.City + " (" + a.IataCode + ")")
                .Take(12)
                .ToListAsync();

            return Ok(list);
        }

        // ===== helpers =====
        private async Task<List<FlightCardVm>> MapToCardsAsync(List<FlightSchedule> schedules, string cabin, int travellers, DateTime date)
        {
            var cards = new List<FlightCardVm>();

            foreach (var s in schedules)
            {
                // pick cheapest fare in desired cabin (fallback: any cabin)
                var fares = await _db.FareClasses.AsNoTracking()
                    .Where(f => f.FlightScheduleId == s.Id)
                    .ToListAsync();

                var wantedCabin = Enum.TryParse<CabinClass>(cabin, true, out var c) ? c : CabinClass.Economy;

                var pick = fares.Where(f => f.Cabin == wantedCabin && f.SeatsAvailable >= travellers)
                                .OrderBy(f => f.BaseFare + f.TaxesAndFees)
                                .FirstOrDefault()
                          ?? fares.OrderBy(f => f.BaseFare + f.TaxesAndFees)
                                  .FirstOrDefault();

                if (pick == null) continue;

                var dep = s.DepTimeLocal;   // TimeOnly
                var arr = s.ArrTimeLocal;   // TimeOnly

                // ✅ Duration calc (no ToTimeSpan on TimeSpan!)
                var diff = arr.ToTimeSpan() - dep.ToTimeSpan();
                if (diff.TotalMinutes < 0) diff += TimeSpan.FromDays(1);
                var durMin = s.DurationMinutes > 0 ? s.DurationMinutes : (int)diff.TotalMinutes;

                var vm = new FlightCardVm
                {
                    ScheduleId = s.Id,
                    AirlineCode = s.Airline.IataCode,
                    AirlineName = s.Airline.Name,
                    FlightNumber = s.FlightNumber,
                    From = s.FromAirport.IataCode,
                    To = s.ToAirport.IataCode,
                    DepTime = dep.ToString("HH:mm"),
                    ArrTime = arr.ToString("HH:mm"),
                    Duration = $"{durMin / 60}h {durMin % 60}m",
                    Cabin = pick.Cabin.ToString(),
                    Currency = string.IsNullOrWhiteSpace(pick.Currency) ? "BDT" : pick.Currency,
                    Price = (pick.BaseFare + pick.TaxesAndFees) * travellers,
                    SeatsAvailable = pick.SeatsAvailable,
                    TravelDate = date.ToString("yyyy-MM-dd")
                };
                cards.Add(vm);
            }

            return cards.OrderBy(v => v.DepTime).ToList();
        }

        private async Task FillSidebarAsync(AirSearchResultViewModel vm)
        {
            vm.AllAirlines = await _db.Airlines.AsNoTracking()
                .Select(a => a.Name)
                .OrderBy(n => n)
                .ToListAsync();
        }
    }
}
