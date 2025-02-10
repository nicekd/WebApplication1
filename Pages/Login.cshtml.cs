using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using WebApplication1.Model;
using WebApplication1.ViewModels;

namespace WebApplication1.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public Login LModel { get; set; }
        public string ErrorMessage { get; private set; }

        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(LModel.Email) || string.IsNullOrEmpty(LModel.Password))
                {
                    ModelState.AddModelError(string.Empty, "Please fill in both email and password.");
                    return Page();
                }
            }

            var user = await userManager.FindByEmailAsync(LModel.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return Page();
            }

            var identityResult = await signInManager.PasswordSignInAsync(user, LModel.Password, LModel.RememberMe, false);

            if (identityResult.Succeeded)
            {
                return RedirectToPage("Index");
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return Page();
        }
    }
}
