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
    public void Create(Game game) { game.Id = _data.Max(g => g.Id) + 1; _data.Add(game); }
    public void Update(Game game) { var i = _data.FindIndex(g => g.Id == game.Id); if (i >= 0) _data[i] = game; }
    public void Delete(int id) { var g = _data.FirstOrDefault(g => g.Id == id); if (g is not null) _data.Remove(g); }
    public List<Game> Search(string q) => _data.Where(g => g.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).Take(20).ToList();
}
