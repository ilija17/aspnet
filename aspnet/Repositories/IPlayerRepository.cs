using aspnet.Models;

namespace aspnet.Repositories;

public interface IPlayerRepository
{
    List<Player> GetAll();
    Player? GetById(int id);
}
