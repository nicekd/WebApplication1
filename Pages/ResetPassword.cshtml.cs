using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using WebApplication1.Model;

namespace WebApplication1.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IPasswordHasher<ApplicationUser> passwordHasher;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager)
        {
            this.userManager = userManager;
            this.passwordHasher = userManager.PasswordHasher; // Get Password Hasher from UserManager
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
            var user = await userManager.FindByIdAsync(UserId);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid user.");
                return Page();
            }

            // ✅ Prevent reusing the last 2 passwords
            if (!string.IsNullOrEmpty(user.PreviousPassword1))
            {
                var result1 = passwordHasher.VerifyHashedPassword(user, user.PreviousPassword1, NewPassword);
                if (result1 == PasswordVerificationResult.Success)
                {
                    ModelState.AddModelError("", "You cannot reuse your last 2 passwords.");
                    return Page();
                }
            }

            if (!string.IsNullOrEmpty(user.PreviousPassword2))
            {
                var result2 = passwordHasher.VerifyHashedPassword(user, user.PreviousPassword2, NewPassword);
                if (result2 == PasswordVerificationResult.Success)
                {
                    ModelState.AddModelError("", "You cannot reuse your last 2 passwords.");
                    return Page();
                }
            }

            // ✅ Reset Password
            var resetResult = await userManager.ResetPasswordAsync(user, Token, NewPassword);
            if (resetResult.Succeeded)
            {
                // ✅ Shift password history
                user.PreviousPassword2 = user.PreviousPassword1;  // Move PreviousPassword1 to PreviousPassword2
                user.PreviousPassword1 = passwordHasher.HashPassword(user, NewPassword); // Store new password in PreviousPassword1

                // ✅ Update user in the database
                await userManager.UpdateAsync(user);

                return RedirectToPage("ResetPasswordSuccess");
            }

            foreach (var error in resetResult.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return Page();
        }
    }
}
