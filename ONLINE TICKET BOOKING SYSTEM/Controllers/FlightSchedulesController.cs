using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models.Air;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FlightSchedulesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public FlightSchedulesController(ApplicationDbContext db) => _db = db;

        private void LoadDropdowns()
        {
            ViewBag.AirlineId = new SelectList(_db.Airlines.AsNoTracking().OrderBy(x => x.Name).ToList(), "Id", "Name");
            var airports = _db.Airports.AsNoTracking().OrderBy(x => x.City).ToList();
            ViewBag.FromAirportId = new SelectList(airports, "Id", "City");
            ViewBag.ToAirportId = new SelectList(airports, "Id", "City");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _db.FlightSchedules
                .Include(f => f.Airline)
                .Include(f => f.FromAirport)
                .Include(f => f.ToAirport)
                .AsNoTracking()
                .OrderBy(f => f.Airline.Name).ThenBy(f => f.FlightNumber)
                .ToListAsync();

            return View(list);
        }

        [HttpGet]
        public IActionResult Create()
        {
            LoadDropdowns();
            return View(new FlightSchedule { OperatingDaysMask = 127 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FlightSchedule model)
        {
            if (!ModelState.IsValid)
            {
                LoadDropdowns();
                return View(model);
            }

            _db.FlightSchedules.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.FlightSchedules.FindAsync(id);
            if (item == null) return NotFound();

            LoadDropdowns();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FlightSchedule model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid)
            {
                LoadDropdowns();
                return View(model);
            }

            _db.FlightSchedules.Update(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.FlightSchedules
                .Include(f => f.Airline)
                .Include(f => f.FromAirport)
                .Include(f => f.ToAirport)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _db.FlightSchedules.FindAsync(id);
            if (item == null) return NotFound();

            _db.FlightSchedules.Remove(item);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
