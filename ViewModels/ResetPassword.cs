using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
    public class ResetPasswordModel
    {
        [Required(ErrorMessage = "New Password is required.")]
        [DataType(DataType.Password)]
        [MinLength(12, ErrorMessage = "Password must be at least 12 characters.")]
        [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*?&])[A-Za-z\\d@$!%*?&]{12,}$",
            ErrorMessage = "Password must include uppercase, lowercase, number, and special character.")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm Password is required.")]  // ✅ Explicitly required
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
