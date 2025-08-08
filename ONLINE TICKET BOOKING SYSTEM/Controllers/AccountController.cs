using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.UI.Services;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using Microsoft.AspNetCore.Hosting;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IWebHostEnvironment _env;

        public AccountController(UserManager<ApplicationUser> userManager, IEmailSender emailSender, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> ForgotPasswordOTP()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    ViewBag.Email = user.Email; 
                }
            }
            return View(); 
        }



        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ViewBag.Error = "Email not found!";
                return View();
            }

            var otp = new Random().Next(100000, 999999).ToString();

            HttpContext.Session.SetString("OTP", otp);
            HttpContext.Session.SetString("Email", email);
            HttpContext.Session.SetString("OtpExpiry", DateTime.Now.AddMinutes(5).ToString());

            await _emailSender.SendEmailAsync(email, "Password Reset OTP", $"<h3>Your OTP is: <b>{otp}</b></h3><p>This OTP will expire in 5 minutes.</p>");

            ViewBag.Message = "OTP sent to your email!";
            TempData["Email"] = email; 
            return RedirectToAction("VerifyOTP");
        }
        [HttpGet]
        public IActionResult VerifyOTP()
        {
            if (TempData["Email"] == null)
            {
                return RedirectToAction("ForgotPasswordOTP");
            }
            return View();
        }

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
                HttpContext.Session.Remove("OTP");
                HttpContext.Session.Remove("Email");
                HttpContext.Session.Remove("OtpExpiry");

                ViewBag.Message = "Password reset successful!";
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Error = string.Join(", ", result.Errors.Select(e => e.Description));
            return View();
        }

        // ✅ Show Profile Page
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(
     string Id,
     string Title,
     string FirstName,
     string LastName,
     string MobileNumber,
     string Gender,
     DateTime? DateOfBirth,
     string Address,
     string NidNo,
     string PassportNo,
     string VisaNo,
     IFormFile ProfileImage)
        {
            var user = await _userManager.FindByIdAsync(Id);
            if (user == null)
                return Json(new { success = false, message = "User not found!" });

            if (!string.IsNullOrWhiteSpace(Title)) user.Title = Title;
            if (!string.IsNullOrWhiteSpace(FirstName)) user.FirstName = FirstName;
            if (!string.IsNullOrWhiteSpace(LastName)) user.LastName = LastName;
            if (!string.IsNullOrWhiteSpace(MobileNumber)) user.MobileNumber = MobileNumber;
            if (!string.IsNullOrWhiteSpace(Gender)) user.Gender = Gender;
            if (DateOfBirth.HasValue) user.DateOfBirth = DateOfBirth.Value;

            // <-- Use the parameter directly here -->
            if (!string.IsNullOrWhiteSpace(Address))
                user.Address = Address;

            if (!string.IsNullOrWhiteSpace(NidNo)) user.NidNo = NidNo;
            if (!string.IsNullOrWhiteSpace(PassportNo)) user.PassportNo = PassportNo;
            if (!string.IsNullOrWhiteSpace(VisaNo)) user.VisaNo = VisaNo;

            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/profile");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string fileName = System.Guid.NewGuid() + Path.GetExtension(ProfileImage.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfileImage.CopyToAsync(stream);
                }

                user.ProfileImagePath = "/uploads/profile/" + fileName;
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
                return Json(new { success = true, message = "Profile updated successfully!" });

            return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

    }
}