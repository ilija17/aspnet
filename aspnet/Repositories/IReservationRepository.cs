using aspnet.Models;

namespace aspnet.Repositories;

public interface IReservationRepository
{
    List<Reservation> GetAll();
    Reservation? GetById(int id);
    void Create(Reservation reservation);
    void Delete(int id);
    List<Reservation> Search(string q);
}
