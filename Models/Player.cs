using System.ComponentModel.DataAnnotations;

namespace ContactsRazor.Models;

public class Player
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty; // 6-digit code, unique everywhere
    
    [Required]
    [StringLength(30)]
    public string Name { get; set; } = string.Empty; // 30 characters, unique everywhere
    
    [Required]
    public decimal Index { get; set; } // Format: nn.n (e.g., 9.4, 21.0)
    
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string ClubCode { get; set; } = string.Empty; // 6-character club code
}
