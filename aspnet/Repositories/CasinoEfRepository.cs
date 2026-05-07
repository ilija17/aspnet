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
}
