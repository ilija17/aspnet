using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace aspnet.Models;

public class Reservation
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Datum rezervacije je obavezan")]
    public DateTime ReservedAt { get; set; }

    [Required(ErrorMessage = "Igrač je obavezan")]
    [ForeignKey("Player")]
    public int PlayerId { get; set; }
    public virtual Player Player { get; set; }

    [Required(ErrorMessage = "Stol je obavezan")]
    [ForeignKey("Table")]
    public int TableId { get; set; }
    public virtual Table Table { get; set; }
}
