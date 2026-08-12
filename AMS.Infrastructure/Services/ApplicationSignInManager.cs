using AMS.Domain.Entities;
using AMS.Application.Features.Home.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AMS.Infrastructure.Services
{
    public class ApplicationSignInManager : SignInManager<ApplicationUser>
    {
        public ApplicationSignInManager(
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<ApplicationUser>> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<ApplicationUser> confirmation)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation) { }

        public override async Task<bool> CanSignInAsync(ApplicationUser user)
        {
            if (!await base.CanSignInAsync(user)) return false;

            if (!user.IsApproved) return false;

            var roles = await UserManager.GetRolesAsync(user);
            if (roles.Count == 1 && roles.Contains("User")) return false;

            return true;
        }
    }
}
