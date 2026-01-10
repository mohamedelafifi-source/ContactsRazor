using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ContactsRazor.Pages;

[AllowAnonymous]
public class LogoutModel : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        await LogoutUserAsync();
        
        // Don't redirect immediately - let the page render first with redirect meta tag
        // This ensures Safari sees the content before redirecting
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LogoutUserAsync();
        
        // Don't redirect immediately - let the page render first
        return Page();
    }

    private async Task LogoutUserAsync()
    {
        // Step 1: Sign out from authentication FIRST
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        // Step 2: Delete cookie with exact same settings it was created with
        var cookieOptions = new Microsoft.AspNetCore.Http.CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(-1),
            HttpOnly = true,
            Secure = false,
            SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
            Path = "/"
        };
        
        // Delete the cookie by setting it to empty and expired
        Response.Cookies.Append(".AspNetCore.Cookies", "", cookieOptions);
        Response.Cookies.Delete(".AspNetCore.Cookies", cookieOptions);
        
        // Note: Session is not configured in this application, so we don't clear it
    }
}
