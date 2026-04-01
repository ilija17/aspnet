namespace aspnet.Models;

public class Player
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public DateTime DateOfBirth { get; set; }
    public decimal Balance { get; set; }

    public List<Transaction> Transactions { get; set; } = new();
    public List<Reservation> Reservations { get; set; } = new();
}
