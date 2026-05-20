using aspnet.Models;

namespace aspnet.Repositories;

public interface IPlayerRepository
{
    List<Player> GetAll();
    Player? GetById(int id);
    void Create(Player player);
    void Update(Player player);
    void Delete(int id);
    List<Player> Search(string q);
}
