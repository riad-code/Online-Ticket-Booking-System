using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            int totalBuses = _context.Buses.Count(); // Count all buses
            ViewData["TotalBuses"] = totalBuses;
            ViewData["TotalUsers"] = _context.Users.Count();// Pass to view

            return View();
        }


        // ====== Bus Management ======

        public IActionResult ManageBuses()
        {
            var buses = _context.Buses.ToList();
            return View(buses); // ManageBuses.cshtml
        }


        // GET: Admin/CreateBus
        public IActionResult CreateBus()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateBus([FromForm] Bus bus, [FromForm] string DepartureTimeInput, [FromForm] string ArrivalTimeInput)
        {
            // Clear errors on DepartureTime and ArrivalTime to avoid default binder errors
            ModelState.Remove("DepartureTime");
            ModelState.Remove("ArrivalTime");

            // Parse time strings manually
            if (TimeSpan.TryParse(DepartureTimeInput, out var depTime))
            {
                bus.DepartureTime = depTime;
            }
            else
            {
                ModelState.AddModelError("DepartureTimeInput", "Invalid Departure Time");
            }

            if (TimeSpan.TryParse(ArrivalTimeInput, out var arrTime))
            {
                bus.ArrivalTime = arrTime;
            }
            else
            {
                ModelState.AddModelError("ArrivalTimeInput", "Invalid Arrival Time");
            }

            // BusType validation
            if (string.IsNullOrEmpty(bus.BusType))
            {
                ModelState.AddModelError("BusType", "Bus Type is required.");
            }

            // Exclude BusSchedules from validation (add this attribute to Bus model)
            // [ValidateNever] on BusSchedules property

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Where(x => x.Value.Errors.Any())
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(new { message = "Invalid input. Please fill all fields correctly.", errors });
            }

            _context.Buses.Add(bus);
            _context.SaveChanges();

            return Ok(new { message = "Bus created successfully!" });
        }

        [HttpGet]
        public IActionResult EditBus(int id)
        {
            var bus = _context.Buses.Find(id);
            if (bus == null) return NotFound();
            return View(bus);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBus([FromForm] Bus bus, [FromForm] string DepartureTimeInput, [FromForm] string ArrivalTimeInput)
        {
            ModelState.Remove("DepartureTime");
            ModelState.Remove("ArrivalTime");

            if (TimeSpan.TryParse(DepartureTimeInput, out var depTime))
            {
                bus.DepartureTime = depTime;
            }
            else
            {
                ModelState.AddModelError("DepartureTimeInput", "Invalid Departure Time");
            }

            if (TimeSpan.TryParse(ArrivalTimeInput, out var arrTime))
            {
                bus.ArrivalTime = arrTime;
            }
            else
            {
                ModelState.AddModelError("ArrivalTimeInput", "Invalid Arrival Time");
            }

            if (string.IsNullOrEmpty(bus.BusType))
            {
                ModelState.AddModelError("BusType", "Bus Type is required.");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Where(x => x.Value.Errors.Any())
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(new { message = "Invalid input. Please fill all fields correctly.", errors });
            }

            _context.Buses.Update(bus);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Bus updated successfully!" });
        }


        [HttpPost]
        public async Task<IActionResult> DeleteBus(int id)
        {
            var bus = _context.Buses.Find(id);
            if (bus == null)
                return Json(new { success = false, message = "Bus not found." });

            _context.Buses.Remove(bus);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Bus deleted successfully." });
        }
    }
}