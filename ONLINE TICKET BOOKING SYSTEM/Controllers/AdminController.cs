using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewData["TotalBuses"] = await _context.Buses.CountAsync();
            ViewData["TotalUsers"] = await _context.Users.CountAsync();
            ViewData["TotalRoutes"] = await _context.Buses
                .Select(b => b.FullRoute)
                .Distinct()
                .CountAsync();

            // Count tickets sold "today" in UTC
            var startUtc = DateTime.UtcNow.Date;        // 00:00 UTC today
            var endUtc = startUtc.AddDays(1);         // 00:00 UTC tomorrow

            var ticketsSoldToday = await _context.BookingSeats
                .AsNoTracking()
                .Where(bs =>
                    bs.Booking.PaymentStatus == PaymentStatus.Paid &&
                    bs.Booking.Status == BookingStatus.Approved &&
                    bs.Booking.CreatedAtUtc >= startUtc &&
                    bs.Booking.CreatedAtUtc < endUtc)
                .CountAsync();

            ViewData["TicketsSoldToday"] = ticketsSoldToday;

            return View();
        }

        // ====== Bus Management ======
        public IActionResult ManageBuses()
        {
            var buses = _context.Buses.ToList();
            return View(buses);
        }

        public IActionResult CreateBus() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateBus([FromForm] Bus bus, [FromForm] string DepartureTimeInput, [FromForm] string ArrivalTimeInput)
        {
            ModelState.Remove(nameof(Bus.DepartureTime));
            ModelState.Remove(nameof(Bus.ArrivalTime));

            if (TimeSpan.TryParse(DepartureTimeInput, out var depTime))
                bus.DepartureTime = depTime;
            else
                ModelState.AddModelError("DepartureTimeInput", "Invalid Departure Time");

            if (TimeSpan.TryParse(ArrivalTimeInput, out var arrTime))
                bus.ArrivalTime = arrTime;
            else
                ModelState.AddModelError("ArrivalTimeInput", "Invalid Arrival Time");

            if (string.IsNullOrWhiteSpace(bus.BusType))
                ModelState.AddModelError(nameof(Bus.BusType), "Bus Type is required.");

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
            ModelState.Remove(nameof(Bus.DepartureTime));
            ModelState.Remove(nameof(Bus.ArrivalTime));

            if (TimeSpan.TryParse(DepartureTimeInput, out var depTime))
                bus.DepartureTime = depTime;
            else
                ModelState.AddModelError("DepartureTimeInput", "Invalid Departure Time");

            if (TimeSpan.TryParse(ArrivalTimeInput, out var arrTime))
                bus.ArrivalTime = arrTime;
            else
                ModelState.AddModelError("ArrivalTimeInput", "Invalid Arrival Time");

            if (string.IsNullOrWhiteSpace(bus.BusType))
                ModelState.AddModelError(nameof(Bus.BusType), "Bus Type is required.");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Where(x => x.Value.Errors.Any())
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray());
                return BadRequest(new { message = "Invalid input. Please fill all fields correctly.", errors });
            }

            // Update the bus itself
            _context.Buses.Update(bus);

            // === SYNC: Bus -> all its schedules ===
            var schedules = await _context.BusSchedules
                .Where(s => s.BusId == bus.Id)
                .ToListAsync();

            foreach (var s in schedules)
            {
                s.From = bus.From;
                s.To = bus.To;
                s.FullRoute = bus.FullRoute;

                s.DepartureTime = bus.DepartureTime;
                s.ArrivalTime = bus.ArrivalTime;

                s.BusType = bus.BusType;
                s.OperatorName = bus.OperatorName;

                s.Fare = bus.Fare;
                s.SeatsAvailable = bus.SeatsAvailable;

                s.BoardingPointsString = bus.BoardingPointsString;
                s.DroppingPointsString = bus.DroppingPointsString;
            }

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

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isAdmin)
            {
                var adminCount = await (from ur in _context.UserRoles
                                        join r in _context.Roles on ur.RoleId equals r.Id
                                        where r.Name == "Admin"
                                        select ur.UserId).Distinct().CountAsync();
                if (adminCount <= 1)
                    return Json(new { success = false, message = "Cannot delete the last remaining admin user." });
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
                return Json(new { success = true, message = "User deleted successfully." });

            return Json(new { success = false, message = "Failed to delete user." });
        }

        // ===== Assign Role (Admin/User) =====
        [HttpPost]
        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(role))
                return Json(new { success = false, message = "Invalid request." });

            role = role.Trim();
            if (role != "Admin" && role != "User")
                return Json(new { success = false, message = "Unsupported role." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            var currentRoles = await _userManager.GetRolesAsync(user);
            var isCurrentlyAdmin = currentRoles.Contains("Admin");

            if (isCurrentlyAdmin && role != "Admin")
            {
                var adminCount = await (from ur in _context.UserRoles
                                        join r in _context.Roles on ur.RoleId equals r.Id
                                        where r.Name == "Admin"
                                        select ur.UserId).Distinct().CountAsync();

                if (adminCount <= 1)
                    return Json(new { success = false, message = "Cannot remove Admin from the last remaining admin user." });
            }

            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            if (!await _roleManager.RoleExistsAsync("User"))
                await _roleManager.CreateAsync(new IdentityRole("User"));

            if (currentRoles.Contains("Admin"))
                await _userManager.RemoveFromRoleAsync(user, "Admin");
            if (currentRoles.Contains("User"))
                await _userManager.RemoveFromRoleAsync(user, "User");

            var add = await _userManager.AddToRoleAsync(user, role);
            if (!add.Succeeded)
                return Json(new { success = false, message = string.Join(", ", add.Errors.Select(e => e.Description)) });

            return Json(new { success = true, message = $"Role updated to {role}." });
        }
    }
}
