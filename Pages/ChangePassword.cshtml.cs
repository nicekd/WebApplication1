using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using WebApplication1.Model;
using System;

namespace WebApplication1.Pages
{
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;

        public ChangePasswordModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
        }

        [BindProperty]
        public ChangePasswordInputModel Input { get; set; }

        public class ChangePasswordInputModel
        {
            [Required]
            [DataType(DataType.Password)]
            public string CurrentPassword { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [MinLength(12, ErrorMessage = "Password must be at least 12 characters.")]
            [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*?&])[A-Za-z\\d@$!%*?&]{12,}$",
                ErrorMessage = "Password must include uppercase, lowercase, number, and special character.")]
            public string NewPassword { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("Index");

            // Ensure password can only be changed once per day
            if (user.LastPasswordChangeDate.HasValue && (DateTime.UtcNow - user.LastPasswordChangeDate.Value).TotalHours < 24)
            {
                ModelState.AddModelError(string.Empty, "You can only change your password once every 24 hours.");
                return Page();
            }

            var result = await userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            // Update last password change date
            user.LastPasswordChangeDate = DateTime.UtcNow;
            await userManager.UpdateAsync(user);

            await signInManager.RefreshSignInAsync(user);
            return RedirectToPage("Index");
        }
    }
}
