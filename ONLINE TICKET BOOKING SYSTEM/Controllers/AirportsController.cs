using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models.Air;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AirportsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public AirportsController(ApplicationDbContext db) => _db = db;

        private static bool IsAjax(Microsoft.AspNetCore.Http.IHeaderDictionary headers) =>
            headers.TryGetValue("X-Requested-With", out var v) && v == "XMLHttpRequest";

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _db.Airports
                .AsNoTracking()
                .OrderBy(a => a.City)
                .ThenBy(a => a.IataCode)
                .ToListAsync();
            return View(list);
        }

        [HttpGet]
        public IActionResult Create() => View(new Airport());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Airport model)
        {
            if (!ModelState.IsValid) return View(model);

            _db.Airports.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.Airports.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Airport model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);

            _db.Airports.Update(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.Airports.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        // ✅ Remove ActionName("Delete"); keep the route as /Airports/DeleteConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _db.Airports.FindAsync(id);
            if (item == null)
            {
                if (IsAjax(Request.Headers)) return NotFound(new { ok = false, message = "Not found" });
                return NotFound();
            }

            try
            {
                _db.Airports.Remove(item);
                await _db.SaveChangesAsync();

                if (IsAjax(Request.Headers))
                    return Json(new { ok = true, redirect = Url.Action(nameof(Index)) });

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                // Likely FK reference from FlightSchedules with Restrict
                var message = "Cannot delete this airport because it is referenced by one or more flight schedules.";
                if (IsAjax(Request.Headers))
                    return BadRequest(new { ok = false, message });
                ModelState.AddModelError(string.Empty, message);
                return View("Delete", item);
            }
        }
    }
}
