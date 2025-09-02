using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models.Air;
using ONLINE_TICKET_BOOKING_SYSTEM.Services;
using ONLINE_TICKET_BOOKING_SYSTEM.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    public class AirBookingController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IAirBookingService _booking;
        private readonly ITicketPdfService _pdf;

        public AirBookingController(ApplicationDbContext db, IAirBookingService booking, ITicketPdfService pdf)
        {
            _db = db;
            _booking = booking;
            _pdf = pdf;
        }

        // ---------- helper: compute fare once if AmountDue is 0 ----------
        private async Task ComputeAndPersistAmountDueAsync(AirBooking b)
        {
            if (b == null) return;
            if (b.AmountDue > 0) return;

            // use first segment for simple one-way pricing (you can extend for multi-seg/return)
            var seg = await _db.FlightSegments
                               .Include(s => s.FlightSchedule)
                               .FirstOrDefaultAsync(s => s.ItineraryId == b.ItineraryId);

            if (seg == null) return;

            var cabin = seg.Cabin; // set from your Hold(request) already
            var fare = await _db.FareClasses
                                .Where(f => f.FlightScheduleId == seg.FlightScheduleId && f.Cabin == cabin)
                                .OrderBy(f => (f.BaseFare + f.TaxesAndFees))
                                .FirstOrDefaultAsync();

            if (fare == null) return;

            // Simple rule: charge adults + children; infants free (adjust if needed)
            var chargeablePax = Math.Max(0, b.Adults + b.Children);
            var perPax = fare.BaseFare + fare.TaxesAndFees;
            var total = perPax * chargeablePax;

            b.AmountDue = total;
            b.Currency = fare.Currency; // usually BDT
            await _db.SaveChangesAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Hold(int sid, string date, string cabin, int pax = 1)
        {
            var schedule = await _db.FlightSchedules
                .Include(s => s.FromAirport)
                .Include(s => s.ToAirport)
                .Include(s => s.Airline)
                .FirstOrDefaultAsync(s => s.Id == sid);

            if (schedule == null) return NotFound();

            var travelDate = DateOnly.Parse(date);

            var itin = new Itinerary
            {
                Cabin = Enum.TryParse<CabinClass>(cabin, true, out var c) ? c : CabinClass.Economy,
                Currency = "BDT",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15),
                Segments = new()
                {
                    new FlightSegment
                    {
                        FlightScheduleId = schedule.Id,
                        TravelDate = travelDate,
                        Cabin = Enum.TryParse<CabinClass>(cabin, true, out var c2) ? c2 : CabinClass.Economy
                    }
                }
            };

            var booking = await _booking.CreateHoldAsync(itin, pax, 0, 0);

            // **ensure AmountDue is non-zero**
            await ComputeAndPersistAmountDueAsync(booking);

            return RedirectToAction(nameof(Passengers), new { pnr = booking.Pnr });
        }

        [HttpGet]
        public async Task<IActionResult> Passengers(string pnr)
        {
            var b = await _db.AirBookings
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.Airline)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.FromAirport)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.ToAirport)
                .FirstOrDefaultAsync(x => x.Pnr == pnr);

            if (b == null) return NotFound();

            // if still 0 (e.g., direct link) compute now
            await ComputeAndPersistAmountDueAsync(b);

            var fs = b.Itinerary.Segments.First().FlightSchedule;

            var vm = new AirPassengersFormVm
            {
                Pnr = b.Pnr,
                From = fs.FromAirport.IataCode,
                To = fs.ToAirport.IataCode,
                TravelDate = b.Itinerary.Segments.First().TravelDate.ToString("yyyy-MM-dd"),
                Flight = $"{fs.Airline.IataCode} {fs.FlightNumber}",
                Adults = b.Adults,
                Children = b.Children,
                Infants = b.Infants,
                AmountDue = b.AmountDue,
                Currency = b.Currency
            };

            for (int i = 0; i < b.Adults; i++)
                vm.Passengers.Add(new PaxRow { Type = PaxType.Adult });

            // Auto-fill contact + first passenger (if logged in and data exists)
            if (User?.Identity?.IsAuthenticated == true)
            {
                var usr = await _db.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
                if (usr != null)
                {
                    ViewBag.ContactName = string.IsNullOrWhiteSpace(usr.FullName) ? usr.UserName : usr.FullName;
                    ViewBag.ContactEmail = usr.Email;
                    ViewBag.ContactPhone = usr.PhoneNumber;

                    string NormalizeGender(string? g)
                    {
                        var s = (g ?? "").Trim().ToLowerInvariant();
                        if (s is "m" or "male" or "পুরুষ" or "chele" or "ছেলে") return "Male";
                        if (s is "f" or "female" or "মহিলা" or "mey" or "meye" or "মেয়ে") return "Female";
                        return "Other";
                    }

                    var gender = NormalizeGender(usr.Gender);

                    if (vm.Passengers.Count > 0)
                    {
                        var fullName = ViewBag.ContactName as string ?? "";
                        var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0) vm.Passengers[0].FirstName = parts[0];
                        if (parts.Length > 1) vm.Passengers[0].LastName = parts[1];
                        vm.Passengers[0].Gender = gender;

                        if (usr.DateOfBirth.HasValue)
                            vm.Passengers[0].Dob = usr.DateOfBirth.Value.ToString("yyyy-MM-dd");
                    }
                }
            }

            return View(vm);
        }
        // AirBookingController.cs  (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Passengers(
            AirPassengersFormVm form,
            string? ContactName,
            string? ContactEmail,
            string? ContactPhone)
        {
            if (form == null || form.Passengers == null || form.Passengers.Count == 0)
            {
                ModelState.AddModelError("", "Please enter at least one passenger.");
                return View(form);
            }

            var pax = new List<Passenger>();
            foreach (var r in form.Passengers)
            {
                if (string.IsNullOrWhiteSpace(r.FirstName) || string.IsNullOrWhiteSpace(r.LastName))
                {
                    ModelState.AddModelError("", "Passenger first & last name are required.");
                    return View(form);
                }

                if (!DateOnly.TryParse(r.Dob, out var dob))
                    dob = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18));

                pax.Add(new Passenger
                {
                    FirstName = r.FirstName.Trim(),
                    LastName = r.LastName.Trim(),
                    Dob = dob,
                    Type = r.Type,
                    Gender = string.IsNullOrWhiteSpace(r.Gender) ? "Other" : r.Gender,
                    PassportNo = r.PassportNo
                });
            }

            await _booking.AttachPassengersAsync(form.Pnr, pax);

            // Load booking to store contact + link to user
            var b = await _db.AirBookings.FirstOrDefaultAsync(x => x.Pnr == form.Pnr);
            if (b != null)
            {
                // Prefer posted contact; fallback to profile if missing
                if (string.IsNullOrWhiteSpace(ContactName) && User?.Identity?.IsAuthenticated == true)
                {
                    var usr = await _db.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
                    ContactName = string.IsNullOrWhiteSpace(ContactName) ? (usr?.FullName ?? usr?.UserName) : ContactName;
                    ContactEmail = string.IsNullOrWhiteSpace(ContactEmail) ? usr?.Email : ContactEmail;
                    ContactPhone = string.IsNullOrWhiteSpace(ContactPhone) ? usr?.PhoneNumber : ContactPhone;
                }

                b.ContactName = ContactName;
                b.ContactEmail = ContactEmail;
                b.ContactPhone = ContactPhone;

                // ✅ NEW: link booking to signed-in user for dashboard
                if (User?.Identity?.IsAuthenticated == true)
                {
                    var usr = await _db.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
                    if (usr != null) b.UserId = usr.Id;   // <-- requires AirBooking.UserId (string?) column
                }

                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Pay), new { pnr = form.Pnr });
        }


        [HttpGet]
        public async Task<IActionResult> Pay(string pnr)
        {
            var b = await _db.AirBookings
                .Include(x => x.Passengers)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.Airline)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.FromAirport)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.ToAirport)
                .FirstOrDefaultAsync(x => x.Pnr == pnr);

            if (b == null) return NotFound();
            return View(b);
        }

        [HttpGet]
        public async Task<IActionResult> ThankYou(string pnr)
        {
            var b = await _db.AirBookings
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.Airline)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.FromAirport)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.ToAirport)
                .FirstOrDefaultAsync(x => x.Pnr == pnr);

            if (b == null) return NotFound();
            return View(b);
        }


        [HttpGet]
        public async Task<IActionResult> Ticket(string pnr)
        {
            var b = await _db.AirBookings
                .Include(x => x.Passengers)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.Airline)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.FromAirport)
                .Include(x => x.Itinerary).ThenInclude(i => i.Segments).ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.ToAirport)
                .FirstOrDefaultAsync(x => x.Pnr == pnr);

            if (b == null) return NotFound();

            var bytes = await _pdf.GenerateAirTicketAsync(b);
            return File(bytes, "application/pdf", $"AirTicket_{pnr}.pdf");
        }
        // =======================
        //  MY AIR BOOKINGS (NEW)
        // =======================
        [Authorize(Roles = "User")]
        [HttpGet]
        public async Task<IActionResult> MyAirBookings()
        {
            string? uid = null;
            if (User?.Identity?.IsAuthenticated == true)
            {
                var usr = await _db.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
                uid = usr?.Id;
            }

            var q = _db.AirBookings
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.Airline)
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.FromAirport)
                .Include(b => b.Itinerary).ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule).ThenInclude(fs => fs.ToAirport)
                .OrderByDescending(b => b.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(uid))
            {
                q = q.Where(b => b.UserId == uid);
            }
            else if (User?.Identity?.IsAuthenticated == true)
            {
                var email = await _db.Users.Where(u => u.UserName == User.Identity!.Name)
                                           .Select(u => u.Email)
                                           .FirstOrDefaultAsync();
                if (!string.IsNullOrWhiteSpace(email))
                    q = q.Where(b => b.ContactEmail == email);
            }

            var list = await q.ToListAsync();
            return View(list);
        }
    }
}
