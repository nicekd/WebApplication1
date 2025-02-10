using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
    public class Login
    {
        [Required(ErrorMessage = "Email Address is required.")]
        [DataType(DataType.EmailAddress)]
        [EmailAddress(ErrorMessage = "Invalid Email Address format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d@$!%*?&]+$",
            ErrorMessage = "Password must contain at least one letter and one number.")]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}
