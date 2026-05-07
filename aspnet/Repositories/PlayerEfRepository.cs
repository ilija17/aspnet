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
           .Include(p => p.Reservations).ThenInclude(r => r.Table)
           .FirstOrDefault(p => p.Id == id);

    public void Update(Player player)
    {
        _db.Entry(player).State = EntityState.Modified;
        _db.SaveChanges();
    }
}
