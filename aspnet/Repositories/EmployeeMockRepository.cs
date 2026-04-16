using aspnet.Data;
using aspnet.Models;

namespace aspnet.Repositories;

public class EmployeeMockRepository : IEmployeeRepository
{
    private static readonly List<Employee> _data =
        SeedData.Create().Casinos.SelectMany(c => c.Employees).ToList();

    public List<Employee> GetAll() => _data;

    public Employee? GetById(int id) => _data.FirstOrDefault(e => e.Id == id);
}
