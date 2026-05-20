using aspnet.Data;
using aspnet.Models;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Repositories;

public class GameEfRepository : IGameRepository
{
    private readonly CasinoDbContext _db;

    public GameEfRepository(CasinoDbContext db) => _db = db;

    public List<Game> GetAll() =>
        _db.Games.ToList();

    public Game? GetById(int id) =>
        _db.Games
           .Include(g => g.Tables).ThenInclude(t => t.Casino)
           .FirstOrDefault(g => g.Id == id);

    public void Create(Game game)
    {
        _db.Games.Add(game);
        _db.SaveChanges();
    }

    public void Update(Game game)
    {
        _db.Entry(game).State = EntityState.Modified;
        _db.SaveChanges();
    }

    public void Delete(int id)
    {
        var game = _db.Games.Find(id);
        if (game is null) return;
        _db.Games.Remove(game);
        _db.SaveChanges();
    }

    public List<Game> Search(string q) =>
        _db.Games
           .Where(g => g.Name.Contains(q) || g.Description.Contains(q))
           .Take(20)
           .ToList();
}
