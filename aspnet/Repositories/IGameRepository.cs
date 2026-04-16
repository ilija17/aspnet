using aspnet.Models;

namespace aspnet.Repositories;

public interface IGameRepository
{
    List<Game> GetAll();
    Game? GetById(int id);
}
