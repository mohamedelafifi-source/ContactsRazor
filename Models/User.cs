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
    [StringLength(4, MinimumLength = 4)]
    public string ClubCode { get; set; } = string.Empty; // "FEDR" for Federation, "CLB1"-"CLB10" for clubs
    
    // Helper properties
    [NotMapped]
    public bool IsFederation => ClubCode == "FEDR";
    
    [NotMapped]
    public bool IsClubCaptain => ClubCode != "FEDR" && !string.IsNullOrEmpty(ClubCode);
}
