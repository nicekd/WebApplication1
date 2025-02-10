using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Model
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [DataType(DataType.CreditCard)]
        public string CreditCardNo { get; set; }  

        [Required]
        [Phone]
        public string MobileNo { get; set; }

        [Required]
        public string BillingAddress { get; set; }

        [Required]
        public string ShippingAddress { get; set; } 

        [Required]
        [DataType(DataType.Upload)]
        public string PhotoPath { get; set; }

        public DateTime? LastPasswordChangeDate { get; set; } // Track last password change
    }
}
