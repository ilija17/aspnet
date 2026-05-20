using aspnet.Data;
using aspnet.Models;

namespace aspnet.Repositories;

public class CasinoMockRepository : ICasinoRepository
{
    private static readonly List<Casino> _data = SeedData.Create().Casinos;

    public List<Casino> GetAll() => _data;
    public Casino? GetById(int id) => _data.FirstOrDefault(c => c.Id == id);
    public void Create(Casino casino) { casino.Id = _data.Max(c => c.Id) + 1; _data.Add(casino); }
    public void Update(Casino casino) { var i = _data.FindIndex(c => c.Id == casino.Id); if (i >= 0) _data[i] = casino; }
    public void Delete(int id) { var c = _data.FirstOrDefault(c => c.Id == id); if (c is not null) _data.Remove(c); }
    public List<Casino> Search(string q) => _data.Where(c => c.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || c.Address.Contains(q, StringComparison.OrdinalIgnoreCase)).Take(20).ToList();
}
