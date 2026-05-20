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

    public void Create(Employee employee)
    {
        _db.Employees.Add(employee);
        _db.SaveChanges();
    }

    public void Update(Employee employee)
    {
        _db.Entry(employee).State = EntityState.Modified;
        _db.SaveChanges();
    }

    public void Delete(int id)
    {
        var employee = _db.Employees.Find(id);
        if (employee is null) return;
        _db.Employees.Remove(employee);
        _db.SaveChanges();
    }

    public List<Employee> Search(string q) =>
        _db.Employees
           .Include(e => e.Casino)
           .Where(e => e.FirstName.Contains(q) || e.LastName.Contains(q) || e.Position.Contains(q) || e.Casino.Name.Contains(q))
           .Take(20)
           .ToList();
}
