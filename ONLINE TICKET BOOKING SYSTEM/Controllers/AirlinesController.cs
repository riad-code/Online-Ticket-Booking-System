using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models.Air;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AirlinesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public AirlinesController(ApplicationDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _db.Airlines.AsNoTracking().OrderBy(a => a.Name).ToListAsync();
            return View(list);
        }

        [HttpGet]
        public IActionResult Create() => View(new Airline());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Airline model)
        {
            if (!ModelState.IsValid) return View(model);
            _db.Airlines.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.Airlines.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Airline model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);
            _db.Airlines.Update(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.Airlines.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _db.Airlines.FindAsync(id);
            if (item == null) return NotFound();
            _db.Airlines.Remove(item);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
