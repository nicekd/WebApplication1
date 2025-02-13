using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WebApplication1.ViewModels
{
    public class Register
    {
        [Required]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "First Name can only contain letters and spaces.")]
        public string FirstName { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Last Name can only contain letters and spaces.")]
        public string LastName { get; set; }

        [Required]
        [DataType(DataType.CreditCard)]
        [RegularExpression(@"^\d{16,19}$", ErrorMessage = "Credit Card Number must be between 16 to 19 digits.")]
        public string CreditCardNo { get; set; }  // Will be encrypted before storing

        [Required]
        [Phone]
        [RegularExpression(@"^\+?\d{8,15}$", ErrorMessage = "Mobile Number must contain only digits and can include a leading + for country code.")]
        public string MobileNo { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9\s,.-]+$", ErrorMessage = "Billing Address contains invalid characters.")]
        public string BillingAddress { get; set; }

        [Required(ErrorMessage = "Shipping Address is required.")]
        public string ShippingAddress { get; set; }


        [Required]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(12, ErrorMessage = "Password must be at least 12 characters.")]
        [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*?&])[A-Za-z\\d@$!%*?&]{12,}$",
            ErrorMessage = "Password must include uppercase, lowercase, number, and special character.")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [DataType(DataType.Upload)]
        [AllowedFileExtensions(new string[] { ".jpg", ".jpeg" })] // ✅ Custom Validation for JPG
        public IFormFile Photo { get; set; }  // JPG only
    }
}
