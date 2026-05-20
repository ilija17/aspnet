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

    public void Create(Reservation reservation)
    {
        _db.Reservations.Add(reservation);
        _db.SaveChanges();
    }

    public void Delete(int id)
    {
        var reservation = _db.Reservations.Find(id);
        if (reservation is null) return;
        _db.Reservations.Remove(reservation);
        _db.SaveChanges();
    }

    public List<Reservation> Search(string q) =>
        _db.Reservations
           .Include(r => r.Player)
           .Include(r => r.Table).ThenInclude(t => t.Casino)
           .Where(r => r.Player.FirstName.Contains(q) || r.Player.LastName.Contains(q) || r.Table.Casino.Name.Contains(q))
           .Take(20)
           .ToList();
}
