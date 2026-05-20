using aspnet.Data;
using aspnet.Models;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Repositories;

public class TableEfRepository : ITableRepository
{
    private readonly CasinoDbContext _db;

    public TableEfRepository(CasinoDbContext db) => _db = db;

    public List<Table> GetAll() =>
        _db.Tables
           .Include(t => t.Casino)
           .Include(t => t.Game)
           .ToList();

    public Table? GetById(int id) =>
        _db.Tables
           .Include(t => t.Casino)
           .Include(t => t.Game)
           .Include(t => t.Reservations).ThenInclude(r => r.Player)
           .FirstOrDefault(t => t.Id == id);

    public void Create(Table table)
    {
        _db.Tables.Add(table);
        _db.SaveChanges();
    }

    public void Update(Table table)
    {
        _db.Entry(table).State = EntityState.Modified;
        _db.SaveChanges();
    }

    public void Delete(int id)
    {
        var table = _db.Tables.Find(id);
        if (table is null) return;
        _db.Tables.Remove(table);
        _db.SaveChanges();
    }

    public List<Table> Search(string q) =>
        _db.Tables
           .Include(t => t.Casino)
           .Include(t => t.Game)
           .Where(t => t.Casino.Name.Contains(q) || t.Game.Name.Contains(q) || t.TableNumber.ToString().Contains(q))
           .Take(20)
           .ToList();
}
