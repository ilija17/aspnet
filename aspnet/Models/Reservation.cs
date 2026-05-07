using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace aspnet.Models;

public class Reservation
{
    [Key]
    public int Id { get; set; }
    public DateTime ReservedAt { get; set; }

    [ForeignKey("Player")]
    public int PlayerId { get; set; }
    public virtual Player Player { get; set; }

    [ForeignKey("Table")]
    public int TableId { get; set; }
    public virtual Table Table { get; set; }
}
