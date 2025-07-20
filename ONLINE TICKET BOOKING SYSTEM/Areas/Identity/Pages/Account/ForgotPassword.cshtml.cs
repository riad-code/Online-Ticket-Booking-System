using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required, EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email not found.");
                return Page();
            }

            // Generate OTP
            var otp = new Random().Next(100000, 999999).ToString();

            // Save OTP in Session
            HttpContext.Session.SetString("OTP", otp);
            HttpContext.Session.SetString("Email", Input.Email);

            // Send OTP Email
            await _emailSender.SendEmailAsync(Input.Email, "Your OTP Code", $"Your OTP is: {otp}");

            return RedirectToPage("/Account/VerifyOtp");
        }
    }
}
