using System.ComponentModel.DataAnnotations;

namespace ContactsRazor.Models;

public class Club
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(4, MinimumLength = 4)]
    public string ClubCode { get; set; } = string.Empty;
    
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string ClubId { get; set; } = string.Empty; // 6-digit unique identifier
    
    [Required]
    [StringLength(30)]
    public string LongName { get; set; } = string.Empty;
    
    public int NumberOfPlayers { get; set; } = 0;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public ICollection<User> Users { get; set; } = new List<User>();
}
