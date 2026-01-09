using ContactsRazor.Data;
using ContactsRazor.Models;
using Microsoft.EntityFrameworkCore;

namespace ContactsRazor.Services;

public class FileLoaderService
{
    private readonly ContactsDbContext _context;
    private readonly AuthService _authService;
    private readonly string _rootPath;

    public FileLoaderService(ContactsDbContext context, AuthService authService, IWebHostEnvironment environment)
    {
        _context = context;
        _authService = authService;
        _rootPath = environment.ContentRootPath;
    }

    /// <summary>
    /// Load clubs from clubs.txt file
    /// Format: CLUBCODE|LONGNAME (one per line)
    /// </summary>
    public async Task<FileLoadResult> LoadClubsAsync()
    {
        var result = new FileLoadResult();
        var clubsFilePath = Path.Combine(_rootPath, "clubs.txt");

        if (!File.Exists(clubsFilePath))
        {
            result.Errors.Add($"File 'clubs.txt' not found in root directory.");
            return result;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(clubsFilePath);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;

                var parts = trimmedLine.Split('|');
                if (parts.Length < 2)
                {
                    result.Errors.Add($"Invalid format in line: {trimmedLine}. Expected: CLUBCODE|LONGNAME");
                    continue;
                }

                var clubCode = parts[0].Trim().ToUpper();
                var longName = parts[1].Trim();

                // Validate club code length
                if (clubCode.Length != 6)
                {
                    result.Errors.Add($"Club code '{clubCode}' must be exactly 6 characters.");
                    continue;
                }

                // Validate long name length
                if (longName.Length > 30)
                {
                    longName = longName.Substring(0, 30);
                    result.Warnings.Add($"Long name for '{clubCode}' truncated to 30 characters.");
                }

                // Check if club already exists
                var existingClub = await _context.Clubs.FirstOrDefaultAsync(c => c.ClubCode == clubCode);
                
                if (existingClub != null)
                {
                    // Update existing club (keep HmId and NumberOfPlayers if they exist)
                    existingClub.LongName = longName;
                    result.Updated++;
                }
                else
                {
                    // Create new club
                    var club = new Club
                    {
                        ClubCode = clubCode,
                        LongName = longName,
                        HmId = null,
                        NumberOfPlayers = null
                    };
                    _context.Clubs.Add(club);
                    result.Added++;
                }
            }

            await _context.SaveChangesAsync();
            result.Success = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error reading clubs.txt: {ex.Message}");
            result.Success = false;
        }

        return result;
    }

    /// <summary>
    /// Load users from users.txt file
    /// Format: USERNAME|PASSWORD|CLUBCODE (one per line)
    /// </summary>
    public async Task<FileLoadResult> LoadUsersAsync()
    {
        var result = new FileLoadResult();
        var usersFilePath = Path.Combine(_rootPath, "users.txt");

        if (!File.Exists(usersFilePath))
        {
            result.Errors.Add($"File 'users.txt' not found in root directory.");
            return result;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(usersFilePath);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;

                var parts = trimmedLine.Split('|');
                if (parts.Length < 3)
                {
                    result.Errors.Add($"Invalid format in line: {trimmedLine}. Expected: USERNAME|PASSWORD|CLUBCODE");
                    continue;
                }

                var username = parts[0].Trim();
                var password = parts[1].Trim();
                var clubCode = parts[2].Trim().ToUpper();

                // Validate inputs
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(clubCode))
                {
                    result.Errors.Add($"Missing required fields in line: {trimmedLine}");
                    continue;
                }

                if (clubCode.Length != 6)
                {
                    result.Errors.Add($"Club code '{clubCode}' for user '{username}' must be exactly 6 characters.");
                    continue;
                }

                // Validate club exists (unless it's Federation)
                if (clubCode != "FEDERE")
                {
                    var clubExists = await _context.Clubs.AnyAsync(c => c.ClubCode == clubCode);
                    if (!clubExists)
                    {
                        result.Errors.Add($"Club '{clubCode}' not found for user '{username}'. Load clubs first.");
                        continue;
                    }
                }

                // Check if user already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                
                if (existingUser != null)
                {
                    // Update existing user (new password and club code)
                    existingUser.PasswordHash = AuthService.HashPassword(password);
                    existingUser.ClubCode = clubCode;
                    result.Updated++;
                }
                else
                {
                    // Create new user
                    try
                    {
                        await _authService.CreateUserAsync(username, password, clubCode);
                        result.Added++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Error creating user '{username}': {ex.Message}");
                    }
                }
            }

            await _context.SaveChangesAsync();
            result.Success = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error reading users.txt: {ex.Message}");
            result.Success = false;
        }

        return result;
    }
}

public class FileLoadResult
{
    public bool Success { get; set; }
    public int Added { get; set; }
    public int Updated { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
