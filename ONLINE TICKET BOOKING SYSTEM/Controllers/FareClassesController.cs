using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models.Air;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FareClassesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public FareClassesController(ApplicationDbContext db) => _db = db;

        private void LoadDropdowns()
        {
            var flights = _db.FlightSchedules
                .Include(f => f.Airline)
                .Include(f => f.FromAirport)
                .Include(f => f.ToAirport)
                .AsNoTracking()
                .OrderBy(f => f.Airline.Name).ThenBy(f => f.FlightNumber)
                .Select(f => new
                {
                    f.Id,
                    Text = $"{f.Airline.Name} {f.FlightNumber} — {f.FromAirport.IataCode}→{f.ToAirport.IataCode}"
                })
                .ToList();

            ViewBag.FlightScheduleId = new SelectList(flights, "Id", "Text");
            ViewBag.Cabins = new SelectList(Enum.GetNames(typeof(CabinClass)));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _db.FareClasses
                .Include(x => x.FlightSchedule).ThenInclude(fs => fs.Airline)
                .Include(x => x.FlightSchedule).ThenInclude(fs => fs.FromAirport)
                .Include(x => x.FlightSchedule).ThenInclude(fs => fs.ToAirport)
                .AsNoTracking()
                .OrderBy(x => x.FlightSchedule.Airline.Name)
                .ThenBy(x => x.FlightSchedule.FlightNumber)
                .ToListAsync();

            return View(list);
        }

        [HttpGet]
        public IActionResult Create()
        {
            LoadDropdowns();
            return View(new FareClass { Currency = "BDT" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FareClass model)
        {
            if (!ModelState.IsValid)
            {
                LoadDropdowns();
                return View(model);
            }
            _db.FareClasses.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.FareClasses.FindAsync(id);
            if (item == null) return NotFound();

            LoadDropdowns();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FareClass model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid)
            {
                LoadDropdowns();
                return View(model);
            }

            _db.FareClasses.Update(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.FareClasses
                .Include(x => x.FlightSchedule).ThenInclude(fs => fs.Airline)
                .Include(x => x.FlightSchedule).ThenInclude(fs => fs.FromAirport)
                .Include(x => x.FlightSchedule).ThenInclude(fs => fs.ToAirport)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _db.FareClasses.FindAsync(id);
            if (item == null) return NotFound();

            _db.FareClasses.Remove(item);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
