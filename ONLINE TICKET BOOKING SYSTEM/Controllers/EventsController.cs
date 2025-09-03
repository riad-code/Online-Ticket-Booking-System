using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models.Event;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _environment;

        public EventsController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _environment = env;
        }

        // ---------- PUBLIC ----------
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var items = await _db.EventItems
                .AsNoTracking()
                .OrderBy(e => e.StartDateUtc)
                .ToListAsync();
            return View(items);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _db.EventItems.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);
            if (item == null) return NotFound();
            return View(item);
        }

        // ---------- ADMIN ----------
        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View(new EventItem { Currency = "৳", IsFeatured = false });

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(EventItem model)
        {
            if (!ModelState.IsValid) return View(model);

            // Handle file upload (optional)
            await SaveCoverImageAsync(model, oldUrlToDelete: null);

            model.StartDateUtc = ToUtc(model.StartDateUtc);
            model.EndDateUtc = ToUtc(model.EndDateUtc);

            _db.EventItems.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.EventItems.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, EventItem model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var existing = await _db.EventItems.FirstOrDefaultAsync(e => e.Id == id);
            if (existing == null) return NotFound();

            var oldUrl = existing.CoverImageUrl;

            // যদি নতুন ফাইল দেয়া হয়, সেভ করবো এবং পুরোনোটা ডিলিট করবো
            bool replaced = await SaveCoverImageAsync(model, oldUrl);

            existing.Title = model.Title;
            existing.Description = model.Description;
            existing.Category = model.Category;
            existing.City = model.City;
            existing.Venue = model.Venue;
            existing.StartDateUtc = ToUtc(model.StartDateUtc);
            existing.EndDateUtc = ToUtc(model.EndDateUtc);
            existing.PriceFrom = model.PriceFrom;
            existing.Currency = model.Currency;
            existing.AvailableTickets = model.AvailableTickets;
            existing.IsFeatured = model.IsFeatured;
            existing.UpdatedAtUtc = DateTime.UtcNow;

            if (replaced)
                existing.CoverImageUrl = model.CoverImageUrl; // নতুন path সেট

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.EventItems.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _db.EventItems.FindAsync(id);
            if (item == null) return NotFound();

            // (Optional) delete physical file if it lives under /uploads/events
            TryDeletePhysicalFile(item.CoverImageUrl);

            _db.EventItems.Remove(item);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private static DateTime? ToUtc(DateTime? localOrUnspecified)
        {
            if (localOrUnspecified is null) return null;
            var dt = localOrUnspecified.Value;
            if (dt.Kind == DateTimeKind.Utc) return dt;
            if (dt.Kind == DateTimeKind.Unspecified)
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Local);
            return dt.ToUniversalTime();
        }

      
        private async Task<bool> SaveCoverImageAsync(EventItem model, string? oldUrlToDelete)
        {
            if (model.CoverImageFile == null || model.CoverImageFile.Length == 0)
                return false;

            var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", "events");
            if (!Directory.Exists(uploadsRoot))
                Directory.CreateDirectory(uploadsRoot);

            var ext = Path.GetExtension(model.CoverImageFile.FileName);
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(ext.ToLower()))
            {
                ModelState.AddModelError("CoverImageFile", "Only JPG, PNG, WEBP are allowed.");
                return false;
            }

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var savePath = Path.Combine(uploadsRoot, fileName);

            using (var fs = new FileStream(savePath, FileMode.Create))
            {
                await model.CoverImageFile.CopyToAsync(fs);
            }

            model.CoverImageUrl = $"/uploads/events/{fileName}";

            // delete old only if new was saved and old lives inside our uploads folder
            if (!string.IsNullOrWhiteSpace(oldUrlToDelete))
                TryDeletePhysicalFile(oldUrlToDelete);

            return true;
        }

        private void TryDeletePhysicalFile(string? webUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(webUrl)) return;

                // only delete our managed uploads
                if (!webUrl.StartsWith("/uploads/events/", StringComparison.OrdinalIgnoreCase))
                    return;

                var fullPath = Path.Combine(_environment.WebRootPath, webUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }
            catch
            {
                // swallow — deleting old files is best-effort
            }
        }
    }
}
