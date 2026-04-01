namespace aspnet.Models;

public class Transaction
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime CreatedAt { get; set; }

    public int PlayerId { get; set; }
    public Player Player { get; set; }
}
