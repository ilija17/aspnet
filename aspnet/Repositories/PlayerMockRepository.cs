using aspnet.Data;
using aspnet.Models;

namespace aspnet.Repositories;

public class PlayerMockRepository : IPlayerRepository
{
    private static readonly List<Player> _data = SeedData.Create().Players;

    public List<Player> GetAll() => _data;
    public Player? GetById(int id) => _data.FirstOrDefault(p => p.Id == id);
    public void Create(Player player) { player.Id = _data.Max(p => p.Id) + 1; _data.Add(player); }
    public void Update(Player player) { var i = _data.FindIndex(p => p.Id == player.Id); if (i >= 0) _data[i] = player; }
    public void Delete(int id) { var p = _data.FirstOrDefault(p => p.Id == id); if (p is not null) _data.Remove(p); }
    public List<Player> Search(string q) => _data.Where(p => p.FirstName.Contains(q, StringComparison.OrdinalIgnoreCase) || p.LastName.Contains(q, StringComparison.OrdinalIgnoreCase) || p.Email.Contains(q, StringComparison.OrdinalIgnoreCase)).Take(20).ToList();
}
