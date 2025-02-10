using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using WebApplication1.Model;

namespace WebApplication1.Pages
{
    public class Verify2FAModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;

        public Verify2FAModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
        }

        [BindProperty]
        public string OtpCode { get; set; }
        public string ErrorMessage { get; private set; }

        public async Task<IActionResult> OnPostAsync(string userId, bool rememberMe)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Invalid session. Please log in again.";
                return RedirectToPage("Login");
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToPage("Login");
            }

            // Validate the OTP Code
            var isValid = await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, OtpCode);
            if (!isValid)
            {
                TempData["ErrorMessage"] = "Invalid OTP code. Please try again.";
                return Page();
            }

            // Sign in the user after successful verification
            await signInManager.SignInAsync(user, rememberMe);
            return RedirectToPage("Index");
        }
    }
}
