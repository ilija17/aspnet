using aspnet.Models;

namespace aspnet.Repositories;

public interface ICasinoRepository
{
    List<Casino> GetAll();
    Casino? GetById(int id);
    void Create(Casino casino);
    void Update(Casino casino);
    void Delete(int id);
    List<Casino> Search(string q);
}
