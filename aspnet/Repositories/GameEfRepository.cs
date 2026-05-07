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
           .Include(g => g.Tables)
           .FirstOrDefault(g => g.Id == id);
}
