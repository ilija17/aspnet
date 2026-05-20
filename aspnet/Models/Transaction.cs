using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Models;

public class Transaction
{
    [Key]
    public int Id { get; set; }

    [Range(0.01, 1000000, ErrorMessage = "Iznos mora biti između 0.01 i 1,000,000")]
    [Precision(18, 2)]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Tip transakcije je obavezan")]
    public TransactionType Type { get; set; }

    [Required(ErrorMessage = "Datum transakcije je obavezan")]
    public DateTime CreatedAt { get; set; }

    [Required(ErrorMessage = "Igrač je obavezan")]
    [ForeignKey("Player")]
    public int PlayerId { get; set; }
    public virtual Player Player { get; set; }
}
