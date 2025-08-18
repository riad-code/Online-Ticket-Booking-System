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
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data submitted." });
            }

            var layout = await _context.SeatLayouts.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (layout == null)
            {
                return Json(new { success = false, message = "Layout not found." });
            }

            try
            {
                layout.TotalSeats = model.TotalSeats;
                layout.LayoutJson = string.IsNullOrWhiteSpace(model.LayoutJson)
                    ? GenerateDefaultLayoutJson(model.TotalSeats)
                    : model.LayoutJson;
                layout.BlockedSeatsCsv = model.BlockedSeatsCsv ?? "";

                _context.SeatLayouts.Update(layout);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Seat layout updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

       
        [HttpPost]
        public IActionResult GenerateLayoutPreview([FromBody] LayoutPreviewRequest request)
        {
            try
            {
                int totalSeats = request.TotalSeats > 0 ? request.TotalSeats : 40;
                var seatNames = ParseSeatNames(request.LayoutJson, totalSeats);
                return Json(seatNames);
            }
            catch
            {
                return BadRequest(new { message = "Invalid JSON or data format." });
            }
        }

        
        public class LayoutPreviewRequest
        {
            public string? LayoutJson { get; set; }
            public int TotalSeats { get; set; }
        }

      
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