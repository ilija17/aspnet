// Mock implementation of IGameRepository. Extracts distinct games from table navigation
// properties in SeedData (games are not stored top-level, only inside Tables).
// Registered as AddSingleton — data is built once at startup and reused.
using aspnet.Data;
using aspnet.Models;

namespace aspnet.Repositories;

public class GameMockRepository : IGameRepository
{
    private static readonly List<Game> _data =
        SeedData.Create().Casinos
            .SelectMany(c => c.Tables)
            .Select(t => t.Game)
            .DistinctBy(g => g.Id)
            .ToList();

    public List<Game> GetAll() => _data;

    public Game? GetById(int id) => _data.FirstOrDefault(g => g.Id == id);
}
