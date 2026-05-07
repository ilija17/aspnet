using aspnet.Data;
using aspnet.Models;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Repositories;

public class EmployeeEfRepository : IEmployeeRepository
{
    private readonly CasinoDbContext _db;

    public EmployeeEfRepository(CasinoDbContext db) => _db = db;

    public List<Employee> GetAll() =>
        _db.Employees
           .Include(e => e.Casino)
           .ToList();

    public Employee? GetById(int id) =>
        _db.Employees
           .Include(e => e.Casino)
           .FirstOrDefault(e => e.Id == id);
}
