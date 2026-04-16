using aspnet.Models;

namespace aspnet.Repositories;

public interface ITableRepository
{
    List<Table> GetAll();
    Table? GetById(int id);
}
