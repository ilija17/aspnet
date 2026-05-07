using aspnet.Data;
using aspnet.Models;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Repositories;

public class ReservationEfRepository : IReservationRepository
{
    private readonly CasinoDbContext _db;

    public ReservationEfRepository(CasinoDbContext db) => _db = db;

    public List<Reservation> GetAll() =>
        _db.Reservations
           .Include(r => r.Player)
           .Include(r => r.Table).ThenInclude(t => t.Casino)
           .Include(r => r.Table).ThenInclude(t => t.Game)
           .ToList();

    public Reservation? GetById(int id) =>
        _db.Reservations
           .Include(r => r.Player)
           .Include(r => r.Table).ThenInclude(t => t.Casino)
           .Include(r => r.Table).ThenInclude(t => t.Game)
           .FirstOrDefault(r => r.Id == id);
}
