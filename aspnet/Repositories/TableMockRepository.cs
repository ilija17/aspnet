using aspnet.Data;
using aspnet.Models;

namespace aspnet.Repositories;

public class TableMockRepository : ITableRepository
{
    private static readonly List<Table> _data =
        SeedData.Create().Casinos.SelectMany(c => c.Tables).ToList();

    public List<Table> GetAll() => _data;
    public Table? GetById(int id) => _data.FirstOrDefault(t => t.Id == id);
    public void Create(Table table) { table.Id = _data.Max(t => t.Id) + 1; _data.Add(table); }
    public void Update(Table table) { var i = _data.FindIndex(t => t.Id == table.Id); if (i >= 0) _data[i] = table; }
    public void Delete(int id) { var t = _data.FirstOrDefault(t => t.Id == id); if (t is not null) _data.Remove(t); }
    public List<Table> Search(string q) => _data.Where(t => t.TableNumber.ToString().Contains(q)).Take(20).ToList();
}
