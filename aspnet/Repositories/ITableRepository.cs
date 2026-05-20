using aspnet.Models;

namespace aspnet.Repositories;

public interface ITableRepository
{
    List<Table> GetAll();
    Table? GetById(int id);
    void Create(Table table);
    void Update(Table table);
    void Delete(int id);
    List<Table> Search(string q);
}
