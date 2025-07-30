using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
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
            return View(); // Your main search page
        }

        [HttpGet]
        public IActionResult Results(string from, string to, DateTime journeyDate, string? returnDate, string tripType)
        {
            // Use Contains + trim + ToLower() for flexible search
            var buses = _context.BusSchedules
                .Where(b =>
                    b.From.ToLower().Contains(from.ToLower().Trim())
                    && b.To.ToLower().Contains(to.ToLower().Trim())
                    && b.JourneyDate.Date == journeyDate.Date)
                .OrderBy(b => b.DepartureTime)
                .ToList();

            var vm = new BusSearchResultViewModel
            {
                From = from,
                To = to,
                JourneyDate = journeyDate,
                ReturnDate = string.IsNullOrEmpty(returnDate) ? null : DateTime.Parse(returnDate),
                TripType = tripType,
                AvailableBuses = buses
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

    }
}
