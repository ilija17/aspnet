using aspnet.Models;

namespace aspnet.Repositories;

public interface IGameRepository
{
    List<Game> GetAll();
    Game? GetById(int id);
    void Create(Game game);
    void Update(Game game);
    void Delete(int id);
    List<Game> Search(string q);
}
