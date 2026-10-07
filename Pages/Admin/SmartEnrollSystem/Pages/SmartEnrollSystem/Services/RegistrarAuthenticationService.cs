using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Ascendia.Services
{
    public class RegistrarAuthenticationService
    {
        private readonly UserManager<RegistrarAccount> _userManager;
        private readonly SignInManager<RegistrarAccount> _signInManager;
        private readonly ILogger<RegistrarAuthenticationService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RegistrarAuthenticationService(
            UserManager<RegistrarAccount> userManager,
            SignInManager<RegistrarAccount> signInManager,
            ILogger<RegistrarAuthenticationService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            var result = await _signInManager.PasswordSignInAsync(username, password, isPersistent: false, lockoutOnFailure: false);
            return result.Succeeded;
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<bool> ChangeCredentialsAsync(string currentPassword, string newPassword, string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                _logger.LogInformation("User changed their password successfully.");
                return true;
            }

            foreach (var error in result.Errors)
            {
                _logger.LogError(error.Description);
            }

            return false;
        }

        public async Task<RegistrarAccount> GetCurrentRegistrarAsync()
        {
            var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return userId != null ? await _userManager.FindByIdAsync(userId) : null;
        }
    }
}