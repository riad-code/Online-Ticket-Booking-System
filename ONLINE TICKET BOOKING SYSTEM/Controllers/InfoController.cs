using Microsoft.AspNetCore.Mvc;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    public class InfoController : Controller
    {
        [HttpGet]
        public IActionResult PrivacyPolicy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Terms()
        {
            return View();
        }
    }
}
