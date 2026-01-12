using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContactsRazor.Models;

public class ResultEntry
{
    public int Id { get; set; }

    [Required]
    public int ResultSetId { get; set; }

    [Required]
    public int PlayerId { get; set; }

    [Required]
    [Column(TypeName = "decimal(4,1)")] // xx.x format
    public decimal HCP { get; set; }

    [Required]
    [Range(0, 50, ErrorMessage = "Result must be between 0 and 50")]
    public int Result { get; set; }

    // Navigation properties
    public ResultSet? ResultSet { get; set; }
    public Player? Player { get; set; }
}
