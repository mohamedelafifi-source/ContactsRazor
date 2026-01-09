using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ContactsRazor.Data;
using ContactsRazor.Models;
using ContactsRazor.Services;
using ContactsRazor.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text;

namespace ContactsRazor.Pages;

[Authorize(Policy = "FederationOnly")]
public class UsersModel : PageModel
{
    private readonly ContactsDbContext _context;
    private readonly AuthService _authService;

    public UsersModel(ContactsDbContext context, AuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    public List<User> AllUsers { get; set; } = new();
    public List<Club> AllClubs { get; set; } = new();

    [BindProperty]
    public UserInput NewUser { get; set; } = new();

    public async Task OnGetAsync()
    {
        AllUsers = await _context.Users
            .OrderBy(u => u.ClubCode)
            .ThenBy(u => u.Username)
            .ToListAsync();

        AllClubs = await _context.Clubs
            .Where(c => c.IsActive)
            .OrderBy(c => c.ClubCode)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDataAsync();
            return Page();
        }

        // Validate ClubCode exists (unless it's FEDR)
        if (NewUser.ClubCode != "FEDR")
        {
            var clubExists = await _context.Clubs.AnyAsync(c => c.ClubCode == NewUser.ClubCode);
            if (!clubExists)
            {
                ModelState.AddModelError("NewUser.ClubCode", $"Club code '{NewUser.ClubCode}' not found. Use 'FEDR' for Federation.");
                await LoadDataAsync();
                return Page();
            }
        }

        // Check if username already exists
        if (await _context.Users.AnyAsync(u => u.Username == NewUser.Username))
        {
            ModelState.AddModelError("NewUser.Username", "Username already exists.");
            await LoadDataAsync();
            return Page();
        }

        try
        {
            await _authService.CreateUserAsync(
                username: NewUser.Username!,
                password: NewUser.Password!,
                clubCode: NewUser.ClubCode!
            );

            TempData["Message"] = $"User '{NewUser.Username}' created successfully!";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Error creating user: {ex.Message}");
            await LoadDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToPage();
        }

        // Prevent deleting yourself
        var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        if (user.Id == currentUserId)
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToPage();
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        TempData["Message"] = $"User '{user.Username}' deleted successfully!";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostImportCsvAsync(IFormFile csvFile)
    {
        if (csvFile == null || csvFile.Length == 0)
        {
            TempData["Error"] = "Please select a CSV file.";
            await LoadDataAsync();
            return Page();
        }

        var imported = 0;
        var errors = new List<string>();

        using (var reader = new StreamReader(csvFile.OpenReadStream()))
        {
            var lineNumber = 0;
            string? line;
            
            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineNumber++;
                
                if (lineNumber == 1) continue; // Skip header

                var parts = line.Split(',');
                if (parts.Length < 3)
                {
                    errors.Add($"Line {lineNumber}: Invalid format. Expected: Username,Password,ClubCode");
                    continue;
                }

                var username = parts[0].Trim();
                var password = parts[1].Trim();
                var clubCode = parts[2].Trim();

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(clubCode))
                {
                    errors.Add($"Line {lineNumber}: Username, Password, and ClubCode are required.");
                    continue;
                }

                // Validate ClubCode
                if (clubCode != "FEDR")
                {
                    var clubExists = await _context.Clubs.AnyAsync(c => c.ClubCode == clubCode);
                    if (!clubExists)
                    {
                        errors.Add($"Line {lineNumber}: Club code '{clubCode}' not found.");
                        continue;
                    }
                }

                // Check if username exists
                if (await _context.Users.AnyAsync(u => u.Username == username))
                {
                    errors.Add($"Line {lineNumber}: Username '{username}' already exists.");
                    continue;
                }

                try
                {
                    await _authService.CreateUserAsync(
                        username: username,
                        password: password,
                        clubCode: clubCode
                    );

                    imported++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Line {lineNumber}: {ex.Message}");
                }
            }
        }

        if (imported > 0)
        {
            TempData["Message"] = $"Successfully imported {imported} user(s).";
        }
        if (errors.Any())
        {
            TempData["Error"] = $"Import completed with {errors.Count} error(s). " + string.Join("; ", errors.Take(5));
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostImportJsonAsync(IFormFile jsonFile)
    {
        if (jsonFile == null || jsonFile.Length == 0)
        {
            TempData["Error"] = "Please select a JSON file.";
            await LoadDataAsync();
            return Page();
        }

        try
        {
            using var stream = jsonFile.OpenReadStream();
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            
            var users = JsonSerializer.Deserialize<List<UserImport>>(json);
            
            if (users == null || !users.Any())
            {
                TempData["Error"] = "No users found in JSON file.";
                return RedirectToPage();
            }

            var imported = 0;
            var errors = new List<string>();

            foreach (var userImport in users)
            {
                try
                {
                    if (string.IsNullOrEmpty(userImport.Username) || string.IsNullOrEmpty(userImport.Password) || string.IsNullOrEmpty(userImport.ClubCode))
                    {
                        errors.Add($"User '{userImport.Username}': Username, Password, and ClubCode are required.");
                        continue;
                    }

                    // Validate ClubCode
                    if (userImport.ClubCode != "FEDR")
                    {
                        var clubExists = await _context.Clubs.AnyAsync(c => c.ClubCode == userImport.ClubCode);
                        if (!clubExists)
                        {
                            errors.Add($"User '{userImport.Username}': Club code '{userImport.ClubCode}' not found.");
                            continue;
                        }
                    }

                    if (await _context.Users.AnyAsync(u => u.Username == userImport.Username))
                    {
                        errors.Add($"User '{userImport.Username}': Username already exists.");
                        continue;
                    }

                    await _authService.CreateUserAsync(
                        username: userImport.Username,
                        password: userImport.Password,
                        clubCode: userImport.ClubCode
                    );

                    imported++;
                }
                catch (Exception ex)
                {
                    errors.Add($"User '{userImport.Username}': {ex.Message}");
                }
            }

            if (imported > 0)
            {
                TempData["Message"] = $"Successfully imported {imported} user(s).";
            }
            if (errors.Any())
            {
                TempData["Error"] = $"Import completed with {errors.Count} error(s). " + string.Join("; ", errors.Take(5));
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error reading JSON file: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetDownloadTemplateAsync(string format)
    {
        if (format == "csv")
        {
            var csv = new StringBuilder();
            csv.AppendLine("Username,Password,ClubCode");
            csv.AppendLine("club1_captain,SecurePass123,CLB1");
            csv.AppendLine("club2_captain,SecurePass123,CLB2");
            csv.AppendLine("federation_user,FedPass123,FEDR");
            
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "users_template.csv");
        }
        else // json
        {
            var template = new
            {
                users = new[]
                {
                    new { Username = "club1_captain", Password = "SecurePass123", ClubCode = "CLB1" },
                    new { Username = "club2_captain", Password = "SecurePass123", ClubCode = "CLB2" },
                    new { Username = "federation_user", Password = "FedPass123", ClubCode = "FEDR" }
                }
            };

            var json = JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });
            return File(Encoding.UTF8.GetBytes(json), "application/json", "users_template.json");
        }
    }

    private async Task LoadDataAsync()
    {
        AllUsers = await _context.Users
            .OrderBy(u => u.ClubCode)
            .ThenBy(u => u.Username)
            .ToListAsync();

        AllClubs = await _context.Clubs
            .Where(c => c.IsActive)
            .OrderBy(c => c.ClubCode)
            .ToListAsync();
    }
}

public class UserInput
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(20, MinimumLength = 3)]
    [Display(Name = "Username")]
    public string? Username { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Club Code is required")]
    [StringLength(4, MinimumLength = 4)]
    [Display(Name = "Club Code (4 characters: FEDR, CLB1-CLB10)")]
    public string? ClubCode { get; set; }
}

public class UserImport
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ClubCode { get; set; } = string.Empty;
}
