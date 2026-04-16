// Mock implementation of IEmployeeRepository. Flattens employees from all casinos in SeedData.
// BuildData() also wires the Casino back-reference on each employee (SeedData does not do this).
// Registered as AddSingleton — data is built once at startup and reused.
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
}
