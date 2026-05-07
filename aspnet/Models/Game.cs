using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Models;

public class Game
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public GameType Type { get; set; }
    [Precision(18, 2)]
    public decimal MinBet { get; set; }
    [Precision(18, 2)]
    public decimal MaxBet { get; set; }
    public string Description { get; set; }

    public virtual ICollection<Table> Tables { get; set; } = new List<Table>();
}
