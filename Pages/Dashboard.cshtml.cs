using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ContactsRazor.Helpers;

namespace ContactsRazor.Pages;

[Authorize]
public class DashboardModel : PageModel
{
    public IActionResult OnGet()
    {
        // Server-side authentication check - [Authorize] attribute handles most of this
        // But double-check claims exist
        var clubCode = User.FindFirst("ClubCode")?.Value;
        if (string.IsNullOrEmpty(clubCode))
        {
            // Invalid authentication - clear cookie and redirect to login
            Response.Cookies.Delete(".AspNetCore.Cookies");
            return Redirect("/Login?t=" + DateTimeOffset.UtcNow.Ticks);
        }
        
        // Set no-cache headers using OnStarting
        Response.OnStarting(() =>
        {
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate, max-age=0, private";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "Thu, 01 Jan 1970 00:00:00 GMT";
            Response.Headers["Vary"] = "Cookie";
            return Task.CompletedTask;
        });
        
        return Page();
    }
}
