using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminSeatController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AdminSeatController(ApplicationDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> EditLayout(int busId)
        {
            var bus = await _context.Buses.FindAsync(busId);
            if (bus == null) return NotFound();

            var layout = await _context.SeatLayouts.FirstOrDefaultAsync(x => x.BusId == busId);
            if (layout == null)
            {
                layout = new SeatLayout
                {
                    BusId = busId,
                    TotalSeats = 40,
                    LayoutJson = GenerateDefaultLayoutJson(40),
                    BlockedSeatsCsv = ""
                };
                _context.SeatLayouts.Add(layout);
                await _context.SaveChangesAsync();
            }
            return View(layout);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLayout(SeatLayout model)
        {
            var layout = await _context.SeatLayouts.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (layout == null) return NotFound();

            layout.TotalSeats = model.TotalSeats;
            layout.LayoutJson = string.IsNullOrWhiteSpace(model.LayoutJson)
                ? GenerateDefaultLayoutJson(model.TotalSeats)
                : model.LayoutJson;
            layout.BlockedSeatsCsv = model.BlockedSeatsCsv ?? "";

            await _context.SaveChangesAsync();
            TempData["msg"] = "Seat layout updated.";
            return RedirectToAction(nameof(EditLayout), new { busId = layout.BusId });
        }

        // Schedule তৈরি হলে চাইলে এই action দিয়ে সিড করতে পারেন
        [HttpPost]
        public async Task<IActionResult> SeedScheduleSeats(int scheduleId)
        {
            var schedule = await _context.BusSchedules.FirstOrDefaultAsync(s => s.Id == scheduleId);
            if (schedule == null || schedule.BusId == null) return NotFound("Schedule/Bus not found.");

            if (await _context.ScheduleSeats.AnyAsync(x => x.BusScheduleId == scheduleId))
                return Ok(new { success = true, message = "Seats already exist for this schedule." });

            var layout = await _context.SeatLayouts.FirstOrDefaultAsync(x => x.BusId == schedule.BusId.Value);
            var names = ParseSeatNames(layout?.LayoutJson, layout?.TotalSeats ?? 40);
            var blocked = ParseBlocked(layout?.BlockedSeatsCsv);

            var seats = names.Select(n => new ScheduleSeat
            {
                BusScheduleId = scheduleId,
                SeatNo = n,
                Status = blocked.Contains(n) ? SeatStatus.Blocked : SeatStatus.Available
            }).ToList();

            _context.ScheduleSeats.AddRange(seats);
            await _context.SaveChangesAsync();

            // defensive: SeatsAvailable sync
            schedule.SeatsAvailable = seats.Count(s => s.Status == SeatStatus.Available);
            _context.BusSchedules.Update(schedule);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Schedule seats seeded from layout." });
        }

        // Helpers
        private static string GenerateDefaultLayoutJson(int total)
        {
            var cols = new[] { "A", "B", "C", "D" };
            var list = new List<string>();
            int rows = (int)Math.Ceiling(total / 4.0);
            for (int r = 1; r <= rows; r++)
            {
                foreach (var c in cols)
                {
                    if (list.Count >= total) break;
                    list.Add($"{c}{r}");
                }
            }
            return System.Text.Json.JsonSerializer.Serialize(list);
        }

        private static List<string> ParseSeatNames(string? json, int totalFallback)
        {
            try
            {
                var arr = System.Text.Json.JsonSerializer.Deserialize<string[]>(json ?? "[]");
                if (arr != null && arr.Length > 0) return arr.ToList();
            }
            catch { }

            var cols = new[] { "A", "B", "C", "D" };
            var list = new List<string>();
            int rows = (int)Math.Ceiling(totalFallback / 4.0);
            for (int r = 1; r <= rows; r++)
            {
                foreach (var c in cols)
                {
                    if (list.Count >= totalFallback) break;
                    list.Add($"{c}{r}");
                }
            }
            return list;
        }

        private static HashSet<string> ParseBlocked(string? csv)
        {
            return new HashSet<string>(
                (csv ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                StringComparer.OrdinalIgnoreCase);
        }
    }
}
