using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Models;

public class Player
{
    [Key]
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public DateTime DateOfBirth { get; set; }
    [Precision(18, 2)]
    public decimal Balance { get; set; }

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
