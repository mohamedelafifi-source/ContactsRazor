using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContactsRazor.Models;

public class User
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [StringLength(255)] // BCrypt hash is 60 chars, but allow extra space
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string ClubCode { get; set; } = string.Empty; // 6 characters: "FEDERE" for Federation, "CLB001"-"CLB010" for clubs
    
    // Helper properties
    [NotMapped]
    public bool IsFederation => ClubCode.ToUpper().Trim() == "FEDERE";
    
    [NotMapped]
    public bool IsClubCaptain => !IsFederation && !string.IsNullOrEmpty(ClubCode);
}
