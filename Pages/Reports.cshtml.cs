using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ContactsRazor.Data;
using ContactsRazor.Models;
using ContactsRazor.Helpers;

namespace ContactsRazor.Pages;

[Authorize]
public class ReportsModel : PageModel
{
    private readonly ContactsDbContext _context;

    public ReportsModel(ContactsDbContext context)
    {
        _context = context;
    }

    public Club? CurrentClub { get; set; }
    public string ReportType { get; set; } = "player"; // "player" or "average"
    
    // For "By Player" report
    public List<PlayerResultReport> PlayerResults { get; set; } = new();
    
    // For "By Average" report
    public List<PlayerAverageReport> PlayerAverages { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? reportType = null)
    {
        var clubCode = User.GetClubCode();
        if (string.IsNullOrEmpty(clubCode) || clubCode == "FEDERE")
        {
            TempData["Error"] = "Invalid access. Only Club Captains can view reports.";
            return RedirectToPage("/Dashboard");
        }

        // Get current club
        CurrentClub = await _context.Clubs.FirstOrDefaultAsync(c => c.ClubCode == clubCode);
        if (CurrentClub == null)
        {
            TempData["Error"] = "Club not found.";
            return RedirectToPage("/Dashboard");
        }

        ReportType = reportType ?? "player";

        if (ReportType == "player")
        {
            await LoadPlayerResultsAsync(clubCode);
        }
        else if (ReportType == "average")
        {
            await LoadPlayerAveragesAsync(clubCode);
        }

        return Page();
    }

    private async Task LoadPlayerResultsAsync(string clubCode)
    {
        // Get all result entries for players in this club
        // Join with ResultSet to get venue and date, and Player to get name
        PlayerResults = await _context.ResultEntries
            .Include(re => re.ResultSet)
                .ThenInclude(rs => rs.VenueClub)
            .Include(re => re.Player)
            .Where(re => re.ResultSet.ClubCode == clubCode && re.Player.ClubCode == clubCode)
            .OrderByDescending(re => re.ResultSet.Date)
            .ThenBy(re => re.Player.Name)
            .Select(re => new PlayerResultReport
            {
                PlayerName = re.Player.Name,
                Venue = re.ResultSet.VenueClub != null ? re.ResultSet.VenueClub.LongName : re.ResultSet.VenueClubCode,
                Date = re.ResultSet.Date,
                Result = re.Result
            })
            .ToListAsync();
    }

    private async Task LoadPlayerAveragesAsync(string clubCode)
    {
        // Get all result entries for players in this club, grouped by player
        // Calculate total points, number of games, and average
        var playerStats = await _context.ResultEntries
            .Include(re => re.ResultSet)
            .Include(re => re.Player)
            .Where(re => re.ResultSet.ClubCode == clubCode && re.Player.ClubCode == clubCode)
            .GroupBy(re => new { re.PlayerId, re.Player.Name })
            .Select(g => new
            {
                PlayerId = g.Key.PlayerId,
                PlayerName = g.Key.Name,
                TotalPoints = g.Sum(re => re.Result),
                NumberOfGames = g.Count(),
                Average = (decimal)g.Average(re => (decimal)re.Result)
            })
            .OrderByDescending(x => x.Average)
            .ToListAsync();

        PlayerAverages = playerStats.Select(x => new PlayerAverageReport
        {
            PlayerName = x.PlayerName,
            TotalPoints = x.TotalPoints,
            NumberOfGames = x.NumberOfGames,
            Average = x.Average
        }).ToList();
    }
}

public class PlayerResultReport
{
    public string PlayerName { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Result { get; set; }
}

public class PlayerAverageReport
{
    public string PlayerName { get; set; } = string.Empty;
    public int TotalPoints { get; set; }
    public int NumberOfGames { get; set; }
    public decimal Average { get; set; }
}
