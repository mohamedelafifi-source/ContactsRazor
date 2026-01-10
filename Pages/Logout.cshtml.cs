using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ContactsRazor.Pages;

public class LogoutModel : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        await LogoutUserAsync();
        // Use redirect with timestamp to prevent caching
        return Redirect($"/Login?t={DateTimeOffset.UtcNow.Ticks}");
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LogoutUserAsync();
        // Use redirect with timestamp to prevent caching
        return Redirect($"/Login?t={DateTimeOffset.UtcNow.Ticks}");
    }

    private async Task LogoutUserAsync()
    {
        // Sign out the user - this should handle cookie deletion
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        // Aggressively delete cookies with all possible combinations
        // Cookie name is typically ".AspNetCore.Cookies" or ".AspNetCore.Identity.Application"
        var cookieNames = new[] { ".AspNetCore.Cookies", ".AspNetCore.Identity.Application" };
        
        var cookieOptions = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(-1),
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        };
        
        // Delete with Lax sameSite
        foreach (var cookieName in cookieNames)
        {
            Response.Cookies.Delete(cookieName, cookieOptions);
        }
        
        // Delete with None sameSite (in case it was set differently)
        var cookieOptionsNone = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(-1),
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.None,
            Path = "/"
        };
        
        foreach (var cookieName in cookieNames)
        {
            Response.Cookies.Delete(cookieName, cookieOptionsNone);
        }
        
        // Delete with Strict sameSite
        var cookieOptionsStrict = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(-1),
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        };
        
        foreach (var cookieName in cookieNames)
        {
            Response.Cookies.Delete(cookieName, cookieOptionsStrict);
        }
        
        // Clear any session data if session is enabled
        if (HttpContext.Session != null)
        {
            HttpContext.Session.Clear();
        }
        
        // Aggressive cache-control headers
        Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate, max-age=0");
        Response.Headers.Add("Pragma", "no-cache");
        Response.Headers.Add("Expires", "Thu, 01 Jan 1970 00:00:00 GMT");
    }
}
