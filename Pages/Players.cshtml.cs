using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ContactsRazor.Data;
using ContactsRazor.Models;
using ContactsRazor.Helpers;
using ContactsRazor.Services;
using System.ComponentModel.DataAnnotations;

namespace ContactsRazor.Pages;

[Authorize]
public class PlayersModel : PageModel
{
    private readonly ContactsDbContext _context;
    private readonly PlayerLoaderService _playerLoader;

    public PlayersModel(ContactsDbContext context, PlayerLoaderService playerLoader)
    {
        _context = context;
        _playerLoader = playerLoader;
    }

    [BindProperty]
    public PlayerInput PlayerInput { get; set; } = new();

    public Club? CurrentClub { get; set; }
    public Player? CurrentPlayer { get; set; }
    public int? PreviousPlayerId { get; set; }
    public int? NextPlayerId { get; set; }
    public int CurrentPlayerIndex { get; set; }
    public int TotalPlayers { get; set; }
    public List<string> AvailableFiles { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id = null, bool @new = false)
    {
        var clubCode = User.GetClubCode();
        if (string.IsNullOrEmpty(clubCode) || clubCode == "FEDERE")
        {
            TempData["Error"] = "Invalid access. Club Captains can only access their club's players.";
            return RedirectToPage("/Dashboard");
        }

        // Get current club
        CurrentClub = await _context.Clubs.FirstOrDefaultAsync(c => c.ClubCode == clubCode);
        if (CurrentClub == null)
        {
            TempData["Error"] = "Club not found.";
            return RedirectToPage("/Dashboard");
        }

        // Get all players for this club, ordered by Code
        var players = await _context.Players
            .Where(p => p.ClubCode == clubCode)
            .OrderBy(p => p.Code)
            .ToListAsync();

        TotalPlayers = players.Count;

        // Get available player files for import
        AvailableFiles = _playerLoader.GetAvailablePlayerFiles();

        // Handle "new player" request explicitly
        if (@new)
        {
            // Show empty form for new player
            CurrentPlayerIndex = 0;
            CurrentPlayer = null;
            PlayerInput.PlayerId = 0;
            PlayerInput.Code = string.Empty;
            PlayerInput.Name = string.Empty;
            PlayerInput.Index = 0;
            return Page();
        }

        // Get current player
        if (id.HasValue && id.Value > 0)
        {
            CurrentPlayer = players.FirstOrDefault(p => p.Id == id.Value);
        }
        else if (players.Any())
        {
            // If no id provided and players exist, show first player
            CurrentPlayer = players.First();
        }

        // Set up Next/Previous navigation and populate form
        if (CurrentPlayer != null && players.Any())
        {
            var currentIndex = players.IndexOf(CurrentPlayer);
            CurrentPlayerIndex = currentIndex + 1;

            // Previous player
            if (currentIndex > 0)
            {
                PreviousPlayerId = players[currentIndex - 1].Id;
            }

            // Next player
            if (currentIndex < players.Count - 1)
            {
                NextPlayerId = players[currentIndex + 1].Id;
            }

            // Populate form with current player data
            PlayerInput.Code = CurrentPlayer.Code;
            PlayerInput.Name = CurrentPlayer.Name;
            PlayerInput.Index = CurrentPlayer.Index;
            PlayerInput.PlayerId = CurrentPlayer.Id;
        }
        else
        {
            // No players exist - show empty form for first player
            CurrentPlayerIndex = 0;
            CurrentPlayer = null;
            PlayerInput.PlayerId = 0;
            PlayerInput.Code = string.Empty;
            PlayerInput.Name = string.Empty;
            PlayerInput.Index = 0;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var clubCode = User.GetClubCode();
        if (string.IsNullOrEmpty(clubCode) || clubCode == "FEDERE")
        {
            TempData["Error"] = "Invalid access.";
            return RedirectToPage("/Dashboard");
        }

        if (!ModelState.IsValid)
        {
            return await OnGetAsync(PlayerInput.PlayerId > 0 ? PlayerInput.PlayerId : null);
        }

        // Validate Code is exactly 6 digits
        if (PlayerInput.Code.Length != 6 || !PlayerInput.Code.All(char.IsDigit))
        {
            ModelState.AddModelError(nameof(PlayerInput.Code), "Code must be exactly 6 digits.");
            return await OnGetAsync(PlayerInput.PlayerId > 0 ? PlayerInput.PlayerId : null);
        }

        // Validate Name length
        if (PlayerInput.Name.Length > 30)
        {
            ModelState.AddModelError(nameof(PlayerInput.Name), "Name must be 30 characters or less.");
            return await OnGetAsync(PlayerInput.PlayerId > 0 ? PlayerInput.PlayerId : null);
        }

        // Check for duplicate Code (global uniqueness)
        var existingByCode = await _context.Players
            .FirstOrDefaultAsync(p => p.Code == PlayerInput.Code && p.Id != PlayerInput.PlayerId);
        if (existingByCode != null)
        {
            ModelState.AddModelError(nameof(PlayerInput.Code), $"Code '{PlayerInput.Code}' already exists for player '{existingByCode.Name}' (Club: {existingByCode.ClubCode}).");
            return await OnGetAsync(PlayerInput.PlayerId > 0 ? PlayerInput.PlayerId : null);
        }

        // Check for duplicate Name (global uniqueness)
        var existingByName = await _context.Players
            .FirstOrDefaultAsync(p => p.Name == PlayerInput.Name && p.Id != PlayerInput.PlayerId);
        if (existingByName != null)
        {
            ModelState.AddModelError(nameof(PlayerInput.Name), $"Name '{PlayerInput.Name}' already exists for player with Code '{existingByName.Code}' (Club: {existingByName.ClubCode}).");
            return await OnGetAsync(PlayerInput.PlayerId > 0 ? PlayerInput.PlayerId : null);
        }

        if (PlayerInput.PlayerId == 0)
        {
            // Add new player
            var player = new Player
            {
                Code = PlayerInput.Code,
                Name = PlayerInput.Name,
                Index = PlayerInput.Index,
                ClubCode = clubCode
            };

            _context.Players.Add(player);
            await _context.SaveChangesAsync();

            // Update club's NumberOfPlayers
            var playerCount = await _context.Players.CountAsync(p => p.ClubCode == clubCode);
            var club = await _context.Clubs.FirstOrDefaultAsync(c => c.ClubCode == clubCode);
            if (club != null)
            {
                club.NumberOfPlayers = playerCount;
                await _context.SaveChangesAsync();
            }

            TempData["Message"] = $"Player '{PlayerInput.Name}' added successfully!";
            return RedirectToPage("/Players", new { id = player.Id });
        }
        else
        {
            // Update existing player
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == PlayerInput.PlayerId && p.ClubCode == clubCode);
            if (player == null)
            {
                TempData["Error"] = "Player not found.";
                return RedirectToPage("/Players");
            }

            player.Code = PlayerInput.Code;
            player.Name = PlayerInput.Name;
            player.Index = PlayerInput.Index;

            await _context.SaveChangesAsync();

            TempData["Message"] = $"Player '{PlayerInput.Name}' updated successfully!";
            return RedirectToPage("/Players", new { id = player.Id });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var clubCode = User.GetClubCode();
        if (string.IsNullOrEmpty(clubCode) || clubCode == "FEDERE")
        {
            TempData["Error"] = "Invalid access.";
            return RedirectToPage("/Dashboard");
        }

        var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id && p.ClubCode == clubCode);
        if (player == null)
        {
            TempData["Error"] = "Player not found.";
            return RedirectToPage("/Players");
        }

        var playerName = player.Name;
        
        // Get next player ID before deleting
        var players = await _context.Players
            .Where(p => p.ClubCode == clubCode)
            .OrderBy(p => p.Code)
            .ToListAsync();
        
        var currentIndex = players.FindIndex(p => p.Id == id);
        int? nextPlayerId = null;
        if (players.Count > 1)
        {
            if (currentIndex < players.Count - 1)
            {
                nextPlayerId = players[currentIndex + 1].Id;
            }
            else if (currentIndex > 0)
            {
                nextPlayerId = players[currentIndex - 1].Id;
            }
        }

        _context.Players.Remove(player);
        await _context.SaveChangesAsync();

        // Update club's NumberOfPlayers
        var playerCount = await _context.Players.CountAsync(p => p.ClubCode == clubCode);
        var club = await _context.Clubs.FirstOrDefaultAsync(c => c.ClubCode == clubCode);
        if (club != null)
        {
            club.NumberOfPlayers = playerCount;
            await _context.SaveChangesAsync();
        }

        TempData["Message"] = $"Player '{playerName}' deleted successfully!";
        
        // Redirect to next player or empty form
        if (nextPlayerId.HasValue)
        {
            return RedirectToPage("/Players", new { id = nextPlayerId.Value });
        }
        else
        {
            return RedirectToPage("/Players");
        }
    }

    public async Task<IActionResult> OnPostImportAsync(string fileName)
    {
        var clubCode = User.GetClubCode();
        if (string.IsNullOrEmpty(clubCode) || clubCode == "FEDERE")
        {
            TempData["Error"] = "Invalid access.";
            return RedirectToPage("/Dashboard");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            TempData["Error"] = "Please select a file to import.";
            return RedirectToPage("/Players");
        }

        var result = await _playerLoader.LoadPlayersFromFileAsync(fileName, clubCode);

        if (result.Success)
        {
            TempData["Message"] = $"Successfully imported {result.PlayersAdded} player(s) from '{fileName}'.";
        }
        else
        {
            var errorMsg = $"Import failed. {result.PlayersAdded} player(s) imported.";
            if (result.Errors.Any())
            {
                // Show all errors, but limit display length
                var allErrors = string.Join("; ", result.Errors);
                if (allErrors.Length > 500)
                {
                    errorMsg += $" Errors: {allErrors.Substring(0, 500)}... (truncated, see first {result.Errors.Count} errors)";
                }
                else
                {
                    errorMsg += $" Errors: {allErrors}";
                }
            }
            TempData["Error"] = errorMsg;
        }

        if (result.Warnings.Any())
        {
            TempData["Warning"] = string.Join("; ", result.Warnings);
        }

        return RedirectToPage("/Players");
    }
}

// PlayerInput class for form binding and validation
public class PlayerInput
{
    public int PlayerId { get; set; } // 0 = new player, >0 = existing player

    [Required(ErrorMessage = "Code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be exactly 6 digits")]
    [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "Code must be exactly 6 digits")]
    [Display(Name = "Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required")]
    [StringLength(30, ErrorMessage = "Name must be 30 characters or less")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Index is required")]
    [Display(Name = "Index")]
    public decimal Index { get; set; }
}
