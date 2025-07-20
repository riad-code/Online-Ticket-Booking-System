using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Areas.Identity.Pages.Account
{
    public class VerifyOtpModel : PageModel
    {
        [BindProperty]
        [Required]
        public string Otp { get; set; }

        public IActionResult OnPost()
        {
            var savedOtp = HttpContext.Session.GetString("OTP");
            if (savedOtp == null || savedOtp != Otp)
            {
                ModelState.AddModelError("", "Invalid OTP.");
                return Page();
            }

            return RedirectToPage("/Account/ResetPassword");
        }
    }
}
