using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public AccountController(UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        // ✅ 1. Show ForgotPasswordOTP Page
        [HttpGet]
        public IActionResult ForgotPasswordOTP()
        {
            return View();
        }

        // ✅ 2. Send OTP to Email
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ViewBag.Error = "Email not found!";
                return View();
            }

            // Generate OTP
            var otp = new Random().Next(100000, 999999).ToString();

            // Store OTP in session (expires in 5 mins)
            HttpContext.Session.SetString("OTP", otp);
            HttpContext.Session.SetString("Email", email);
            HttpContext.Session.SetString("OtpExpiry", DateTime.Now.AddMinutes(5).ToString());

            // Send OTP email
            await _emailSender.SendEmailAsync(email, "Password Reset OTP", $"<h3>Your OTP is: <b>{otp}</b></h3><p>This OTP will expire in 5 minutes.</p>");

            ViewBag.Message = "OTP sent to your email!";
            TempData["Email"] = email; // Pass to next page
            return RedirectToAction("VerifyOTP");
        }

        // ✅ 3. Show Verify OTP Page
        [HttpGet]
        public IActionResult VerifyOtp()
        {
            if (TempData["Email"] == null)
            {
                return RedirectToAction("ForgotPasswordOTP");
            }
            return View();
        }

        // ✅ 4. Verify OTP and Reset Password
        [HttpPost]
        public async Task<IActionResult> VerifyOTP(string email, string otp, string newPassword)
        {
            var sessionOtp = HttpContext.Session.GetString("OTP");
            var sessionEmail = HttpContext.Session.GetString("Email");
            var expiry = HttpContext.Session.GetString("OtpExpiry");

            if (sessionOtp == null || sessionEmail == null || expiry == null)
            {
                ViewBag.Error = "OTP expired. Please request again.";
                return View();
            }

            if (DateTime.Now > DateTime.Parse(expiry))
            {
                ViewBag.Error = "OTP expired. Please request again.";
                return View();
            }

            if (otp != sessionOtp || email != sessionEmail)
            {
                ViewBag.Error = "Invalid OTP!";
                return View();
            }

            // Reset password
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ViewBag.Error = "User not found!";
                return View();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                // Clear OTP session
                HttpContext.Session.Remove("OTP");
                HttpContext.Session.Remove("Email");
                HttpContext.Session.Remove("OtpExpiry");

                ViewBag.Message = "Password reset successful!";
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Error = string.Join(", ", result.Errors.Select(e => e.Description));
            return View();
        }
    }
}
