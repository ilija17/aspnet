// Contract for game data access. Returns in-memory seed data — no database.
using aspnet.Models;

namespace aspnet.Repositories;

public interface IGameRepository
{
    List<Game> GetAll();
    Game? GetById(int id);
}
