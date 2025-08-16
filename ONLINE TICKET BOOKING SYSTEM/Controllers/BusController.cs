using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using ONLINE_TICKET_BOOKING_SYSTEM.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

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
        public async Task<IActionResult> Results(string from, string to, DateTime journeyDate, string? returnDate, string tripType)
        {
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            {
                // Also build sidebar universes even if no route entered, so filters still show full data
                var emptyVm = new BusSearchResultViewModel
                {
                    AvailableBuses = new List<BusSchedule>()
                };

                await FillSidebarUniversesAsync(emptyVm);
                return View("SearchResults", emptyVm);
            }

            var fromNorm = from.Trim().ToLower();
            var toNorm = to.Trim().ToLower();

            var outboundBase = await _context.Buses
                .Where(b => b.From.ToLower().Contains(fromNorm) &&
                            b.To.ToLower().Contains(toNorm))
                .OrderBy(b => b.OperatorName).ThenBy(b => b.DepartureTime)
                .ToListAsync();

            var ensuredOutbound = new List<BusSchedule>();
            foreach (var b in outboundBase)
            {
                var s = await EnsureScheduleAsync(b, journeyDate.Date);
                ensuredOutbound.Add(s);
            }
            ensuredOutbound = ensuredOutbound.OrderBy(s => s.DepartureTime).ToList();

            List<BusSchedule>? returnEnsured = null;
            var tripTypeNormalized = tripType?.ToLower().Replace(" ", "");
            if (tripTypeNormalized == "roundway" && !string.IsNullOrWhiteSpace(returnDate))
            {
                var retDate = DateTime.Parse(returnDate).Date;
                var returnBase = await _context.Buses
                    .Where(b => b.From.ToLower().Contains(toNorm) &&
                                b.To.ToLower().Contains(fromNorm))
                    .OrderBy(b => b.OperatorName).ThenBy(b => b.DepartureTime)
                    .ToListAsync();

                returnEnsured = new List<BusSchedule>();
                foreach (var b in returnBase)
                {
                    var s = await EnsureScheduleAsync(b, retDate);
                    returnEnsured.Add(s);
                }
                returnEnsured = returnEnsured.OrderBy(s => s.DepartureTime).ToList();
            }

            var vm = new BusSearchResultViewModel
            {
                From = from,
                To = to,
                JourneyDate = journeyDate.Date,
                ReturnDate = string.IsNullOrEmpty(returnDate) ? null : DateTime.Parse(returnDate).Date,
                TripType = tripType,
                AvailableBuses = ensuredOutbound,
                ReturnBuses = returnEnsured
            };

            // ✅ Populate left sidebar universes from the FULL database
            await FillSidebarUniversesAsync(vm);

            return View("SearchResults", vm);
        }

        private async Task FillSidebarUniversesAsync(BusSearchResultViewModel vm)
        {
            // Operators: distinct from the whole Buses table
            vm.AllOperators = await _context.Buses
                .AsNoTracking()
                .Select(b => b.OperatorName)
                .Where(s => s != null && s != "")
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            // Boarding/Dropping: fetch raw CSVs first (server-side), then split on client memory
            var allBoardingStrings = await _context.Buses
                .AsNoTracking()
                .Select(b => b.BoardingPointsString)
                .Where(s => s != null && s != "")
                .ToListAsync();

            var allDroppingStrings = await _context.Buses
                .AsNoTracking()
                .Select(b => b.DroppingPointsString)
                .Where(s => s != null && s != "")
                .ToListAsync();

            vm.AllBoardingPoints = allBoardingStrings
                .SelectMany(s => s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

            vm.AllDroppingPoints = allDroppingStrings
                .SelectMany(s => s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task<BusSchedule> EnsureScheduleAsync(Bus b, DateTime journeyDate)
        {
            var sched = await _context.BusSchedules
                .FirstOrDefaultAsync(s => s.BusId == b.Id && s.JourneyDate == journeyDate);

            if (sched == null)
            {
                sched = new BusSchedule
                {
                    BusId = b.Id,
                    From = b.From,
                    To = b.To,
                    FullRoute = b.FullRoute,
                    JourneyDate = journeyDate,
                    DepartureTime = b.DepartureTime,
                    ArrivalTime = b.ArrivalTime,
                    BusType = b.BusType,
                    OperatorName = b.OperatorName,
                    Fare = b.Fare,
                    SeatsAvailable = b.SeatsAvailable,
                    BoardingPointsString = b.BoardingPointsString,
                    DroppingPointsString = b.DroppingPointsString,
                    IsBlocked = b.IsBlocked // if you use whole-schedule blocking
                };

                _context.BusSchedules.Add(sched);
                await _context.SaveChangesAsync(); // gets sched.Id

                // Seed seats
                if (!await _context.ScheduleSeats.AnyAsync(x => x.BusScheduleId == sched.Id))
                {
                    var cols = new[] { "A", "B", "C", "D" };
                    var names = new List<string>();
                    for (int r = 1; r <= 10; r++)
                        foreach (var c in cols) names.Add($"{c}{r}");

                    var seats = names.Select(n => new ScheduleSeat
                    {
                        BusScheduleId = sched.Id,
                        SeatNo = n,
                        Status = SeatStatus.Available
                    }).ToList();

                    _context.ScheduleSeats.AddRange(seats);
                    await _context.SaveChangesAsync();

                    // ✅ Apply Admin blocked seats immediately on creation
                    var layout = await _context.SeatLayouts.AsNoTracking().FirstOrDefaultAsync(x => x.BusId == b.Id);
                    if (layout != null && !string.IsNullOrWhiteSpace(layout.BlockedSeatsCsv))
                    {
                        var blocked = layout.BlockedSeatsCsv
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(s => s.ToUpperInvariant())
                            .ToHashSet();

                        var toBlock = await _context.ScheduleSeats
                            .Where(s => s.BusScheduleId == sched.Id &&
                                        blocked.Contains(s.SeatNo.ToUpper()) &&
                                        s.Status == SeatStatus.Available)
                            .ToListAsync();

                        if (toBlock.Count > 0)
                        {
                            toBlock.ForEach(s => s.Status = SeatStatus.Blocked);
                            _context.ScheduleSeats.UpdateRange(toBlock);
                            await _context.SaveChangesAsync();
                        }
                    }

                    // refresh count
                    sched.SeatsAvailable = await _context.ScheduleSeats
                        .CountAsync(x => x.BusScheduleId == sched.Id && x.Status == SeatStatus.Available);
                    _context.BusSchedules.Update(sched);
                    await _context.SaveChangesAsync();
                }
            }

            return sched;
        }

        // ===== Autocomplete endpoints (unchanged) =====

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
