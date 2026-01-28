using ContactsRazor.Data;
using ContactsRazor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.RegularExpressions;

namespace ContactsRazor.Services;

public class PlayerLoaderService
{
    private readonly ContactsDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public PlayerLoaderService(
        ContactsDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    /// <summary>
    /// Load players from a text file
    /// Format: Code : value; Name : value; Index: value
    /// </summary>
    public async Task<PlayerLoadResult> LoadPlayersFromFileAsync(string fileName, string clubCode)
    {
        var result = new PlayerLoadResult();
        var rootPath = _environment.ContentRootPath;
        var filePath = Path.Combine(rootPath, fileName);

        if (!File.Exists(filePath))
        {
            result.Errors.Add($"File '{fileName}' not found in root directory: {rootPath}");
            return result;
        }

        // Validate club exists
        var club = await _context.Clubs.FirstOrDefaultAsync(c => c.ClubCode == clubCode);
        if (club == null)
        {
            result.Errors.Add($"Club with code '{clubCode}' does not exist.");
            return result;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            var batchCodes = new HashSet<string>(); // Track codes in this batch
            var batchNames = new HashSet<string>(); // Track names in this batch

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;

                // Parse format: Code : value; Name : value; Index: value
                // Use regex to handle case-insensitive field names and flexible spacing
                var code = ExtractValue(trimmedLine, "Code");
                var name = ExtractValue(trimmedLine, "Name");
                var indexStr = ExtractValue(trimmedLine, "Index");

                // Validate Code (required, exactly 6 digits)
                if (string.IsNullOrWhiteSpace(code))
                {
                    result.Errors.Add($"Missing Code in line: {trimmedLine}");
                    continue;
                }

                code = code.Trim();
                if (code.Length != 6 || !code.All(char.IsDigit))
                {
                    result.Errors.Add($"Code '{code}' in line '{trimmedLine}' must be exactly 6 digits.");
                    continue;
                }

                // Validate Name (required)
                if (string.IsNullOrWhiteSpace(name))
                {
                    result.Errors.Add($"Missing Name in line: {trimmedLine}");
                    continue;
                }

                name = name.Trim();
                if (name.Length > 30)
                {
                    name = name.Substring(0, 30);
                    result.Warnings.Add($"Name for Code '{code}' truncated to 30 characters.");
                }

                // Validate Index (required, must be valid decimal)
                if (string.IsNullOrWhiteSpace(indexStr))
                {
                    result.Errors.Add($"Missing Index in line: {trimmedLine}");
                    continue;
                }

                if (!decimal.TryParse(indexStr.Trim(), out var index))
                {
                    result.Errors.Add($"Invalid Index '{indexStr}' in line: {trimmedLine}");
                    continue;
                }

                // Check for duplicates within the same batch first
                if (batchCodes.Contains(code))
                {
                    result.Errors.Add($"Duplicate Code '{code}' found within the same file. Line: {trimmedLine}");
                    continue;
                }

                if (batchNames.Contains(name))
                {
                    result.Errors.Add($"Duplicate Name '{name}' found within the same file. Line: {trimmedLine}");
                    continue;
                }

                // Check for duplicates in database (Code and Name must be unique globally)
                var existingByCode = await _context.Players.FirstOrDefaultAsync(p => p.Code == code);
                if (existingByCode != null)
                {
                    result.Errors.Add($"Code '{code}' already exists in database for player '{existingByCode.Name}' (Club: {existingByCode.ClubCode}). Duplicate in line: {trimmedLine}");
                    continue;
                }

                var existingByName = await _context.Players.FirstOrDefaultAsync(p => p.Name == name);
                if (existingByName != null)
                {
                    result.Errors.Add($"Name '{name}' already exists in database for player with Code '{existingByName.Code}' (Club: {existingByName.ClubCode}). Duplicate in line: {trimmedLine}");
                    continue;
                }

                // Add to batch tracking
                batchCodes.Add(code);
                batchNames.Add(name);

                // Create player
                var player = new Player
                {
                    Code = code,
                    Name = name,
                    Index = index,
                    ClubCode = clubCode
                };

                _context.Players.Add(player);
                result.PlayersAdded++;
            }

            if (result.PlayersAdded > 0)
            {
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException dbEx)
                {
                    // Get more detailed error information
                    var errorMessage = $"Database error while saving players: {dbEx.Message}";
                    if (dbEx.InnerException != null)
                    {
                        errorMessage += $" Inner exception: {dbEx.InnerException.Message}";
                    }
                    
                    // Try to identify which player caused the issue
                    var entries = dbEx.Entries?.ToList();
                    if (entries != null && entries.Any())
                    {
                        foreach (var entry in entries)
                        {
                            if (entry.Entity is Player failedPlayer)
                            {
                                errorMessage += $" Failed player - Code: {failedPlayer.Code}, Name: {failedPlayer.Name}";
                            }
                        }
                    }
                    
                    result.Errors.Add(errorMessage);
                    result.Success = false;
                    // Rollback the changes
                    _context.ChangeTracker.Clear();
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Error saving players: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        result.Errors.Add($"Inner exception: {ex.InnerException.Message}");
                    }
                    result.Success = false;
                    // Rollback the changes
                    _context.ChangeTracker.Clear();
                }
            }

            // Update club's NumberOfPlayers
            var playerCount = await _context.Players.CountAsync(p => p.ClubCode == clubCode);
            club.NumberOfPlayers = playerCount;
            await _context.SaveChangesAsync();

            result.Success = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error reading file '{fileName}': {ex.Message}");
            result.Success = false;
        }

        return result;
    }

    /// <summary>
    /// Extract value from line based on field name (case-insensitive)
    /// Format: FieldName : value; or FieldName: value;
    /// Handles variations: Code, CODE, Name, NAME, NAme, Index, INDEX, etc.
    /// </summary>
    private string? ExtractValue(string line, string fieldName)
    {
        // Use regex for case-insensitive matching
        // Pattern: fieldname followed by optional space, colon, optional space, then value until semicolon or end
        var pattern = $@"{fieldName}\s*:\s*([^;]+)";
        var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
        
        if (match.Success && match.Groups.Count > 1)
        {
            var value = match.Groups[1].Value.Trim();
            return value;
        }

        return null;
    }

    /// <summary>
    /// Get list of available player text files in root directory
    /// </summary>
    public List<string> GetAvailablePlayerFiles()
    {
        var rootPath = _environment.ContentRootPath;
        var files = Directory.GetFiles(rootPath, "TeamList*.txt")
            .Select(f => Path.GetFileName(f))
            .OrderBy(f => f)
            .ToList();

        return files;
    }
}

public class PlayerLoadResult
{
    public bool Success { get; set; }
    public int PlayersAdded { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
