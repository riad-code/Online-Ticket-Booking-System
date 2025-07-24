using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
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

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToPage("/Account/VerifyOtp");
            }

            // Generate OTP
            var otp = new Random().Next(100000, 999999).ToString();

            // Save OTP and timestamp in Session
            HttpContext.Session.SetString("OTP", otp);
            HttpContext.Session.SetString("OTPExpiry", DateTime.Now.AddMinutes(5).ToString()); // Expires in 5 min
            HttpContext.Session.SetString("Email", Input.Email);

            // Send email
            var message = $"Your OTP is {otp}. It will expire in 5 minutes.";
            await _emailSender.SendEmailAsync(Input.Email, "Password Reset OTP", message);

            return RedirectToPage("/Account/VerifyOtp");
        }
    }
}
