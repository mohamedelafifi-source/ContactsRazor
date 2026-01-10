using ContactsRazor.Data;
using ContactsRazor.Models;
using Microsoft.EntityFrameworkCore;

namespace ContactsRazor.Services;

public class BasicDataLoaderService
{
    private readonly ContactsDbContext _context;
    private readonly AuthService _authService;
    private readonly IWebHostEnvironment _environment;

    public BasicDataLoaderService(
        ContactsDbContext context, 
        AuthService authService,
        IWebHostEnvironment environment)
    {
        _context = context;
        _authService = authService;
        _environment = environment;
    }

    /// <summary>
    /// Load all data from BasicData.txt file
    /// Format: USERNAME|PASSWORD|SHORT_CLUB_NAME(6 chars)|LONG_CLUB_NAME
    /// </summary>
    public async Task<LoadResult> LoadBasicDataAsync()
    {
        var result = new LoadResult();
        var rootPath = _environment.ContentRootPath;
        var filePath = Path.Combine(rootPath, "BasicData.txt");

        if (!File.Exists(filePath))
        {
            result.Errors.Add($"File 'BasicData.txt' not found in root directory: {rootPath}");
            return result;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            var clubsToProcess = new Dictionary<string, string>(); // ClubCode -> LongName
            var usersToProcess = new List<(string Username, string Password, string ClubCode)>();

            // First pass: Parse all data
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;

                var parts = trimmedLine.Split('|');
                if (parts.Length < 4)
                {
                    result.Errors.Add($"Invalid format in line: {trimmedLine}. Expected: USERNAME|PASSWORD|CLUBCODE|LONGNAME");
                    continue;
                }

                var username = parts[0].Trim();
                var password = parts[1].Trim();
                var clubCode = parts[2].Trim().ToUpper();
                var longName = parts[3].Trim();

                // Validate inputs
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || 
                    string.IsNullOrWhiteSpace(clubCode) || string.IsNullOrWhiteSpace(longName))
                {
                    result.Errors.Add($"Missing required fields in line: {trimmedLine}");
                    continue;
                }

                // Validate club code length
                if (clubCode.Length != 6)
                {
                    result.Errors.Add($"Club code '{clubCode}' for user '{username}' must be exactly 6 characters.");
                    continue;
                }

                // Validate long name length
                if (longName.Length > 30)
                {
                    longName = longName.Substring(0, 30);
                    result.Warnings.Add($"Long name for '{clubCode}' truncated to 30 characters.");
                }

                // Store club info (will be unique per ClubCode)
                if (!clubsToProcess.ContainsKey(clubCode))
                {
                    clubsToProcess[clubCode] = longName;
                }

                // Store user info
                usersToProcess.Add((username, password, clubCode));
            }

            // Second pass: Process all clubs
            foreach (var kvp in clubsToProcess)
            {
                var clubCode = kvp.Key;
                var longName = kvp.Value;

                var existingClub = await _context.Clubs.FirstOrDefaultAsync(c => c.ClubCode == clubCode);
                
                if (existingClub != null)
                {
                    // Update existing club
                    existingClub.LongName = longName;
                    result.ClubsUpdated++;
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
                    result.ClubsAdded++;
                }
            }

            // Save clubs first
            if (result.ClubsAdded > 0 || result.ClubsUpdated > 0)
            {
                await _context.SaveChangesAsync();
            }

            // Third pass: Process all users
            foreach (var (username, password, clubCode) in usersToProcess)
            {
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                
                if (existingUser != null)
                {
                    // Update existing user (password and club code)
                    existingUser.PasswordHash = AuthService.HashPassword(password);
                    existingUser.ClubCode = clubCode;
                    result.UsersUpdated++;
                }
                else
                {
                    // Create new user
                    try
                    {
                        await _authService.CreateUserAsync(username, password, clubCode);
                        result.UsersAdded++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Error creating user '{username}': {ex.Message}");
                    }
                }
            }

            // Save users
            if (result.UsersAdded > 0 || result.UsersUpdated > 0)
            {
                await _context.SaveChangesAsync();
            }

            result.Success = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error reading BasicData.txt: {ex.Message}");
            result.Success = false;
        }

        return result;
    }
}

public class LoadResult
{
    public bool Success { get; set; }
    public int ClubsAdded { get; set; }
    public int ClubsUpdated { get; set; }
    public int UsersAdded { get; set; }
    public int UsersUpdated { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
