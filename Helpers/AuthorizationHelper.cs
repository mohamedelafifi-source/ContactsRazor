using System.Security.Claims;

namespace ContactsRazor.Helpers;

public static class AuthorizationHelper
{
    /// <summary>
    /// Get the user's Club Code (4-character code)
    /// </summary>
    public static string? GetClubCode(this ClaimsPrincipal user)
    {
        return user.FindFirst("ClubCode")?.Value;
    }

    /// <summary>
    /// Check if user is Federation (ClubCode == "FEDR")
    /// </summary>
    public static bool IsFederation(this ClaimsPrincipal user)
    {
        return GetClubCode(user) == "FEDR";
    }

    /// <summary>
    /// Check if user is Club Captain (ClubCode != "FEDR")
    /// </summary>
    public static bool IsClubCaptain(this ClaimsPrincipal user)
    {
        var clubCode = GetClubCode(user);
        return !string.IsNullOrEmpty(clubCode) && clubCode != "FEDR";
    }

    /// <summary>
    /// Check if user can access a specific club's data
    /// Federation can access all clubs, Club Captains can only access their own
    /// </summary>
    public static bool CanAccessClub(this ClaimsPrincipal user, string clubCode)
    {
        // Federation can access all clubs
        if (IsFederation(user))
        {
            return true;
        }

        // Club Captain can only access their own club
        return GetClubCode(user) == clubCode;
    }

    /// <summary>
    /// Get user's username
    /// </summary>
    public static string? GetUsername(this ClaimsPrincipal user)
    {
        return user.Identity?.Name;
    }
}
