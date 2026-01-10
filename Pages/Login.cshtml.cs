using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ContactsRazor.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ContactsRazor.Pages;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly AuthService _authService;

    public LoginModel(AuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public IActionResult OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        
        // CRITICAL: Delete authentication cookie when accessing Login page
        var cookieOptions = new Microsoft.AspNetCore.Http.CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(-1),
            Path = "/",
            SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
            HttpOnly = true,
            Secure = false
        };
        
        // Delete by setting empty value
        Response.Cookies.Append(".AspNetCore.Cookies", "", cookieOptions);
        Response.Cookies.Delete(".AspNetCore.Cookies", cookieOptions);
        
        // If logout parameter is present, force sign out
        if (Request.Query.ContainsKey("logout"))
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).Wait();
        }
        
        // NEVER redirect authenticated users to Dashboard from Login - always show Login
        if (User.Identity?.IsAuthenticated == true)
        {
            // Force logout
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).Wait();
            Response.Cookies.Append(".AspNetCore.Cookies", "", cookieOptions);
        }
        
        // Let Razor Pages handle Content-Type automatically - don't set it manually
        // This prevents Safari from downloading the page
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        // Ensure Content-Type
        Response.ContentType = "text/html; charset=utf-8";
        
        // Set no-cache headers
        Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate, max-age=0, private";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "Thu, 01 Jan 1970 00:00:00 GMT";
        
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Authenticate user
        var user = await _authService.AuthenticateAsync(Input.Username!.Trim(), Input.Password!);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            Input.Password = string.Empty;
            return Page();
        }

        // Normalize ClubCode
        var clubCode = user.ClubCode.ToUpper().Trim();

        // Create claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("ClubCode", clubCode),
        };

        var role = clubCode == "FEDERE" ? "Federation" : "ClubCaptain";
        claims.Add(new Claim("Role", role));

        var claimsIdentity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
            IsPersistent = false,
            AllowRefresh = false
        };

        // Sign in
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        // Redirect to Dashboard with timestamp
        returnUrl ??= "/Dashboard";
        var timestamp = DateTimeOffset.UtcNow.Ticks;
        var redirectUrl = returnUrl.Contains('?') 
            ? $"{returnUrl}&t={timestamp}" 
            : $"{returnUrl}?t={timestamp}";
        return LocalRedirect(redirectUrl);
    }
}

public class LoginInput
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 20 characters")]
    [Display(Name = "Username")]
    public string? Username { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string? Password { get; set; }
}
