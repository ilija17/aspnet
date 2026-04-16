// Contract for casino data access. Returns in-memory seed data — no database.
using aspnet.Models;

namespace aspnet.Repositories;

public interface ICasinoRepository
{
    List<Casino> GetAll();
    Casino? GetById(int id);
}
