// Contract for reservation data access. Returns in-memory seed data — no database.
using aspnet.Models;

namespace aspnet.Repositories;

public interface IReservationRepository
{
    List<Reservation> GetAll();
    Reservation? GetById(int id);
}
