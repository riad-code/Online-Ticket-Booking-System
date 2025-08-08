using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        public IActionResult Dashboard()
        {
            int totalBuses = _context.Buses.Count();
            ViewData["TotalBuses"] = totalBuses;
            ViewData["TotalUsers"] = _context.Users.Count();
            var totalRoutes = _context.Buses
         .Select(b => b.FullRoute)
         .Distinct()
         .Count();
            ViewData["TotalRoutes"] = totalRoutes;
            return View();
        }

        // ====== Bus Management ======

        public IActionResult ManageBuses()
        {
            var buses = _context.Buses.ToList();
            return View(buses);
        }

        public IActionResult CreateBus()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateBus([FromForm] Bus bus, [FromForm] string DepartureTimeInput, [FromForm] string ArrivalTimeInput)
        {
            ModelState.Remove("DepartureTime");
            ModelState.Remove("ArrivalTime");

            if (TimeSpan.TryParse(DepartureTimeInput, out var depTime))
                bus.DepartureTime = depTime;
            else
                ModelState.AddModelError("DepartureTimeInput", "Invalid Departure Time");

            if (TimeSpan.TryParse(ArrivalTimeInput, out var arrTime))
                bus.ArrivalTime = arrTime;
            else
                ModelState.AddModelError("ArrivalTimeInput", "Invalid Arrival Time");

            if (string.IsNullOrEmpty(bus.BusType))
                ModelState.AddModelError("BusType", "Bus Type is required.");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Where(x => x.Value.Errors.Any())
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray());

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
                bus.DepartureTime = depTime;
            else
                ModelState.AddModelError("DepartureTimeInput", "Invalid Departure Time");

            if (TimeSpan.TryParse(ArrivalTimeInput, out var arrTime))
                bus.ArrivalTime = arrTime;
            else
                ModelState.AddModelError("ArrivalTimeInput", "Invalid Arrival Time");

            if (string.IsNullOrEmpty(bus.BusType))
                ModelState.AddModelError("BusType", "Bus Type is required.");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Where(x => x.Value.Errors.Any())
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray());

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

        // ===== Manage Users =====
        public IActionResult ManageUsers()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
                return Json(new { success = true, message = "User deleted successfully." });

            return Json(new { success = false, message = "Failed to delete user." });
        }
    }
}
