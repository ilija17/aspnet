namespace aspnet.Models;

public class Table
{
    public int Id { get; set; }
    public int TableNumber { get; set; }
    public bool IsAvailable { get; set; }
    public decimal MinBet { get; set; }
    public decimal MaxBet { get; set; }

    public int CasinoId { get; set; }
    public Casino Casino { get; set; }

    public int GameId { get; set; }
    public Game Game { get; set; }

    public List<Reservation> Reservations { get; set; } = new();
}
