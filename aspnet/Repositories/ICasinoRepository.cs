using aspnet.Models;

namespace aspnet.Repositories;

public interface ICasinoRepository
{
    List<Casino> GetAll();
    Casino? GetById(int id);
}
