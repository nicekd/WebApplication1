using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using WebApplication1.Model;
using WebApplication1.ViewModels;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace WebApplication1.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public Login LModel { get; set; }

        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IEmailSender emailSender;

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.emailSender = emailSender;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
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

            // ✅ Step 1: Check password first
            var passwordCheck = await signInManager.CheckPasswordSignInAsync(user, LModel.Password, false);
            if (!passwordCheck.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return Page();
            }

            // ✅ Step 2: Check if 2FA is enabled
            if (await userManager.GetTwoFactorEnabledAsync(user))
            {
                var token = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
                await emailSender.SendEmailAsync(user.Email, "Your 2FA Code", $"Your verification code is: {token}");

                return RedirectToPage("Verify2FA", new { userId = user.Id, rememberMe = LModel.RememberMe });
            }


            // ✅ Step 5: Log in directly if 2FA is NOT enabled
            var result = await signInManager.PasswordSignInAsync(user, LModel.Password, LModel.RememberMe, false);

            if (result.Succeeded)
            {
                return RedirectToPage("Index");
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return Page();
        }
    }
}
