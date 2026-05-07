using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Models;

public class Table
{
    [Key]
    public int Id { get; set; }
    public int TableNumber { get; set; }
    public bool IsAvailable { get; set; }
    [Precision(18, 2)]
    public decimal MinBet { get; set; }
    [Precision(18, 2)]
    public decimal MaxBet { get; set; }

    [ForeignKey("Casino")]
    public int CasinoId { get; set; }
    public virtual Casino Casino { get; set; }

    [ForeignKey("Game")]
    public int GameId { get; set; }
    public virtual Game Game { get; set; }

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
