// Contract for player data access. Returns in-memory seed data — no database.
using aspnet.Models;

namespace aspnet.Repositories;

public interface IPlayerRepository
{
    List<Player> GetAll();
    Player? GetById(int id);
}
