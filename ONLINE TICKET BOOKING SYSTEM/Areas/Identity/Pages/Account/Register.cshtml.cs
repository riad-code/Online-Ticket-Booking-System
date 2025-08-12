using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IWebHostEnvironment _env;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _env = env;
        }

        [BindProperty]
        [FromForm]
        public InputModel Input { get; set; } = new();

        public string ReturnUrl { get; set; } = string.Empty;

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public class InputModel
        {
            [Display(Name = "Title")]
            public string Title { get; set; } = string.Empty;

            [Required] public string FirstName { get; set; } = string.Empty;
            [Required] public string LastName { get; set; } = string.Empty;

            [Required]
            [RegularExpression(@"^\d{11}$", ErrorMessage = "Mobile Number must be exactly 11 digits.")]
            public string MobileNumber { get; set; } = string.Empty;

            [Required, EmailAddress] public string Email { get; set; } = string.Empty;
            [Required] public string Gender { get; set; } = string.Empty;

            [DataType(DataType.Date)]
            public DateTime? DateOfBirth { get; set; }

            public string Address { get; set; } = string.Empty;
            public string? NidNo { get; set; }         
            public string? PassportNo { get; set; }     
            public string? VisaNo { get; set; }

            public IFormFile? ProfileImage { get; set; }

            [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // DOB check
                if (!Input.DateOfBirth.HasValue)
                {
                    ModelState.AddModelError("Input.DateOfBirth", "Date of birth is required.");
                }
                else
                {
                    var cutoff = DateTime.Today.AddYears(-18);
                    if (Input.DateOfBirth.Value.Date > cutoff)
                        ModelState.AddModelError("Input.DateOfBirth", "You must be at least 18 years old.");
                }

                // Unique mobile
                var mobile = Input.MobileNumber?.Trim();
                if (!string.IsNullOrEmpty(mobile))
                {
                    var mobileExists = await _userManager.Users.AnyAsync(u => u.MobileNumber == mobile);
                    if (mobileExists)
                        ModelState.AddModelError("Input.MobileNumber", "This mobile number is already registered.");
                }

                // Optional IDs uniqueness
                if (!string.IsNullOrEmpty(Input.NidNo))
                {
                    var nidExists = await _userManager.Users.AnyAsync(u => u.NidNo == Input.NidNo);
                    if (nidExists) ModelState.AddModelError("Input.NidNo", "NID Number is already registered.");
                }
                if (!string.IsNullOrEmpty(Input.PassportNo))
                {
                    var passportExists = await _userManager.Users.AnyAsync(u => u.PassportNo == Input.PassportNo);
                    if (passportExists) ModelState.AddModelError("Input.PassportNo", "Passport Number is already registered.");
                }
                if (!string.IsNullOrEmpty(Input.VisaNo))
                {
                    var visaExists = await _userManager.Users.AnyAsync(u => u.VisaNo == Input.VisaNo);
                    if (visaExists) ModelState.AddModelError("Input.VisaNo", "Visa Number is already registered.");
                }

                // Image size
                if (Input.ProfileImage != null && Input.ProfileImage.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError("Input.ProfileImage", "Image must be 2 MB or less.");
            }

            // If any errors after checks
            if (!ModelState.IsValid)
            {
                // Log all errors to help debugging
                foreach (var kvp in ModelState)
                {
                    foreach (var error in kvp.Value.Errors)
                    {
                        _logger.LogWarning($"Validation failed for {kvp.Key}: {error.ErrorMessage}");
                    }
                }

                if (IsAjaxRequest())
                {
                    var errs = ModelState.Where(kvp => kvp.Value.Errors.Any())
                                         .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray());
                    return new JsonResult(new { success = false, errors = errs });
                }
                return Page();
            }

            // Save image
            string profileImagePath = "/images/default-avatar.png";
            if (Input.ProfileImage != null && Input.ProfileImage.Length > 0)
            {
                var uploadFolder = Path.Combine(_env.WebRootPath, "uploads/profile-images");
                Directory.CreateDirectory(uploadFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(Input.ProfileImage.FileName);
                var filePath = Path.Combine(uploadFolder, uniqueFileName);
                using var fs = new FileStream(filePath, FileMode.Create);
                await Input.ProfileImage.CopyToAsync(fs);
                profileImagePath = "/uploads/profile-images/" + uniqueFileName;
            }
            // Bangladesh local time fix (Windows + Linux safe)
            var bdTimeZoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "Bangladesh Standard Time"
                : "Asia/Dhaka";
            var bdTimeZone = TimeZoneInfo.FindSystemTimeZoneById(bdTimeZoneId);
            var bangladeshTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, bdTimeZone);

            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                Title = Input.Title,
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                Gender = Input.Gender,
                DateOfBirth = Input.DateOfBirth,
                Address = Input.Address,
                NidNo = Input.NidNo,
                PassportNo = Input.PassportNo,
                VisaNo = Input.VisaNo,
                MobileNumber = Input.MobileNumber,
                ProfileImagePath = profileImagePath,
                RegisteredAtUtc = bangladeshTime
            };

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                // Add default role
                var addRole = await _userManager.AddToRoleAsync(user, "User");
                if (!addRole.Succeeded)
                    _logger.LogWarning("AddToRole failed: {errs}", string.Join(", ", addRole.Errors.Select(e => e.Description)));

                // Try email
                try
                {
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = user.Id, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(
                        Input.Email,
                        "🎉 Congratulations!",
                        $@"<h2>Congratulations, {Input.Email}!</h2>
                           <p>Your account has been successfully created. Welcome! 🎉</p>");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Email sending failed; continuing registration.");
                }

                var msg = _userManager.Options.SignIn.RequireConfirmedAccount
                    ? "Account created. Please confirm your email, then log in."
                    : "Registration successful. Please log in.";

                if (IsAjaxRequest())
                    return new JsonResult(new { success = true, message = msg, redirectUrl = Url.Page("./Login") });

                TempData["RegisterSuccess"] = msg;
                return RedirectToPage("./Login");
            }

            // Add identity errors to ModelState
            foreach (var error in result.Errors)
            {
                _logger.LogWarning($"Identity create failed: {error.Description}");
                ModelState.AddModelError(string.Empty, error.Description);
            }

            if (IsAjaxRequest())
            {
                var errs = ModelState.Where(kvp => kvp.Value.Errors.Any())
                                     .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray());
                return new JsonResult(new { success = false, errors = errs });
            }

            return Page();
        }

        private bool IsAjaxRequest()
        {
            var xv = Request?.Headers["X-Requested-With"].ToString();
            return string.Equals(xv, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }
    }
}
