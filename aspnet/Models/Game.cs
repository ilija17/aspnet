namespace aspnet.Models;

public class Game
{
    public int Id { get; set; }
    public string Name { get; set; }
    public GameType Type { get; set; }
    public decimal MinBet { get; set; }
    public decimal MaxBet { get; set; }
    public string Description { get; set; }

    public List<Table> Tables { get; set; } = new();
}
