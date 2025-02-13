using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using WebApplication1.Model;
using WebApplication1.ViewModels;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace WebApplication1.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IConfiguration configuration;

        [BindProperty]
        public Register RModel { get; set; }

        public RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
                             RoleManager<IdentityRole> roleManager, IWebHostEnvironment webHostEnvironment,
                             IConfiguration configuration)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.roleManager = roleManager;
            this.webHostEnvironment = webHostEnvironment;
            this.configuration = configuration;
        }

        public void OnGet()
        {
        }

        // Save data into the database
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // ✅ Sanitize user input
            SanitizeInput();

            // Encrypt Credit Card Number using settings from appsettings.json
            string encryptedCreditCard = EncryptCreditCard(RModel.CreditCardNo);

            // Create ApplicationUser object
            var user = new ApplicationUser()
            {
                UserName = RModel.Email.Trim(),
                Email = RModel.Email.Trim(),
                FirstName = RModel.FirstName,
                LastName = RModel.LastName,
                MobileNo = RModel.MobileNo,
                BillingAddress = RModel.BillingAddress,
                ShippingAddress = RModel.ShippingAddress,
                CreditCardNo = encryptedCreditCard,
                TwoFactorEnabled = true
            };

            // Save Profile Photo
            if (RModel.Photo != null)
            {
                string uploadsFolder = Path.Combine(webHostEnvironment.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder); // Ensure folder exists

                string uniqueFileName = $"{user.Id}.jpg";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await RModel.Photo.CopyToAsync(fileStream);
                }

                user.PhotoPath = "/uploads/" + uniqueFileName; // Save relative path in database
            }

            var result = await userManager.CreateAsync(user, RModel.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    if (error.Code == "DuplicateUserName") // Identity treats Email as UserName
                    {
                        ModelState.AddModelError("", $"Email '{RModel.Email}' is already in use.");
                    }
                    else
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
                return Page();
            }


            // Assign Default Role (if needed)
            IdentityRole role = await roleManager.FindByNameAsync("User");
            if (role == null)
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
            }
            await userManager.AddToRoleAsync(user, "User");

            // Sign in and redirect to homepage
            return RedirectToPage("Login");
        }

        // ✅ Data Sanitization Method
        // ✅ Data Sanitization & Encoding Method
        private void SanitizeInput()
        {
            RModel.FirstName = HttpUtility.HtmlEncode(CleanText(RModel.FirstName));
            RModel.LastName = HttpUtility.HtmlEncode(CleanText(RModel.LastName));
            RModel.Email = HttpUtility.HtmlEncode(RModel.Email.Trim());
            RModel.MobileNo = Regex.Replace(RModel.MobileNo, @"[^0-9+]", ""); // Only allow digits and +
            RModel.BillingAddress = HttpUtility.HtmlEncode(CleanAddress(RModel.BillingAddress));
            RModel.ShippingAddress = HttpUtility.HtmlEncode(CleanAddress(RModel.ShippingAddress));
        }


        // ✅ Removes special characters except spaces (For Names)
        private string CleanText(string input)
        {
            return string.IsNullOrWhiteSpace(input) ? "" : Regex.Replace(input.Trim(), @"[^a-zA-Z\s]", "");
        }

        // ✅ Cleans Address Fields (Allows letters, numbers, spaces, and common address symbols)
        private string CleanAddress(string input)
        {
            return string.IsNullOrWhiteSpace(input) ? "" : Regex.Replace(input.Trim(), @"[^a-zA-Z0-9\s,.-]", "");
        }

        // Encrypt Credit Card Number before storing
        private string EncryptCreditCard(string creditCardNo)
        {
            byte[] key = Encoding.UTF8.GetBytes(configuration["EncryptionSettings:Key"]);
            byte[] iv = Encoding.UTF8.GetBytes(configuration["EncryptionSettings:IV"]);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(creditCardNo);
                    byte[] encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    return Convert.ToBase64String(encrypted);
                }
            }
        }
    }
}
