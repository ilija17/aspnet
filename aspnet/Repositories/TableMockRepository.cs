// Mock implementation of ITableRepository. Flattens tables from all casinos in SeedData.
// Registered as AddSingleton — data is built once at startup and reused.
using aspnet.Data;
using aspnet.Models;

namespace aspnet.Repositories;

public class TableMockRepository : ITableRepository
{
    private static readonly List<Table> _data =
        SeedData.Create().Casinos.SelectMany(c => c.Tables).ToList();

    public List<Table> GetAll() => _data;

    public Table? GetById(int id) => _data.FirstOrDefault(t => t.Id == id);
}
