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
    public bool IsFederation { get; set; }
    public string ReportType { get; set; } = "player"; // "player" or "average"
    
    // For "By Player" report
    public List<PlayerResultReport> PlayerResults { get; set; } = new();
    
    // For "By Average" report
    public List<PlayerAverageReport> PlayerAverages { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? reportType = null)
    {
        var clubCode = User.GetClubCode();
        IsFederation = User.IsFederation();

        if (string.IsNullOrEmpty(clubCode))
        {
            TempData["Error"] = "Invalid access.";
            return RedirectToPage("/Dashboard");
        }

        if (IsFederation)
        {
            // Federation can view all clubs
            CurrentClub = null; // No specific club for Federation
        }
        else
        {
            // Get current club for Club Captain
            CurrentClub = await _context.Clubs.FirstOrDefaultAsync(c => c.ClubCode == clubCode);
            if (CurrentClub == null)
            {
                TempData["Error"] = "Club not found.";
                return RedirectToPage("/Dashboard");
            }
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
        // Get all result entries
        // For Federation: all clubs, for Club Captain: only their club
        IQueryable<ResultEntry> query = _context.ResultEntries
            .Include(re => re.ResultSet)
                .ThenInclude(rs => rs.VenueClub)
            .Include(re => re.Player);

        if (!IsFederation)
        {
            // Filter by club for Club Captains
            query = query.Where(re => re.ResultSet.ClubCode == clubCode && re.Player.ClubCode == clubCode);
        }
        // For Federation, no filtering - show all clubs

        var results = await query
            .OrderByDescending(re => re.ResultSet.Date)
            .ThenBy(re => re.Player.Name)
            .Select(re => new
            {
                PlayerName = re.Player.Name,
                PlayerClubCode = re.Player.ClubCode,
                Venue = re.ResultSet.VenueClub != null ? re.ResultSet.VenueClub.LongName : re.ResultSet.VenueClubCode,
                Date = re.ResultSet.Date,
                Result = re.Result
            })
            .ToListAsync();

        // Get club names for all unique club codes
        var clubCodes = results.Select(r => r.PlayerClubCode).Distinct().ToList();
        var clubs = await _context.Clubs
            .Where(c => clubCodes.Contains(c.ClubCode))
            .ToDictionaryAsync(c => c.ClubCode, c => c.LongName);

        PlayerResults = results.Select(r => new PlayerResultReport
        {
            PlayerName = r.PlayerName,
            ClubName = clubs.ContainsKey(r.PlayerClubCode) ? clubs[r.PlayerClubCode] : r.PlayerClubCode,
            Venue = r.Venue,
            Date = r.Date,
            Result = r.Result
        }).ToList();
    }

    private async Task LoadPlayerAveragesAsync(string clubCode)
    {
        // Get all result entries, grouped by player
        // For Federation: all clubs, for Club Captain: only their club
        IQueryable<ResultEntry> query = _context.ResultEntries
            .Include(re => re.ResultSet)
            .Include(re => re.Player);

        if (!IsFederation)
        {
            // Filter by club for Club Captains
            query = query.Where(re => re.ResultSet.ClubCode == clubCode && re.Player.ClubCode == clubCode);
        }
        // For Federation, no filtering - show all clubs

        // First get the grouped data
        var playerStats = await query
            .GroupBy(re => new { re.PlayerId, re.Player.Name, re.Player.ClubCode })
            .Select(g => new
            {
                PlayerId = g.Key.PlayerId,
                PlayerName = g.Key.Name,
                ClubCode = g.Key.ClubCode,
                TotalPoints = g.Sum(re => re.Result),
                NumberOfGames = g.Count(),
                Average = (decimal)g.Average(re => (decimal)re.Result)
            })
            .OrderByDescending(x => x.Average)
            .ToListAsync();

        // Get club names for the club codes
        var clubCodes = playerStats.Select(x => x.ClubCode).Distinct().ToList();
        var clubs = await _context.Clubs
            .Where(c => clubCodes.Contains(c.ClubCode))
            .ToDictionaryAsync(c => c.ClubCode, c => c.LongName);

        PlayerAverages = playerStats.Select(x => new PlayerAverageReport
        {
            PlayerName = x.PlayerName,
            ClubName = clubs.ContainsKey(x.ClubCode) ? clubs[x.ClubCode] : x.ClubCode,
            TotalPoints = x.TotalPoints,
            NumberOfGames = x.NumberOfGames,
            Average = x.Average
        }).ToList();
    }
}

public class PlayerResultReport
{
    public string PlayerName { get; set; } = string.Empty;
    public string ClubName { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Result { get; set; }
}

public class PlayerAverageReport
{
    public string PlayerName { get; set; } = string.Empty;
    public string ClubName { get; set; } = string.Empty;
    public int TotalPoints { get; set; }
    public int NumberOfGames { get; set; }
    public decimal Average { get; set; }
}
