// Contract for employee data access. Returns in-memory seed data — no database.
using aspnet.Models;

namespace aspnet.Repositories;

public interface IEmployeeRepository
{
    List<Employee> GetAll();
    Employee? GetById(int id);
}
