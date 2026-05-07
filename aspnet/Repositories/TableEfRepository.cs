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
}
