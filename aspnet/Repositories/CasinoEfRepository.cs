using aspnet.Data;
using aspnet.Models;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Repositories;

public class CasinoEfRepository : ICasinoRepository
{
    private readonly CasinoDbContext _db;

    public CasinoEfRepository(CasinoDbContext db) => _db = db;

    public List<Casino> GetAll() =>
        _db.Casinos
           .Include(c => c.Tables).ThenInclude(t => t.Game)
           .Include(c => c.Employees)
           .ToList();

    public Casino? GetById(int id) =>
        _db.Casinos
           .Include(c => c.Tables).ThenInclude(t => t.Game)
           .Include(c => c.Employees)
           .FirstOrDefault(c => c.Id == id);

    public void Create(Casino casino)
    {
        _db.Casinos.Add(casino);
        _db.SaveChanges();
    }

    public void Update(Casino casino)
    {
        _db.Entry(casino).State = EntityState.Modified;
        _db.SaveChanges();
    }

    public void Delete(int id)
    {
        var casino = _db.Casinos.Find(id);
        if (casino is null) return;
        _db.Casinos.Remove(casino);
        _db.SaveChanges();
    }

    public List<Casino> Search(string q) =>
        _db.Casinos
           .Where(c => c.Name.Contains(q) || c.Address.Contains(q))
           .Take(20)
           .ToList();
}
