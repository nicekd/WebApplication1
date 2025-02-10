using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using WebApplication1.Model;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace WebApplication1.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IEmailSender emailSender;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            this.userManager = userManager;
            this.emailSender = emailSender;
        }

        [BindProperty]
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public bool EmailSent { get; set; } = false;

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email not found.");
                return Page();
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Page(
                "/ResetPassword",
                pageHandler: null,
                values: new { userId = user.Id, token = await userManager.GeneratePasswordResetTokenAsync(user) },
                protocol: Request.Scheme
);
            await emailSender.SendEmailAsync(user.Email, "Reset Password", $"Click here to reset your password: <a href='{resetLink}'>Reset Password</a>");


            EmailSent = true;
            return Page();
        }
    }
}
