using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminBusController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AdminBusController(ApplicationDbContext context) => _context = context;

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockBus(int busId, bool block = true)
        {
            var bus = await _context.Buses.FirstOrDefaultAsync(b => b.Id == busId);
            if (bus == null) return NotFound();

            bus.IsBlocked = block;
            _context.Buses.Update(bus);

            // Also reflect on existing schedules of this bus
            var schedules = await _context.BusSchedules.Where(s => s.BusId == busId).ToListAsync();
            foreach (var s in schedules) s.IsBlocked = block;
            _context.BusSchedules.UpdateRange(schedules);

            await _context.SaveChangesAsync();
            TempData["ok"] = block ? "Bus blocked." : "Bus unblocked.";
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockSchedule(int scheduleId, bool block = true)
        {
            var s = await _context.BusSchedules.FirstOrDefaultAsync(x => x.Id == scheduleId);
            if (s == null) return NotFound();

            s.IsBlocked = block;
            _context.BusSchedules.Update(s);
            await _context.SaveChangesAsync();

            TempData["ok"] = block ? "Schedule blocked." : "Schedule unblocked.";
            return Redirect(Request.Headers["Referer"].ToString());
        }
    }
}
