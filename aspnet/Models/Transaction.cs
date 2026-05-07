using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Models;

public class Transaction
{
    [Key]
    public int Id { get; set; }
    [Precision(18, 2)]
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime CreatedAt { get; set; }

    [ForeignKey("Player")]
    public int PlayerId { get; set; }
    public virtual Player Player { get; set; }
}
