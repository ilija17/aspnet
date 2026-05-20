using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Models;

public class Table
{
    [Key]
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Broj stola mora biti pozitivan")]
    public int TableNumber { get; set; }

    public bool IsAvailable { get; set; }

    [Range(0.01, 1000000, ErrorMessage = "Minimalni ulog mora biti između 0.01 i 1,000,000")]
    [Precision(18, 2)]
    public decimal MinBet { get; set; }

    [Range(0.01, 1000000, ErrorMessage = "Maksimalni ulog mora biti između 0.01 i 1,000,000")]
    [Precision(18, 2)]
    public decimal MaxBet { get; set; }

    [Required(ErrorMessage = "Casino je obavezan")]
    [ForeignKey("Casino")]
    public int CasinoId { get; set; }
    public virtual Casino Casino { get; set; }

    [Required(ErrorMessage = "Igra je obavezna")]
    [ForeignKey("Game")]
    public int GameId { get; set; }
    public virtual Game Game { get; set; }

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
