using System.ComponentModel.DataAnnotations;

namespace ContactsRazor.Models;

public class Club
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string ClubCode { get; set; } = string.Empty; // 6-character unique code (e.g., "CLB001", "FEDERE")
    
    [StringLength(6)]
    public string? HmId { get; set; } // 6-digit HM ID, nullable/blank initially
    
    [Required]
    [StringLength(30)]
    public string LongName { get; set; } = string.Empty;
    
    public int? NumberOfPlayers { get; set; } // Nullable, blank initially
    
    // Navigation property
    public ICollection<User> Users { get; set; } = new List<User>();
}
