using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContactsRazor.Models;

public class ResultSet
{
    public int Id { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string ClubCode { get; set; } = string.Empty; // The captain's club who created this

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string VenueClubCode { get; set; } = string.Empty; // The venue (club) where competition was held

    [Required]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Club? Club { get; set; }
    public Club? VenueClub { get; set; }
    public ICollection<ResultEntry> ResultEntries { get; set; } = new List<ResultEntry>();
}
