using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WebApplication1.Model;
using WebApplication1.ViewModels;
using Microsoft.AspNetCore.Identity.UI.Services;
using System;

namespace WebApplication1.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public Login LModel { get; set; }

        public string RecaptchaScoreMessage { get; private set; } // Store reCAPTCHA score message for debugging

        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IEmailSender emailSender;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly AuthDbContext dbContext; // ✅ Added DbContext for Audit Logging

        private const string RecaptchaSecretKey = "6LfA9tIqAAAAAFOa5uNXzzn0DtYP7aW6sYqIj3jl"; // 🔹 Hardcoded reCAPTCHA Secret Key
        private const float RecaptchaThreshold = 0.5f; // 🔹 Adjust the threshold as needed

        public LoginModel(SignInManager<ApplicationUser> signInManager,
                          UserManager<ApplicationUser> userManager,
                          IEmailSender emailSender,
                          IHttpClientFactory httpClientFactory,
                          AuthDbContext dbContext) // ✅ Injected DbContext
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.emailSender = emailSender;
            this.httpClientFactory = httpClientFactory;
            this.dbContext = dbContext;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // ✅ Step 1: Get reCAPTCHA token from form submission
            var recaptchaResponse = Request.Form["g-recaptcha-response"];

            // ✅ Step 2: Validate reCAPTCHA response
            var recaptchaValid = await ValidateRecaptcha(recaptchaResponse);
            RecaptchaScoreMessage = $"reCAPTCHA Score: {recaptchaValid.score}"; // Debugging purposes

            if (!recaptchaValid.success)
            {
                ModelState.AddModelError(string.Empty, $"reCAPTCHA verification failed. Score: {recaptchaValid.score}");
                return Page();
            }

            // ✅ Step 3: Validate email & password
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Please fill in both email and password.");
                return Page();
            }

            var user = await userManager.FindByEmailAsync(LModel.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return Page();
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"; // ✅ Get user's IP Address

            // ✅ Step 4: Check if the user is locked out
            if (await userManager.IsLockedOutAsync(user))
            {
                // ✅ Log failed login attempt due to lockout
                dbContext.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    Action = "Login Failed - Account Locked",
                    Timestamp = DateTime.UtcNow,
                    IPAddress = ipAddress
                });
                await dbContext.SaveChangesAsync();

                ModelState.AddModelError(string.Empty, "Your account has been locked due to multiple failed attempts. Please try again in 1 minute.");
                return Page();
            }

            // ✅ Step 5: Attempt login (enables lockout on failure)
            var result = await signInManager.PasswordSignInAsync(user, LModel.Password, false, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                await userManager.ResetAccessFailedCountAsync(user); // ✅ Reset failed attempt count on successful login

                // ✅ Step 6: Enforce Single Active Session Per User
                string newSessionId = Guid.NewGuid().ToString(); // Generate a new session ID
                if (user.SessionId != null)
                {
                    // If the user is already logged in somewhere else, log them out
                    await signInManager.SignOutAsync();
                }

                // ✅ Update session ID in the database
                user.SessionId = newSessionId;
                await userManager.UpdateAsync(user);

                // ✅ Store session in HttpContext
                HttpContext.Session.SetString("SessionId", newSessionId);

                // ✅ Log successful login
                dbContext.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    Action = "Login Success",
                    Timestamp = DateTime.UtcNow,
                    IPAddress = ipAddress
                });
                await dbContext.SaveChangesAsync();

                // ✅ Step 7: Check if 2FA is enabled
                if (await userManager.GetTwoFactorEnabledAsync(user))
                {
                    // ✅ Temporarily authenticate the user, requiring 2FA verification
                    await signInManager.SignInAsync(user, isPersistent: false, authenticationMethod: "TwoFactor");

                    var token = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
                    await emailSender.SendEmailAsync(user.Email, "Your 2FA Code", $"Your verification code is: {token}");

                    return RedirectToPage("Verify2FA", new { userId = user.Id, rememberMe = LModel.RememberMe });
                }

                // ✅ If 2FA is NOT enabled, log the user in fully
                await signInManager.SignInAsync(user, LModel.RememberMe);
                return RedirectToPage("Index");
            }
            else if (result.IsLockedOut)
            {
                // ✅ Log lockout event
                dbContext.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    Action = "Login Failed - Account Locked",
                    Timestamp = DateTime.UtcNow,
                    IPAddress = ipAddress
                });
                await dbContext.SaveChangesAsync();

                ModelState.AddModelError(string.Empty, "Your account has been locked due to multiple failed attempts. Please try again in 1 minute.");
                return Page();
            }
            else
            {
                // ✅ Increment failed login attempt count
                await userManager.AccessFailedAsync(user);

                // ✅ Log failed login attempt
                dbContext.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    Action = "Login Failed - Invalid Credentials",
                    Timestamp = DateTime.UtcNow,
                    IPAddress = ipAddress
                });
                await dbContext.SaveChangesAsync();

                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return Page();
            }
        }

        // 🔹 Validate reCAPTCHA response
        private async Task<(bool success, float score)> ValidateRecaptcha(string recaptchaResponse)
        {
            if (string.IsNullOrEmpty(recaptchaResponse))
            {
                return (false, 0f);
            }

            var client = httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={RecaptchaSecretKey}&response={recaptchaResponse}",
                null
            );

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var recaptchaResult = JsonSerializer.Deserialize<RecaptchaVerificationResponse>(jsonResponse);

            return (recaptchaResult.success && recaptchaResult.score >= RecaptchaThreshold, recaptchaResult.score);
        }

        private class RecaptchaVerificationResponse
        {
            public bool success { get; set; }
            public float score { get; set; }
        }
    }
}
