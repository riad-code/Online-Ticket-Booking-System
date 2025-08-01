﻿using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; // Make sure this is added for AnyAsync

namespace ONLINE_TICKET_BOOKING_SYSTEM.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IWebHostEnvironment _env; // For saving uploaded file

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
        [FromForm]  // <--- Add this attribute to fix file upload binding
        public InputModel Input { get; set; } = new();

        public string ReturnUrl { get; set; } = string.Empty;

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public class InputModel
        {
            [Display(Name = "Title")]
            public string Title { get; set; } = string.Empty;

            [Required]
            [Display(Name = "First Name")]
            public string FirstName { get; set; } = string.Empty;
            [Required]
            [Display(Name = "Last Name")]
            public string LastName { get; set; } = string.Empty;


            [Required]
            [RegularExpression(@"^\d{11}$", ErrorMessage = "Mobile Number must be exactly 11 digits.")]
            [Display(Name = "Mobile Number")]
            public string MobileNumber { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Gender")]
            public string Gender { get; set; } = string.Empty;

            [DataType(DataType.Date)]
            [Display(Name = "Date of Birth")]
            public DateTime? DateOfBirth { get; set; }

            [Display(Name = "Address")]
            public string Address { get; set; } = string.Empty;

            [Display(Name = "NID Number")]
            public string NidNo { get; set; } = string.Empty;

            [Display(Name = "Passport Number")]
            public string PassportNo { get; set; } = string.Empty;

            [Display(Name = "Visa Number")]
            public string VisaNo { get; set; } = string.Empty;

            [Display(Name = "Profile Image")]
            public IFormFile? ProfileImage { get; set; } // For image upload

            [Required]
            [StringLength(100, MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
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
                // *** UNIQUE VALIDATION CHECKS ADDED HERE ***

                if (!string.IsNullOrEmpty(Input.NidNo))
                {
                    var nidExists = await _userManager.Users.AnyAsync(u => u.NidNo == Input.NidNo);
                    if (nidExists)
                    {
                        ModelState.AddModelError("Input.NidNo", "NID Number is already registered.");
                    }
                }

                if (!string.IsNullOrEmpty(Input.PassportNo))
                {
                    var passportExists = await _userManager.Users.AnyAsync(u => u.PassportNo == Input.PassportNo);
                    if (passportExists)
                    {
                        ModelState.AddModelError("Input.PassportNo", "Passport Number is already registered.");
                    }
                }

                if (!string.IsNullOrEmpty(Input.VisaNo))
                {
                    var visaExists = await _userManager.Users.AnyAsync(u => u.VisaNo == Input.VisaNo);
                    if (visaExists)
                    {
                        ModelState.AddModelError("Input.VisaNo", "Visa Number is already registered.");
                    }
                }

                if (!ModelState.IsValid)
                {
                    return Page();
                }

                string profileImagePath = "/images/default-avatar.png"; // Default image if none uploaded

                // Save uploaded image if provided
                if (Input.ProfileImage != null && Input.ProfileImage.Length > 0)
                {
                    var uploadFolder = Path.Combine(_env.WebRootPath, "uploads/profile-images");
                    Directory.CreateDirectory(uploadFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(Input.ProfileImage.FileName);
                    var filePath = Path.Combine(uploadFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await Input.ProfileImage.CopyToAsync(fileStream);
                    }

                    profileImagePath = "/uploads/profile-images/" + uniqueFileName;
                }

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
                    ProfileImagePath = profileImagePath
                };

                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // Assign default role to every new user
                    await _userManager.AddToRoleAsync(user, "User");

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
                                  $@"
                                    <h2>Congratulations, {Input.Email}!</h2>
                                        <p>Your account has been successfully created. Welcome ! 🎉</p>
                                  ");


                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Log model errors for debugging
            if (!ModelState.IsValid)
            {
                foreach (var entry in ModelState)
                {
                    foreach (var error in entry.Value.Errors)
                    {
                        _logger.LogError($"Model error on {entry.Key}: {error.ErrorMessage}");
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
