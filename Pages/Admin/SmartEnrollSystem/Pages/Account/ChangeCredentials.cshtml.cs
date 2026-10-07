using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace Ascendia.Pages.Account
{
    public class ChangeCredentialsModel : PageModel
    {
        private readonly UserManager<RegistrarAccount> _userManager;
        private readonly SignInManager<RegistrarAccount> _signInManager;

        public ChangeCredentialsModel(UserManager<RegistrarAccount> userManager, SignInManager<RegistrarAccount> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public string NewUsername { get; set; }

        [BindProperty]
        public string NewPassword { get; set; }

        [BindProperty]
        public string CurrentPassword { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostChangeUsernameAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var result = await _userManager.SetUserNameAsync(user, NewUsername);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                return RedirectToPage("ChangeCredentials", new { Message = "Username changed successfully." });
            }

            return Page();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var result = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                return RedirectToPage("ChangeCredentials", new { Message = "Password changed successfully." });
            }

            return Page();
        }
    }
}