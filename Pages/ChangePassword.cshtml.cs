using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using WebApplication1.Model;
using WebApplication1.Services;
using System;

namespace WebApplication1.Pages
{
    public class ChangePasswordModel : PageModel
    {
        private readonly CustomUserManager userManager;
        private readonly SignInManager<ApplicationUser> signInManager;

        public ChangePasswordModel(CustomUserManager userManager,
                                   SignInManager<ApplicationUser> signInManager)
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

            var userId = userManager.GetUserId(User); // Get User ID from claims
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return RedirectToPage("Index");

            // ✅ Change Password Using CustomUserManager (Handles History + Age Policies)
            var result = await userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            await signInManager.RefreshSignInAsync(user);
            return RedirectToPage("Index");
        }

    }
}
