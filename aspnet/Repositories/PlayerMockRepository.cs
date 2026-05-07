// Mock implementation of IPlayerRepository. Serves static player data from SeedData (Lab 1).
// Registered as AddSingleton — data is built once at startup and reused.
using aspnet.Data;
using aspnet.Models;

namespace aspnet.Repositories;

public class PlayerMockRepository : IPlayerRepository
{
    private static readonly List<Player> _data = SeedData.Create().Players;

    public List<Player> GetAll() => _data;

    public Player? GetById(int id) => _data.FirstOrDefault(p => p.Id == id);

    public void Update(Player player)
    {
        var index = _data.FindIndex(p => p.Id == player.Id);
        if (index >= 0) _data[index] = player;
    }
}
