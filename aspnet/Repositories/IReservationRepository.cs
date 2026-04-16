using aspnet.Models;

namespace aspnet.Repositories;

public interface IReservationRepository
{
    List<Reservation> GetAll();
    Reservation? GetById(int id);
}
