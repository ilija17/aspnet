// Contract for table data access. Returns in-memory seed data — no database.
using aspnet.Models;

namespace aspnet.Repositories;

public interface ITableRepository
{
    List<Table> GetAll();
    Table? GetById(int id);
}
