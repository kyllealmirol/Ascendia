using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Ascendia.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public async Task<IActionResult> OnGet()
        {
            // Clear the session and sign out the user
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync();

            // Redirect to the login page after logout
            return RedirectToPage("/Account/Login");
        }
    }
}