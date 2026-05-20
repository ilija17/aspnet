using aspnet.Data;
using aspnet.Models;

namespace aspnet.Repositories;

public class ReservationMockRepository : IReservationRepository
{
    private static readonly List<Reservation> _data = SeedData.Create().Reservations;

    public List<Reservation> GetAll() => _data;
    public Reservation? GetById(int id) => _data.FirstOrDefault(r => r.Id == id);
    public void Create(Reservation reservation) { reservation.Id = _data.Max(r => r.Id) + 1; _data.Add(reservation); }
    public void Delete(int id) { var r = _data.FirstOrDefault(r => r.Id == id); if (r is not null) _data.Remove(r); }
    public List<Reservation> Search(string q) => _data.Where(r => (r.Player?.FirstName + " " + r.Player?.LastName).Contains(q, StringComparison.OrdinalIgnoreCase)).Take(20).ToList();
}
