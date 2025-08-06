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
            var buses = _context.BusSchedules
                .Where(b =>
                    b.From.ToLower().Contains(from.ToLower().Trim()) &&
                    b.To.ToLower().Contains(to.ToLower().Trim()) &&
                    b.JourneyDate.Date == journeyDate.Date)
                .OrderBy(b => b.JourneyDate)
                .ThenBy(b => b.DepartureTime)
                .ToList();

            List<BusSchedule>? returnBuses = null;

            var tripTypeNormalized = tripType?.ToLower().Replace(" ", "");
            if (tripTypeNormalized == "roundway" && !string.IsNullOrEmpty(returnDate))

            {
                DateTime retDate = DateTime.Parse(returnDate);
                returnBuses = _context.BusSchedules
                    .Where(b =>
                        b.From.ToLower().Contains(to.ToLower().Trim()) &&
                        b.To.ToLower().Contains(from.ToLower().Trim()) &&
                        b.JourneyDate.Date == retDate.Date)
                    .OrderBy(b => b.JourneyDate)
                    .ThenBy(b => b.DepartureTime)
                    .ToList();
            }

            var vm = new BusSearchResultViewModel
            {
                From = from,
                To = to,
                JourneyDate = journeyDate,
                ReturnDate = string.IsNullOrEmpty(returnDate) ? null : DateTime.Parse(returnDate),
                TripType = tripType,
                AvailableBuses = buses,
                ReturnBuses = returnBuses
            };

            return View(vm);
        }





        // Autocomplete for "From"
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetFromSuggestions(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Ok(new List<string>());

            var suggestions = _context.BusSchedules
                .Where(b => EF.Functions.Like(b.From, $"%{term}%"))
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

            var suggestions = _context.BusSchedules
                .Where(b => EF.Functions.Like(b.To, $"%{term}%"))
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

            term = term.ToLower();

            var fromLocations = _context.BusSchedules
                .Where(b => b.From.ToLower().StartsWith(term))
                .Select(b => b.From);

            var toLocations = _context.BusSchedules
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
