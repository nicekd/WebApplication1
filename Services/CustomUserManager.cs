using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplication1.Model;

namespace WebApplication1.Services
{
    public class CustomUserManager : UserManager<ApplicationUser>
    {
        public CustomUserManager(IUserStore<ApplicationUser> store, IOptions<IdentityOptions> optionsAccessor,
            IPasswordHasher<ApplicationUser> passwordHasher, IEnumerable<IUserValidator<ApplicationUser>> userValidators,
            IEnumerable<IPasswordValidator<ApplicationUser>> passwordValidators, ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<ApplicationUser>> logger)
            : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
        {
        }

        public override async Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword)
        {
            TimeSpan minPasswordAge = TimeSpan.FromDays(0);  // User must keep password for at least 1 day
            TimeSpan maxPasswordAge = TimeSpan.FromDays(30); // Force password change every 30 days

            // ✅ Check Minimum Password Age
            if (user.LastPasswordChangeDate.HasValue && user.LastPasswordChangeDate.Value.Add(minPasswordAge) > DateTime.UtcNow)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "MinPasswordAge",
                    Description = "You must keep your password for at least 1 day before changing it."
                });
            }

            // ✅ Check Maximum Password Age
            if (user.LastPasswordChangeDate.HasValue && user.LastPasswordChangeDate.Value.Add(maxPasswordAge) < DateTime.UtcNow)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "MaxPasswordAge",
                    Description = "Your password has expired. Please change your password."
                });
            }

            // ✅ Verify if new password matches the last 2 stored password hashes
            if (!string.IsNullOrEmpty(user.PreviousPassword1) &&
                PasswordHasher.VerifyHashedPassword(user, user.PreviousPassword1, newPassword) == PasswordVerificationResult.Success)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "PasswordReuse",
                    Description = "You cannot reuse your last 2 passwords."
                });
            }

            if (!string.IsNullOrEmpty(user.PreviousPassword2) &&
                PasswordHasher.VerifyHashedPassword(user, user.PreviousPassword2, newPassword) == PasswordVerificationResult.Success)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "PasswordReuse",
                    Description = "You cannot reuse your last 2 passwords."
                });
            }

            // ✅ Change Password
            var result = await base.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                // ✅ Shift password history
                user.PreviousPassword2 = user.PreviousPassword1;  // Move PreviousPassword1 to PreviousPassword2
                user.PreviousPassword1 = PasswordHasher.HashPassword(user, newPassword); // Store new password in PreviousPassword1

                // ✅ Update last password change date
                user.LastPasswordChangeDate = DateTime.UtcNow;
                await UpdateAsync(user);
            }

            return result;
        }
    }
}
