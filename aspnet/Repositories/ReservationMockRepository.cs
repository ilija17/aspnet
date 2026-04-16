using aspnet.Data;
using aspnet.Models;

namespace aspnet.Repositories;

public class ReservationMockRepository : IReservationRepository
{
    private static readonly List<Reservation> _data = SeedData.Create().Reservations;

    public List<Reservation> GetAll() => _data;

    public Reservation? GetById(int id) => _data.FirstOrDefault(r => r.Id == id);
}
