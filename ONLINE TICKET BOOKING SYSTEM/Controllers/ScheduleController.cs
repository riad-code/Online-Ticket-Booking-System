using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using ONLINE_TICKET_BOOKING_SYSTEM.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ScheduleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Index: list schedules
        public IActionResult Index()
        {
            var schedules = _context.BusSchedules
                .Include(s => s.Bus)
                .Include(s => s.ReturnBus)
                .OrderBy(s => s.JourneyDate)
                .ThenBy(s => s.DepartureTime)
                .ToList();

            return View(schedules);
        }

        // GET: Create
        public IActionResult Create()
        {
            var vm = new CreateScheduleViewModel
            {
                JourneyDate = DateTime.Today,
                AllBuses = _context.Buses.ToList()
            };
            return View(vm);
        }

        // POST: Create - AJAX POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateScheduleViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.AllBuses = _context.Buses.ToList();
                // For AJAX, return validation errors as JSON
                return BadRequest(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            }

            var bus = _context.Buses.FirstOrDefault(b => b.Id == vm.BusId);
            if (bus == null)
            {
                return BadRequest("Selected bus not found.");
            }

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
                SeatsAvailable = vm.SeatsAvailable
            };

            if (vm.ReturnBusId.HasValue)
            {
                var rbus = _context.Buses.FirstOrDefault(b => b.Id == vm.ReturnBusId.Value);
                if (rbus != null)
                {
                    schedule.ReturnBusId = vm.ReturnBusId;
                }
            }

            _context.BusSchedules.Add(schedule);
            _context.SaveChanges();

            // Return success JSON for AJAX
            return Json(new { success = true, message = "Schedule created successfully." });
        }

        // GET: Edit
        public IActionResult Edit(int id)
        {
            var sched = _context.BusSchedules.Find(id);
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

        // POST: Edit - AJAX POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CreateScheduleViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.AllBuses = _context.Buses.ToList();
                return BadRequest(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            }

            var sched = _context.BusSchedules.Find(vm.Id);
            if (sched == null) return NotFound();

            var bus = _context.Buses.FirstOrDefault(b => b.Id == vm.BusId);
            if (bus == null)
            {
                return BadRequest("Selected bus not found.");
            }

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

            if (vm.ReturnBusId.HasValue)
            {
                var rbus = _context.Buses.FirstOrDefault(b => b.Id == vm.ReturnBusId.Value);
                if (rbus != null)
                    sched.ReturnBusId = vm.ReturnBusId;
            }
            else
            {
                sched.ReturnBusId = null;
            }

            _context.BusSchedules.Update(sched);
            _context.SaveChanges();

            return Json(new { success = true, message = "Schedule updated successfully." });
        }

        // Delete - AJAX POST
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var sched = _context.BusSchedules.Find(id);
            if (sched == null)
                return Json(new { success = false, message = "Schedule not found." });

            _context.BusSchedules.Remove(sched);
            _context.SaveChanges();

            return Json(new { success = true, message = "Schedule deleted successfully." });
        }

        // Search for front-end booking (optional)
        [HttpPost]
        public IActionResult Search(string from, string to, DateTime journeyDate, DateTime? returnDate)
        {
            var available = _context.BusSchedules
                .Where(s => s.From == from && s.To == to && s.JourneyDate.Date == journeyDate.Date)
                .Include(s => s.Bus)
                .ToList();

            List<BusSchedule> returnBuses = new List<BusSchedule>();
            if (returnDate.HasValue)
            {
                returnBuses = _context.BusSchedules
                    .Where(s => s.From == to && s.To == from && s.JourneyDate.Date == returnDate.Value.Date)
                    .Include(s => s.Bus)
                    .ToList();
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
