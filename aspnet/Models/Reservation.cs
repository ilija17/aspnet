namespace aspnet.Models;

public class Reservation
{
    public int Id { get; set; }
    public DateTime ReservedAt { get; set; }

    public int PlayerId { get; set; }
    public Player Player { get; set; }

    public int TableId { get; set; }
    public Table Table { get; set; }
}
