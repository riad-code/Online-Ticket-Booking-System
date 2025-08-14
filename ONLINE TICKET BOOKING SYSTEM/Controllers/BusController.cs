using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using ONLINE_TICKET_BOOKING_SYSTEM.ViewModels;
using System;
using System.Linq;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    public class BusController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BusController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(); 
        }

        [HttpGet]
        public IActionResult Results(string from, string to, DateTime journeyDate, string? returnDate, string tripType)
        {
            // Normalize inputs
            var fromNorm = (from ?? "").Trim().ToLower();
            var toNorm = (to ?? "").Trim().ToLower();

            // 1) OUTBOUND: find matching buses by route only (no date filter)
            var outboundBase = _context.Buses
                .Where(b => b.From.ToLower().Contains(fromNorm) &&
                            b.To.ToLower().Contains(toNorm))
                .OrderBy(b => b.OperatorName).ThenBy(b => b.DepartureTime)
                .ToList();

            // Materialize "virtual" schedules for the requested journeyDate
            var buses = outboundBase.Select(b => new BusSchedule
            {
                BusId = b.Id,
                From = b.From,
                To = b.To,
                FullRoute = b.FullRoute,
                JourneyDate = journeyDate.Date,     // << user-selected date
                ReturnDate = null,
                DepartureTime = b.DepartureTime,
                ArrivalTime = b.ArrivalTime,
                BusType = b.BusType,
                OperatorName = b.OperatorName,
                Fare = b.Fare,
                SeatsAvailable = b.SeatsAvailable
            }).OrderBy(s => s.DepartureTime).ToList();

            // 2) RETURN: only when requested
            List<BusSchedule>? returnBuses = null;
            var tripTypeNormalized = tripType?.ToLower().Replace(" ", "");
            if (tripTypeNormalized == "roundway" && !string.IsNullOrWhiteSpace(returnDate))
            {
                var retDate = DateTime.Parse(returnDate).Date;

                var returnBase = _context.Buses
                    .Where(b => b.From.ToLower().Contains(toNorm) &&
                                b.To.ToLower().Contains(fromNorm))
                    .OrderBy(b => b.OperatorName).ThenBy(b => b.DepartureTime)
                    .ToList();

                returnBuses = returnBase.Select(b => new BusSchedule
                {
                    BusId = b.Id,
                    From = b.From,            // naturally: to→from route in data
                    To = b.To,
                    FullRoute = b.FullRoute,
                    JourneyDate = retDate,           // << user-selected return date
                    ReturnDate = null,
                    DepartureTime = b.DepartureTime,
                    ArrivalTime = b.ArrivalTime,
                    BusType = b.BusType,
                    OperatorName = b.OperatorName,
                    Fare = b.Fare,
                    SeatsAvailable = b.SeatsAvailable
                }).OrderBy(s => s.DepartureTime).ToList();
            }

            var vm = new BusSearchResultViewModel
            {
                From = from,
                To = to,
                JourneyDate = journeyDate.Date,
                ReturnDate = string.IsNullOrEmpty(returnDate) ? null : DateTime.Parse(returnDate).Date,
                TripType = tripType,
                AvailableBuses = buses,
                ReturnBuses = returnBuses
            };

            return View(vm);
        }




        // Autocomplete for "From"
        // Autocomplete for "From"
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetFromSuggestions(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Ok(new List<string>());

            var t = term.Trim();
            var suggestions = _context.Buses
                .Where(b => EF.Functions.Like(b.From, $"%{t}%"))
                .Select(b => b.From)
                .Distinct()
                .Take(10)
                .ToList();

            return Ok(suggestions);
        }

        // Autocomplete for "To"
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetToSuggestions(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Ok(new List<string>());

            var t = term.Trim();
            var suggestions = _context.Buses
                .Where(b => EF.Functions.Like(b.To, $"%{t}%"))
                .Select(b => b.To)
                .Distinct()
                .Take(10)
                .ToList();

            return Ok(suggestions);
        }

        [HttpGet]
        public JsonResult GetLocationSuggestions(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<string>());

            term = term.Trim().ToLower();

            var fromLocations = _context.Buses
                .Where(b => b.From.ToLower().StartsWith(term))
                .Select(b => b.From);

            var toLocations = _context.Buses
                .Where(b => b.To.ToLower().StartsWith(term))
                .Select(b => b.To);

            var locations = fromLocations
                .Union(toLocations)
                .Distinct()
                .Take(10)
                .ToList();

            return Json(locations);
        }



    }
}
