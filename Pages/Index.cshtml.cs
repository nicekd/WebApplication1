using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using WebApplication1.Model;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WebApplication1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IConfiguration configuration;

        public ApplicationUser CurrentUser { get; private set; }
        public string DecryptedCreditCardNo { get; private set; }
        public bool TwoFactorEnabled { get; private set; } // Track 2FA status

        public IndexModel(UserManager<ApplicationUser> userManager,
                          SignInManager<ApplicationUser> signInManager,
                          IConfiguration configuration)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // ✅ Redirect to login if not authenticated
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("Login");
            }

            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("Login");
            }

            // ✅ Check if user requires 2FA but hasn't verified yet
            if (user.TwoFactorEnabled && !(await signInManager.IsTwoFactorClientRememberedAsync(user)))
            {
                return RedirectToPage("Verify2FA", new { userId = user.Id });
            }

            // ✅ Load user details
            CurrentUser = user;
            TwoFactorEnabled = user.TwoFactorEnabled;

            if (!string.IsNullOrEmpty(user.CreditCardNo))
            {
                DecryptedCreditCardNo = DecryptCreditCard(user.CreditCardNo);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostEnable2FA()
        {
            var user = await userManager.GetUserAsync(User);
            if (user != null)
            {
                await userManager.SetTwoFactorEnabledAsync(user, true);
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDisable2FA()
        {
            var user = await userManager.GetUserAsync(User);
            if (user != null)
            {
                await userManager.SetTwoFactorEnabledAsync(user, false);
            }
            return RedirectToPage();
        }

        // Decrypt Credit Card Number using Key & IV from appsettings.json
        private string DecryptCreditCard(string encryptedCreditCard)
        {
            try
            {
                byte[] key = Encoding.UTF8.GetBytes(configuration["EncryptionSettings:Key"]);
                byte[] iv = Encoding.UTF8.GetBytes(configuration["EncryptionSettings:IV"]);
                byte[] encryptedBytes = Convert.FromBase64String(encryptedCreditCard);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    {
                        byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
            }
            catch
            {
                return "Decryption Failed";
            }
        }
    }
}
