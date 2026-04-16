using aspnet.Data;
using aspnet.Models;

namespace aspnet.Repositories;

public class CasinoMockRepository : ICasinoRepository
{
    private static readonly List<Casino> _data = SeedData.Create().Casinos;

    public List<Casino> GetAll() => _data;

    public Casino? GetById(int id) => _data.FirstOrDefault(c => c.Id == id);
}
