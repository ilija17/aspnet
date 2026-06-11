using aspnet.Data;
using aspnet.Models;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Repositories;

public class PlayerEfRepository : IPlayerRepository
{
    private readonly CasinoDbContext _db;

    public PlayerEfRepository(CasinoDbContext db) => _db = db;

    public List<Player> GetAll() =>
        _db.Players.ToList();

    public Player? GetById(int id) =>
        _db.Players
           .Include(p => p.Transactions)
           .Include(p => p.Reservations).ThenInclude(r => r.Table).ThenInclude(t => t.Casino)
           .Include(p => p.Reservations).ThenInclude(r => r.Table).ThenInclude(t => t.Game)
           .FirstOrDefault(p => p.Id == id);

    public Player? GetByEmail(string email) =>
        _db.Players.FirstOrDefault(p => p.Email == email);

    public void Create(Player player)
    {
        _db.Players.Add(player);
        _db.SaveChanges();
    }

    public void Update(Player player)
    {
        _db.Entry(player).State = EntityState.Modified;
        _db.SaveChanges();
    }

    public void Delete(int id)
    {
        var player = _db.Players.Find(id);
        if (player is null) return;
        _db.Players.Remove(player);
        _db.SaveChanges();
    }

    public List<Player> Search(string q) =>
        _db.Players
           .Include(p => p.Reservations)
           .Include(p => p.Transactions)
           .Where(p => p.FirstName.Contains(q) || p.LastName.Contains(q) || p.Email.Contains(q))
           .Take(20)
           .ToList();
}
