using aspnet.Data;
using aspnet.Models;

namespace aspnet.Repositories;

public class EmployeeMockRepository : IEmployeeRepository
{
    private static readonly List<Employee> _data = BuildData();

    private static List<Employee> BuildData()
    {
        var casinos = SeedData.Create().Casinos;
        foreach (var casino in casinos)
            foreach (var emp in casino.Employees)
                emp.Casino = casino;
        return casinos.SelectMany(c => c.Employees).ToList();
    }

    public List<Employee> GetAll() => _data;
    public Employee? GetById(int id) => _data.FirstOrDefault(e => e.Id == id);
    public void Create(Employee employee) { employee.Id = _data.Max(e => e.Id) + 1; _data.Add(employee); }
    public void Update(Employee employee) { var i = _data.FindIndex(e => e.Id == employee.Id); if (i >= 0) _data[i] = employee; }
    public void Delete(int id) { var e = _data.FirstOrDefault(e => e.Id == id); if (e is not null) _data.Remove(e); }
    public List<Employee> Search(string q) => _data.Where(e => e.FirstName.Contains(q, StringComparison.OrdinalIgnoreCase) || e.LastName.Contains(q, StringComparison.OrdinalIgnoreCase) || e.Position.Contains(q, StringComparison.OrdinalIgnoreCase)).Take(20).ToList();
}
