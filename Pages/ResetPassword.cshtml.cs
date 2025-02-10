using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using WebApplication1.Model;

namespace WebApplication1.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> userManager;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager)
        {
            this.userManager = userManager;
        }

        [BindProperty]
        public string UserId { get; set; }

        [BindProperty]
        public string Token { get; set; }

        [BindProperty]
        [Required]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [BindProperty]
        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        public void OnGet(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                ModelState.AddModelError("", "Invalid password reset request.");
            }

            UserId = userId;
            Token = token;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            System.Diagnostics.Debug.WriteLine("ResetPassword OnPostAsync method triggered.");

            var user = await userManager.FindByIdAsync(UserId);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid user.");
                return Page();
            }

            var result = await userManager.ResetPasswordAsync(user, Token, NewPassword);
            if (result.Succeeded)
            {
                System.Diagnostics.Debug.WriteLine("Password reset successful.");
                return RedirectToPage("ResetPasswordSuccess"); // ✅ Redirecting to success page
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            System.Diagnostics.Debug.WriteLine("Password reset failed.");
            return Page(); // Stay on the same page if reset fails
        }

    }
}
