using Microsoft.AspNetCore.Mvc;
using System;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    public class BusController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Results(string from, string to, DateTime journeyDate, string? returnDate, string tripType)
        {
            ViewBag.From = from;
            ViewBag.To = to;
            ViewBag.JourneyDate = journeyDate.ToString("yyyy-MM-dd");
            ViewBag.ReturnDate = returnDate;
            ViewBag.TripType = tripType;

            // TODO: Add actual search logic or fetch from database

            return View();
        }
    }
}
