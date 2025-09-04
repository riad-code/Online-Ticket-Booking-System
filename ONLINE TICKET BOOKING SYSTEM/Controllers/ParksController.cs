using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models.Park;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    public class ParksController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ParksController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // Public Index
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var parks = await _db.ParkItems.AsNoTracking().OrderBy(p => p.Title).ToListAsync();
            return View(parks);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex()
        {
            var list = await _db.ParkItems.AsNoTracking().OrderBy(p => p.Title).ToListAsync();
            return View(list);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var park = await _db.ParkItems.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (park == null) return NotFound();
            return View(park);
        }

        // Admin Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View(new ParkItem { Currency = "৳", IsFeatured = false });

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(ParkItem model)
        {
            if (!ModelState.IsValid) return View(model);

            if (model.CoverImageFile != null && model.CoverImageFile.Length > 0)
            {
                var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "parks");
                if (!Directory.Exists(uploadsRoot)) Directory.CreateDirectory(uploadsRoot);

                var ext = Path.GetExtension(model.CoverImageFile.FileName);
                var fileName = $"{Guid.NewGuid():N}{ext}";
                var savePath = Path.Combine(uploadsRoot, fileName);

                using (var fs = new FileStream(savePath, FileMode.Create))
                    await model.CoverImageFile.CopyToAsync(fs);

                model.CoverImageUrl = $"/uploads/parks/{fileName}";
            }

            _db.ParkItems.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(AdminIndex));
        }

        // Admin Edit
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var park = await _db.ParkItems.FindAsync(id);
            if (park == null) return NotFound();
            return View(park);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, ParkItem model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var existing = await _db.ParkItems.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Title = model.Title;
            existing.City = model.City;
            existing.Location = model.Location;
            existing.OpeningHours = model.OpeningHours;
            existing.Category = model.Category;
            existing.Description = model.Description;
            existing.PriceFrom = model.PriceFrom;
            existing.Currency = model.Currency;
            existing.AvailableTickets = model.AvailableTickets;
            existing.IsFeatured = model.IsFeatured;
            existing.UpdatedAtUtc = DateTime.UtcNow;

            if (model.CoverImageFile != null && model.CoverImageFile.Length > 0)
            {
                var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "parks");
                if (!Directory.Exists(uploadsRoot)) Directory.CreateDirectory(uploadsRoot);

                var ext = Path.GetExtension(model.CoverImageFile.FileName);
                var fileName = $"{Guid.NewGuid():N}{ext}";
                var savePath = Path.Combine(uploadsRoot, fileName);

                using (var fs = new FileStream(savePath, FileMode.Create))
                    await model.CoverImageFile.CopyToAsync(fs);

                existing.CoverImageUrl = $"/uploads/parks/{fileName}";
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(AdminIndex));
        }

        // Admin Delete
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var park = await _db.ParkItems.FindAsync(id);
            if (park == null) return NotFound();
            return View(park);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var park = await _db.ParkItems.FindAsync(id);
            if (park == null) return NotFound();

            _db.ParkItems.Remove(park);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(AdminIndex));
        }
    }
}
