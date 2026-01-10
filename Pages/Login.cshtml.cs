using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ContactsRazor.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ContactsRazor.Pages;

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

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        
        // Aggressive cache-control headers for Safari
        Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate, max-age=0";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "Thu, 01 Jan 1970 00:00:00 GMT";
        
        // If user is already logged in, redirect to dashboard
        if (User.Identity?.IsAuthenticated == true)
        {
            Response.Redirect("/Dashboard");
        }
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        // Set cache headers even on POST
        Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate, max-age=0";
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
            return Page();
        }

        // Normalize ClubCode to uppercase for consistency
        var clubCode = user.ClubCode.ToUpper().Trim();

        // Create claims with normalized ClubCode
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("ClubCode", clubCode),
        };

        // Derive role from ClubCode - Federation is "FEDERE"
        var role = clubCode == "FEDERE" ? "Federation" : "ClubCaptain";
        claims.Add(new Claim("Role", role));

        var claimsIdentity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
            IsPersistent = false, // Don't persist across browser sessions
            AllowRefresh = false // Prevent cookie refresh to avoid caching issues
        };

        // Sign in
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        // Redirect to return URL or default page
        returnUrl ??= "/Dashboard";
        return LocalRedirect(returnUrl);
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
