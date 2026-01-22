using ContactsRazor.Data;
using ContactsRazor.Helpers;
using ContactsRazor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Text;

namespace ContactsRazor.Pages;

[Authorize(Policy = "FederationOnly")]
public class ResultsManagementModel : PageModel
{
    private readonly ContactsDbContext _context;
    private readonly ILogger<ResultsManagementModel> _logger;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public ResultsManagementModel(
        ContactsDbContext context, 
        ILogger<ResultsManagementModel> logger,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _environment = environment;
    }

    public int TotalResultSets { get; set; }
    public int TotalResultEntries { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!User.IsFederation())
        {
            return RedirectToPage("/AccessDenied");
        }

        // Get statistics
        TotalResultSets = await _context.ResultSets.CountAsync();
        TotalResultEntries = await _context.ResultEntries.CountAsync();

        // Check for messages from TempData
        if (TempData["SuccessMessage"] != null)
        {
            SuccessMessage = TempData["SuccessMessage"]?.ToString();
        }
        if (TempData["ErrorMessage"] != null)
        {
            ErrorMessage = TempData["ErrorMessage"]?.ToString();
        }

        return Page();
    }

    private string GetDatabasePath()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string not found.");
        }

        // Extract the database file path from connection string
        // Format: "Data Source=golf.db" or "Data Source=/path/to/golf.db"
        var dataSource = connectionString
            .Split(';')
            .FirstOrDefault(s => s.TrimStart().StartsWith("Data Source", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrEmpty(dataSource))
        {
            throw new InvalidOperationException("Could not find Data Source in connection string.");
        }

        var dbPath = dataSource.Split('=')[1].Trim();
        
        // If relative path, make it relative to the content root
        if (!Path.IsPathRooted(dbPath))
        {
            dbPath = Path.Combine(_environment.ContentRootPath, dbPath);
        }

        return dbPath;
    }

    public async Task<IActionResult> OnPostBackupDatabaseAsync()
    {
        if (!User.IsFederation())
        {
            return RedirectToPage("/AccessDenied");
        }

        try
        {
            // Ensure database connection is closed before accessing the file
            if (_context.Database.GetDbConnection().State == System.Data.ConnectionState.Open)
            {
                await _context.Database.CloseConnectionAsync();
            }

            var dbPath = GetDatabasePath();

            if (!System.IO.File.Exists(dbPath))
            {
                TempData["ErrorMessage"] = "Database file not found.";
                return RedirectToPage();
            }

            // Read the database file
            var dbBytes = await System.IO.File.ReadAllBytesAsync(dbPath);

            // Create filename with timestamp - using the original database name pattern
            var dbFileName = Path.GetFileName(dbPath);
            var dbNameWithoutExt = Path.GetFileNameWithoutExtension(dbPath);
            var dbExtension = Path.GetExtension(dbPath);
            var filename = $"{dbNameWithoutExt}_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}{dbExtension}";

            // Return file - this will trigger the browser's save dialog
            return File(dbBytes, "application/x-sqlite3", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database backup");
            TempData["ErrorMessage"] = $"Error creating database backup: {ex.Message}";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostRestoreDatabaseAsync(IFormFile? databaseFile)
    {
        if (!User.IsFederation())
        {
            return RedirectToPage("/AccessDenied");
        }

        if (databaseFile == null || databaseFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a database backup file to restore.";
            return RedirectToPage();
        }

        // Validate file extension
        var fileName = databaseFile.FileName.ToLower();
        if (!fileName.EndsWith(".db") && !fileName.EndsWith(".sqlite") && !fileName.EndsWith(".sqlite3"))
        {
            TempData["ErrorMessage"] = "Invalid file type. Please select a .db, .sqlite, or .sqlite3 file.";
            return RedirectToPage();
        }

        try
        {
            // Ensure database connection is closed before accessing the file
            if (_context.Database.GetDbConnection().State == System.Data.ConnectionState.Open)
            {
                await _context.Database.CloseConnectionAsync();
            }

            var dbPath = GetDatabasePath();
            var backupPath = $"{dbPath}.backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";

            // Create backup of current database before restoring
            if (System.IO.File.Exists(dbPath))
            {
                System.IO.File.Copy(dbPath, backupPath, overwrite: true);
                _logger.LogInformation($"Created backup of existing database at {backupPath}");
            }

            // Read the uploaded database file
            using var stream = new MemoryStream();
            await databaseFile.CopyToAsync(stream);
            var dbBytes = stream.ToArray();

            // Write the new database file
            await System.IO.File.WriteAllBytesAsync(dbPath, dbBytes);

            // Verify the database can be opened
            try
            {
                await _context.Database.OpenConnectionAsync();
                await _context.Database.CloseConnectionAsync();
            }
            catch (Exception ex)
            {
                // Restore the backup if the new database is invalid
                if (System.IO.File.Exists(backupPath))
                {
                    System.IO.File.Copy(backupPath, dbPath, overwrite: true);
                    _logger.LogWarning($"Restored previous database due to validation error: {ex.Message}");
                }
                throw new InvalidOperationException($"Invalid database file. The previous database has been restored. Error: {ex.Message}");
            }

            TempData["SuccessMessage"] = "Database successfully restored from backup file.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database restore");
            TempData["ErrorMessage"] = $"Error restoring database: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClearResultsAsync()
    {
        if (!User.IsFederation())
        {
            return RedirectToPage("/AccessDenied");
        }

        try
        {
            // Delete all result entries first (due to foreign key constraints)
            var entriesCount = await _context.ResultEntries.CountAsync();
            _context.ResultEntries.RemoveRange(_context.ResultEntries);

            // Delete all result sets
            var setsCount = await _context.ResultSets.CountAsync();
            _context.ResultSets.RemoveRange(_context.ResultSets);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Successfully cleared {setsCount} result set(s) and {entriesCount} result entry(ies).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing results");
            TempData["ErrorMessage"] = $"Error clearing results: {ex.Message}";
        }

        return RedirectToPage();
    }
}
