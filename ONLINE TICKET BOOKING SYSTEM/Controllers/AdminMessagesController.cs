using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminMessagesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public AdminMessagesController(ApplicationDbContext db) => _db = db;

        // List with simple search & page size
        [HttpGet]
        public async Task<IActionResult> Index(string? q, int take = 20)
        {
            take = new[] { 10, 20, 50, 100 }.Contains(take) ? take : 20;

            var query = _db.ContactMessages.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(x =>
                    x.Name.ToLower().Contains(term) ||
                    x.Email.ToLower().Contains(term) ||
                    x.Message.ToLower().Contains(term));
            }

            var items = await query
                .OrderByDescending(x => x.SentAt)
                .Take(take)
                .ToListAsync();

            ViewBag.q = q;
            ViewBag.take = take;
            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var msg = await _db.ContactMessages.FindAsync(id);
            if (msg == null) return NotFound();

            if (!msg.IsRead)
            {
                msg.IsRead = true;
                await _db.SaveChangesAsync();
            }

            return View(msg);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var msg = await _db.ContactMessages.FindAsync(id);
            if (msg != null)
            {
                _db.ContactMessages.Remove(msg);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
