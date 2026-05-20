using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Models;

public class Game
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Naziv igre je obavezan")]
    [StringLength(100, ErrorMessage = "Naziv ne smije biti duži od 100 znakova")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Tip igre je obavezan")]
    public GameType Type { get; set; }

    [Range(0.01, 1000000, ErrorMessage = "Minimalni ulog mora biti između 0.01 i 1,000,000")]
    [Precision(18, 2)]
    public decimal MinBet { get; set; }

    [Range(0.01, 1000000, ErrorMessage = "Maksimalni ulog mora biti između 0.01 i 1,000,000")]
    [Precision(18, 2)]
    public decimal MaxBet { get; set; }

    [StringLength(500, ErrorMessage = "Opis ne smije biti duži od 500 znakova")]
    public string Description { get; set; }

    public virtual ICollection<Table> Tables { get; set; } = new List<Table>();
}
