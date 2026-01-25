using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ContactsRazor.Data;
using ContactsRazor.Models;
using ContactsRazor.Helpers;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace ContactsRazor.Pages;

[Authorize]
public class ResultsModel : PageModel
{
    private readonly ContactsDbContext _context;

    public ResultsModel(ContactsDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ResultSetInput ResultSetInput { get; set; } = new();

    [BindProperty]
    public ResultEntryInput ResultEntryInput { get; set; } = new();

    public Club? CurrentClub { get; set; }
    public ResultSet? CurrentResultSet { get; set; }
    public ResultEntry? CurrentResultEntry { get; set; }
    public int? PreviousResultEntryId { get; set; }
    public int? NextResultEntryId { get; set; }
    public int CurrentResultEntryIndex { get; set; }
    public int TotalResultEntries { get; set; }
    public int MaxResultEntries { get; set; } = 8;
    
    public List<Club> Clubs { get; set; } = new();
    public List<SelectListItem> Players { get; set; } = new();
    public List<ResultSetSummary> ExistingResultSets { get; set; } = new();

    public string Mode { get; set; } = "new"; // "new" or "existing"

    public async Task<IActionResult> OnGetAsync(string? mode = null, int? resultSetId = null, int? entryId = null, bool @new = false)
    {
        var clubCode = User.GetClubCode();
        if (string.IsNullOrEmpty(clubCode) || clubCode == "FEDERE")
        {
            TempData["Error"] = "Invalid access. Only Club Captains can enter results.";
            return RedirectToPage("/Dashboard");
        }

        // Get current club
        CurrentClub = await _context.Clubs.FirstOrDefaultAsync(c => c.ClubCode == clubCode);
        if (CurrentClub == null)
        {
            TempData["Error"] = "Club not found.";
            return RedirectToPage("/Dashboard");
        }

        // Set mode
        Mode = mode ?? "new";

        // Load clubs for venue selection
        Clubs = await _context.Clubs
            .Where(c => c.ClubCode != "FEDERE")
            .OrderBy(c => c.LongName)
            .ToListAsync();

        // Load players for the current club
        var clubPlayers = await _context.Players
            .Where(p => p.ClubCode == clubCode)
            .OrderBy(p => p.Name)
            .ToListAsync();
        Players = clubPlayers.Select(p => new SelectListItem
        {
            Value = p.Id.ToString(),
            Text = p.Name
        }).ToList();

        // Load existing result sets for selection
        var resultSets = await _context.ResultSets
            .Where(rs => rs.ClubCode == clubCode)
            .OrderByDescending(rs => rs.Date)
            .ThenBy(rs => rs.VenueClubCode)
            .Include(rs => rs.VenueClub)
            .ToListAsync();
        ExistingResultSets = resultSets.Select(rs => new ResultSetSummary
        {
            Id = rs.Id,
            DisplayText = $"{rs.VenueClub?.LongName ?? rs.VenueClubCode} - {rs.Date:yyyy-MM-dd}"
        }).ToList();

        // Handle "existing" mode - need to select a result set first
        if (Mode == "existing" && !resultSetId.HasValue)
        {
            // Show selection list - don't load result entries yet
            return Page();
        }

        // Handle result set selection
        if (resultSetId.HasValue && resultSetId.Value > 0)
        {
            CurrentResultSet = await _context.ResultSets
                .Include(rs => rs.VenueClub)
                .FirstOrDefaultAsync(rs => rs.Id == resultSetId.Value && rs.ClubCode == clubCode);

            if (CurrentResultSet == null)
            {
                TempData["Error"] = "Result set not found.";
                return RedirectToPage("/Results", new { mode = "existing" });
            }

            // Set mode to existing when a result set is selected
            Mode = "existing";

            // Populate ResultSetInput
            ResultSetInput.ResultSetId = CurrentResultSet.Id;
            ResultSetInput.Date = CurrentResultSet.Date;
            ResultSetInput.VenueClubCode = CurrentResultSet.VenueClubCode;
        }

        // Handle "new" mode or after result set is selected
        if (CurrentResultSet != null || Mode == "new")
        {
            // Get result entries for current result set
            List<ResultEntry> resultEntries = new();
            if (CurrentResultSet != null)
            {
                resultEntries = await _context.ResultEntries
                    .Where(re => re.ResultSetId == CurrentResultSet.Id)
                    .Include(re => re.Player)
                    .OrderBy(re => re.Id)
                    .ToListAsync();
            }

            TotalResultEntries = resultEntries.Count;

            // Handle "new entry" request
            if (@new)
            {
                // Check max limit
                if (TotalResultEntries >= MaxResultEntries)
                {
                    TempData["Error"] = $"Maximum {MaxResultEntries} results allowed per result set.";
                    if (CurrentResultSet != null)
                    {
                        return RedirectToPage("/Results", new { mode = "existing", resultSetId = CurrentResultSet.Id });
                    }
                    return Page();
                }

                // Show empty form for new entry
                CurrentResultEntryIndex = 0;
                CurrentResultEntry = null;
                ResultEntryInput.ResultEntryId = 0;
                ResultEntryInput.PlayerId = 0;
                ResultEntryInput.HCP = 0;
                ResultEntryInput.Result = 0;
                return Page();
            }

            // Get current result entry
            // Handle special case: entryId = -1 means "new entry after last"
            if (entryId.HasValue && entryId.Value == -1)
            {
                // Show empty form for new entry after last
                if (resultEntries.Count < MaxResultEntries)
                {
                    CurrentResultEntryIndex = 0;
                    CurrentResultEntry = null;
                    ResultEntryInput.ResultEntryId = 0;
                    ResultEntryInput.PlayerId = 0;
                    ResultEntryInput.HCP = 0;
                    ResultEntryInput.Result = 0;
                    // Set Previous to last entry
                    if (resultEntries.Any())
                    {
                        PreviousResultEntryId = resultEntries.Last().Id;
                    }
                    return Page();
                }
                else
                {
                    TempData["Error"] = $"Maximum {MaxResultEntries} results allowed per result set.";
                    return RedirectToPage("/Results", new { mode = "existing", resultSetId = CurrentResultSet.Id });
                }
            }
            else if (entryId.HasValue && entryId.Value > 0)
            {
                CurrentResultEntry = resultEntries.FirstOrDefault(re => re.Id == entryId.Value);
            }
            else if (resultEntries.Any())
            {
                CurrentResultEntry = resultEntries.First();
            }

            // Set up Next/Previous navigation and populate form
            if (CurrentResultEntry != null && resultEntries.Any())
            {
                var currentIndex = resultEntries.IndexOf(CurrentResultEntry);
                CurrentResultEntryIndex = currentIndex + 1;

                // Previous entry
                if (currentIndex > 0)
                {
                    PreviousResultEntryId = resultEntries[currentIndex - 1].Id;
                }

                // Next entry - allow going one step beyond last entry to create new entry
                if (currentIndex < resultEntries.Count - 1)
                {
                    NextResultEntryId = resultEntries[currentIndex + 1].Id;
                }
                else if (currentIndex == resultEntries.Count - 1 && resultEntries.Count < MaxResultEntries)
                {
                    // Allow Next to go one step beyond to create new entry
                    NextResultEntryId = -1; // Special marker for "new entry after last"
                }

                // Populate form with current entry data
                ResultEntryInput.ResultEntryId = CurrentResultEntry.Id;
                ResultEntryInput.PlayerId = CurrentResultEntry.PlayerId;
                ResultEntryInput.HCP = CurrentResultEntry.HCP;
                ResultEntryInput.Result = CurrentResultEntry.Result;
            }
            else if (CurrentResultSet != null)
            {
                // Result set exists but no entries yet - show empty form
                ResultSetInput.ResultSetId = CurrentResultSet.Id;
                CurrentResultEntryIndex = 0;
                CurrentResultEntry = null;
                ResultEntryInput.ResultEntryId = 0;
                ResultEntryInput.PlayerId = 0;
                ResultEntryInput.HCP = 0;
                ResultEntryInput.Result = 0;
                // Allow Next to create new entry if under max
                if (resultEntries.Count < MaxResultEntries)
                {
                    NextResultEntryId = -1; // Special marker for "new entry"
                }
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostNewResultSetAsync()
    {
        var clubCode = User.GetClubCode();
        if (string.IsNullOrEmpty(clubCode) || clubCode == "FEDERE")
        {
            TempData["Error"] = "Invalid access.";
            return RedirectToPage("/Dashboard");
        }

        if (!ModelState.IsValid)
        {
            return await OnGetAsync("new");
        }

        // Validate date is not in the future (optional - adjust as needed)
        if (ResultSetInput.Date > DateTime.Today)
        {
            ModelState.AddModelError(nameof(ResultSetInput.Date), "Date cannot be in the future.");
            return await OnGetAsync("new");
        }

        // Check if result set already exists for this date and venue
        var existingResultSet = await _context.ResultSets
            .FirstOrDefaultAsync(rs => rs.ClubCode == clubCode && 
                                      rs.VenueClubCode == ResultSetInput.VenueClubCode && 
                                      rs.Date == ResultSetInput.Date);

        if (existingResultSet != null)
        {
            // Use existing result set
            TempData["Message"] = "Result set already exists. You can now add result entries.";
            return RedirectToPage("/Results", new { mode = "existing", resultSetId = existingResultSet.Id });
        }

        // Create new result set
        var resultSet = new ResultSet
        {
            ClubCode = clubCode,
            VenueClubCode = ResultSetInput.VenueClubCode,
            Date = ResultSetInput.Date,
            CreatedAt = DateTime.UtcNow
        };

        _context.ResultSets.Add(resultSet);
        await _context.SaveChangesAsync();

        TempData["Message"] = "Result set created. You can now add result entries.";
        return RedirectToPage("/Results", new { mode = "existing", resultSetId = resultSet.Id });
    }

    public async Task<IActionResult> OnPostAddUpdateEntryAsync()
    {
        var clubCode = User.GetClubCode();
        if (string.IsNullOrEmpty(clubCode) || clubCode == "FEDERE")
        {
            TempData["Error"] = "Invalid access.";
            return RedirectToPage("/Dashboard");
        }

        // Remove validation errors for ResultSetInput.Date and VenueClubCode
        // since we only need ResultSetId for adding/updating entries
        ModelState.Remove(nameof(ResultSetInput) + "." + nameof(ResultSetInput.Date));
        ModelState.Remove(nameof(ResultSetInput) + "." + nameof(ResultSetInput.VenueClubCode));

        if (!ModelState.IsValid)
        {
            if (ResultSetInput.ResultSetId > 0)
            {
                return await OnGetAsync("existing", ResultSetInput.ResultSetId, ResultEntryInput.ResultEntryId > 0 ? ResultEntryInput.ResultEntryId : null);
            }
            return await OnGetAsync("new");
        }

        // Validate result set exists and belongs to user's club
        if (ResultSetInput.ResultSetId <= 0)
        {
            ModelState.AddModelError("", "Result set ID is required.");
            return await OnGetAsync("existing");
        }

        var resultSet = await _context.ResultSets
            .FirstOrDefaultAsync(rs => rs.Id == ResultSetInput.ResultSetId && rs.ClubCode == clubCode);
        
        if (resultSet == null)
        {
            TempData["Error"] = "Result set not found.";
            return RedirectToPage("/Results", new { mode = "existing" });
        }

        // Validate max entries
        var currentEntryCount = await _context.ResultEntries.CountAsync(re => re.ResultSetId == resultSet.Id);
        if (ResultEntryInput.ResultEntryId == 0 && currentEntryCount >= MaxResultEntries)
        {
            ModelState.AddModelError("", $"Maximum {MaxResultEntries} results allowed per result set.");
            return await OnGetAsync("existing", resultSet.Id);
        }

        // Validate player belongs to user's club
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.Id == ResultEntryInput.PlayerId && p.ClubCode == clubCode);
        
        if (player == null)
        {
            ModelState.AddModelError(nameof(ResultEntryInput.PlayerId), "Player not found in your club.");
            return await OnGetAsync("existing", resultSet.Id, ResultEntryInput.ResultEntryId > 0 ? ResultEntryInput.ResultEntryId : null);
        }

        // Validate result range
        if (ResultEntryInput.Result < 0 || ResultEntryInput.Result > 50)
        {
            ModelState.AddModelError(nameof(ResultEntryInput.Result), "Result must be between 0 and 50.");
            return await OnGetAsync("existing", resultSet.Id, ResultEntryInput.ResultEntryId > 0 ? ResultEntryInput.ResultEntryId : null);
        }

        if (ResultEntryInput.ResultEntryId == 0)
        {
            // Add new entry
            // Check for duplicate player in same result set
            var existingEntry = await _context.ResultEntries
                .FirstOrDefaultAsync(re => re.ResultSetId == resultSet.Id && re.PlayerId == ResultEntryInput.PlayerId);
            
            if (existingEntry != null)
            {
                ModelState.AddModelError(nameof(ResultEntryInput.PlayerId), "This player already has a result in this result set.");
                return await OnGetAsync("existing", resultSet.Id);
            }

            var resultEntry = new ResultEntry
            {
                ResultSetId = resultSet.Id,
                PlayerId = ResultEntryInput.PlayerId,
                HCP = ResultEntryInput.HCP,
                Result = ResultEntryInput.Result
            };

            _context.ResultEntries.Add(resultEntry);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Result entry added successfully!";
            return RedirectToPage("/Results", new { mode = "existing", resultSetId = resultSet.Id, entryId = resultEntry.Id });
        }
        else
        {
            // Update existing entry
            var resultEntry = await _context.ResultEntries
                .FirstOrDefaultAsync(re => re.Id == ResultEntryInput.ResultEntryId && re.ResultSetId == resultSet.Id);
            
            if (resultEntry == null)
            {
                TempData["Error"] = "Result entry not found.";
                return RedirectToPage("/Results", new { mode = "existing", resultSetId = resultSet.Id });
            }

            // Check for duplicate player (if player changed)
            if (resultEntry.PlayerId != ResultEntryInput.PlayerId)
            {
                var existingEntry = await _context.ResultEntries
                    .FirstOrDefaultAsync(re => re.ResultSetId == resultSet.Id && 
                                             re.PlayerId == ResultEntryInput.PlayerId && 
                                             re.Id != ResultEntryInput.ResultEntryId);
                
                if (existingEntry != null)
                {
                    ModelState.AddModelError(nameof(ResultEntryInput.PlayerId), "This player already has a result in this result set.");
                    return await OnGetAsync("existing", resultSet.Id, ResultEntryInput.ResultEntryId);
                }
            }

            resultEntry.PlayerId = ResultEntryInput.PlayerId;
            resultEntry.HCP = ResultEntryInput.HCP;
            resultEntry.Result = ResultEntryInput.Result;

            await _context.SaveChangesAsync();

            TempData["Message"] = "Result entry updated successfully!";
            return RedirectToPage("/Results", new { mode = "existing", resultSetId = resultSet.Id, entryId = resultEntry.Id });
        }
    }

    public async Task<IActionResult> OnPostImportTextAsync(int resultSetId, IFormFile? importFile)
    {
        var clubCode = User.GetClubCode();
        if (string.IsNullOrEmpty(clubCode) || clubCode == "FEDERE")
        {
            TempData["Error"] = "Invalid access.";
            return RedirectToPage("/Dashboard");
        }

        if (importFile == null || importFile.Length == 0)
        {
            TempData["Error"] = "Please select a .txt file to import.";
            return RedirectToPage("/Results", new { mode = "existing", resultSetId });
        }

        // Verify result set belongs to user's club
        var resultSet = await _context.ResultSets
            .FirstOrDefaultAsync(rs => rs.Id == resultSetId && rs.ClubCode == clubCode);

        if (resultSet == null)
        {
            TempData["Error"] = "Result set not found.";
            return RedirectToPage("/Results", new { mode = "existing" });
        }

        // Load existing entries to check duplicates and current count
        var existingEntries = await _context.ResultEntries
            .Where(re => re.ResultSetId == resultSet.Id)
            .ToListAsync();

        var currentCount = existingEntries.Count;
        int addedCount = 0;
        int skippedNameNotFound = 0;
        int skippedInvalidFormat = 0;
        int skippedInvalidResult = 0;
        int skippedDuplicate = 0;
        int skippedMaxReached = 0;

        using (var reader = new StreamReader(importFile.OpenReadStream()))
        {
            string? line;
            int lineNumber = 0;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineNumber++;
                line = line.Trim();

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                // Expected format: Player Name | Result
                var parts = line.Split('|');
                if (parts.Length < 2)
                {
                    skippedInvalidFormat++;
                    continue;
                }

                var playerName = parts[0].Trim();
                var resultText = parts[1].Trim();

                if (string.IsNullOrEmpty(playerName))
                {
                    skippedInvalidFormat++;
                    continue;
                }

                if (!int.TryParse(resultText, out var resultValue))
                {
                    skippedInvalidResult++;
                    continue;
                }

                if (resultValue < 0 || resultValue > 50)
                {
                    skippedInvalidResult++;
                    continue;
                }

                if (currentCount + addedCount >= MaxResultEntries)
                {
                    skippedMaxReached++;
                    break; // No more entries can be added
                }

                // Find player in this club by exact name (names are unique globally)
                var player = await _context.Players
                    .FirstOrDefaultAsync(p => p.ClubCode == clubCode && p.Name == playerName);

                if (player == null)
                {
                    skippedNameNotFound++;
                    continue;
                }

                // Check for duplicate player in this result set
                if (existingEntries.Any(e => e.PlayerId == player.Id) ||
                    await _context.ResultEntries.AnyAsync(re => re.ResultSetId == resultSet.Id && re.PlayerId == player.Id))
                {
                    skippedDuplicate++;
                    continue;
                }

                var entry = new ResultEntry
                {
                    ResultSetId = resultSet.Id,
                    PlayerId = player.Id,
                    HCP = player.Index, // Use player's current index as HCP
                    Result = resultValue
                };

                _context.ResultEntries.Add(entry);
                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            await _context.SaveChangesAsync();
        }

        // Build feedback message
        var messages = new List<string>();
        if (addedCount > 0)
        {
            messages.Add($"{addedCount} result(s) imported successfully.");
        }
        if (skippedNameNotFound > 0)
        {
            messages.Add($"{skippedNameNotFound} line(s) skipped because player name was not found in your club.");
        }
        if (skippedInvalidFormat > 0)
        {
            messages.Add($"{skippedInvalidFormat} line(s) skipped due to invalid format (expected: Name | Result).");
        }
        if (skippedInvalidResult > 0)
        {
            messages.Add($"{skippedInvalidResult} line(s) skipped due to invalid result (must be an integer between 0 and 50).");
        }
        if (skippedDuplicate > 0)
        {
            messages.Add($"{skippedDuplicate} line(s) skipped because the player already has a result in this set.");
        }
        if (skippedMaxReached > 0)
        {
            messages.Add("Maximum number of results for this set has been reached; remaining lines were not imported.");
        }

        if (messages.Any())
        {
            TempData["Message"] = string.Join(" ", messages);
        }
        else
        {
            TempData["Message"] = "No results were imported from the file.";
        }

        return RedirectToPage("/Results", new { mode = "existing", resultSetId = resultSet.Id });
    }

    public async Task<IActionResult> OnPostDeleteEntryAsync(int id, int resultSetId)
    {
        var clubCode = User.GetClubCode();
        if (string.IsNullOrEmpty(clubCode) || clubCode == "FEDERE")
        {
            TempData["Error"] = "Invalid access.";
            return RedirectToPage("/Dashboard");
        }

        // Verify result set belongs to user's club
        var resultSet = await _context.ResultSets
            .FirstOrDefaultAsync(rs => rs.Id == resultSetId && rs.ClubCode == clubCode);
        
        if (resultSet == null)
        {
            TempData["Error"] = "Result set not found.";
            return RedirectToPage("/Results", new { mode = "existing" });
        }

        var resultEntry = await _context.ResultEntries
            .FirstOrDefaultAsync(re => re.Id == id && re.ResultSetId == resultSetId);
        
        if (resultEntry == null)
        {
            TempData["Error"] = "Result entry not found.";
            return RedirectToPage("/Results", new { mode = "existing", resultSetId = resultSetId });
        }

        _context.ResultEntries.Remove(resultEntry);
        await _context.SaveChangesAsync();

        TempData["Message"] = "Result entry deleted successfully!";
        return RedirectToPage("/Results", new { mode = "existing", resultSetId = resultSetId });
    }
}

public class ResultSetInput
{
    public int ResultSetId { get; set; }

    [Required(ErrorMessage = "Date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Venue is required")]
    [Display(Name = "Venue (Club)")]
    public string VenueClubCode { get; set; } = string.Empty;
}

public class ResultEntryInput
{
    public int ResultEntryId { get; set; }

    [Required(ErrorMessage = "Player is required")]
    [Display(Name = "Player")]
    public int PlayerId { get; set; }

    [Required(ErrorMessage = "HCP is required")]
    [Range(0.0, 99.9, ErrorMessage = "HCP must be between 0.0 and 99.9")]
    [Display(Name = "HCP (xx.x)")]
    public decimal HCP { get; set; }

    [Required(ErrorMessage = "Result is required")]
    [Range(0, 50, ErrorMessage = "Result must be between 0 and 50")]
    [Display(Name = "Result")]
    public int Result { get; set; }
}

public class ResultSetSummary
{
    public int Id { get; set; }
    public string DisplayText { get; set; } = string.Empty;
}
