using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using ONLINE_TICKET_BOOKING_SYSTEM.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ScheduleController(ApplicationDbContext context)
        {
            _context = context;
        }

       
        public async Task<IActionResult> Index()
        {
            var schedules = await _context.BusSchedules
                .Include(s => s.Bus)
                .Include(s => s.ReturnBus)
                .OrderBy(s => s.JourneyDate)
                .ThenBy(s => s.DepartureTime)
                .ToListAsync();

            return View(schedules);
        }

       
        public IActionResult Create()
        {
            var vm = new CreateScheduleViewModel
            {
                JourneyDate = DateTime.Today,
                AllBuses = _context.Buses.ToList()
            };
            return View(vm);
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateScheduleViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.AllBuses = _context.Buses.ToList();
                var errs = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(string.Join("; ", errs));
            }

            var bus = await _context.Buses.FirstOrDefaultAsync(b => b.Id == vm.BusId);
            if (bus == null)
                return BadRequest("Selected bus not found.");

            var schedule = new BusSchedule
            {
                BusId = vm.BusId,
                From = bus.From,
                To = bus.To,
                FullRoute = bus.FullRoute,
                JourneyDate = vm.JourneyDate.Date,
                ReturnDate = vm.ReturnDate?.Date,
                DepartureTime = vm.DepartureTime,
                ArrivalTime = vm.ArrivalTime,
                BusType = bus.BusType,
                OperatorName = bus.OperatorName,
                Fare = vm.Fare,
                SeatsAvailable = vm.SeatsAvailable, 
                BoardingPointsString = bus.BoardingPointsString,
                DroppingPointsString = bus.DroppingPointsString
            };

            if (vm.ReturnBusId.HasValue)
            {
                var rbus = await _context.Buses.FirstOrDefaultAsync(b => b.Id == vm.ReturnBusId.Value);
                if (rbus != null)
                    schedule.ReturnBusId = vm.ReturnBusId;
            }

            _context.BusSchedules.Add(schedule);
            await _context.SaveChangesAsync(); 

          
            if (!await _context.ScheduleSeats.AnyAsync(x => x.BusScheduleId == schedule.Id))
            {
                var layout = await _context.SeatLayouts
                                           .FirstOrDefaultAsync(x => x.BusId == schedule.BusId);

                var names = new List<string>();
                if (!string.IsNullOrWhiteSpace(layout?.LayoutJson))
                {
                    try
                    {
                        names = System.Text.Json.JsonSerializer
                                    .Deserialize<List<string>>(layout.LayoutJson) ?? new();
                    }
                    catch
                    {
                        
                    }
                }

                if (names.Count == 0)
                {
                   
                    var cols = new[] { "A", "B", "C", "D" };
                    for (int r = 1; r <= 10; r++)
                    {
                        foreach (var c in cols)
                        {
                            names.Add($"{c}{r}");
                        }
                    }
                }

                var blocked = new HashSet<string>(
                    (layout?.BlockedSeatsCsv ?? "")
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                    StringComparer.OrdinalIgnoreCase);

                var seats = names.Select(n => new ScheduleSeat
                {
                    BusScheduleId = schedule.Id,
                    SeatNo = n,
                    Status = blocked.Contains(n) ? SeatStatus.Blocked : SeatStatus.Available
                }).ToList();

                _context.ScheduleSeats.AddRange(seats);

                schedule.SeatsAvailable = seats.Count(s => s.Status == SeatStatus.Available);
                _context.BusSchedules.Update(schedule);

                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Schedule created successfully." });
        }

     
        public async Task<IActionResult> Edit(int id)
        {
            var sched = await _context.BusSchedules.FindAsync(id);
            if (sched == null) return NotFound();

            var vm = new CreateScheduleViewModel
            {
                Id = sched.Id,
                BusId = sched.BusId ?? 0,
                ReturnBusId = sched.ReturnBusId,
                JourneyDate = sched.JourneyDate,
                ReturnDate = sched.ReturnDate,
                DepartureTime = sched.DepartureTime,
                ArrivalTime = sched.ArrivalTime,
                Fare = sched.Fare,
                SeatsAvailable = sched.SeatsAvailable,
                From = sched.From,
                To = sched.To,
                FullRoute = sched.FullRoute,
                AllBuses = _context.Buses.ToList()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CreateScheduleViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.AllBuses = _context.Buses.ToList();
                var errs = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(string.Join("; ", errs));
            }

            var sched = await _context.BusSchedules.FindAsync(vm.Id);
            if (sched == null) return NotFound();

            var bus = await _context.Buses.FirstOrDefaultAsync(b => b.Id == vm.BusId);
            if (bus == null)
                return BadRequest("Selected bus not found.");

          
            sched.BusId = vm.BusId;
            sched.From = bus.From;
            sched.To = bus.To;
            sched.FullRoute = bus.FullRoute;
            sched.DepartureTime = vm.DepartureTime;
            sched.ArrivalTime = vm.ArrivalTime;
            sched.JourneyDate = vm.JourneyDate.Date;
            sched.ReturnDate = vm.ReturnDate?.Date;
            sched.BusType = bus.BusType;
            sched.OperatorName = bus.OperatorName;
            sched.Fare = vm.Fare;
            sched.SeatsAvailable = vm.SeatsAvailable;
            sched.BoardingPointsString = bus.BoardingPointsString;
            sched.DroppingPointsString = bus.DroppingPointsString;

            if (vm.ReturnBusId.HasValue)
            {
                var rbus = await _context.Buses.FirstOrDefaultAsync(b => b.Id == vm.ReturnBusId.Value);
                sched.ReturnBusId = (rbus != null) ? vm.ReturnBusId : null;
            }
            else
            {
                sched.ReturnBusId = null;
            }

          
            if (sched.BusId.HasValue)
            {
                var baseBus = await _context.Buses.FirstOrDefaultAsync(b => b.Id == sched.BusId.Value);
                if (baseBus != null)
                {
                    baseBus.DepartureTime = sched.DepartureTime;
                    baseBus.ArrivalTime = sched.ArrivalTime;
                    baseBus.Fare = sched.Fare;
                    baseBus.SeatsAvailable = sched.SeatsAvailable;
                    baseBus.BoardingPointsString = sched.BoardingPointsString;
                    baseBus.DroppingPointsString = sched.DroppingPointsString;

                    _context.Buses.Update(baseBus);
                }
            }

            _context.BusSchedules.Update(sched);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Schedule updated successfully." });
        }

      
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var sched = await _context.BusSchedules.FindAsync(id);
            if (sched == null)
                return Json(new { success = false, message = "Schedule not found." });

            _context.BusSchedules.Remove(sched);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Schedule deleted successfully." });
        }

      
        [HttpPost]
        public async Task<IActionResult> Search(string from, string to, DateTime journeyDate, DateTime? returnDate)
        {
            var available = await _context.BusSchedules
                .Where(s => s.From == from && s.To == to && s.JourneyDate.Date == journeyDate.Date)
                .Include(s => s.Bus)
                .ToListAsync();

            List<BusSchedule> returnBuses = new List<BusSchedule>();
            if (returnDate.HasValue)
            {
                returnBuses = await _context.BusSchedules
                    .Where(s => s.From == to && s.To == from && s.JourneyDate.Date == returnDate.Value.Date)
                    .Include(s => s.Bus)
                    .ToListAsync();
            }

            var vm = new BusSearchResultViewModel
            {
                From = from,
                To = to,
                JourneyDate = journeyDate,
                ReturnDate = returnDate,
                AvailableBuses = available,
                ReturnBuses = returnBuses
            };

            return View("SearchResults", vm);
        }
    }
}
