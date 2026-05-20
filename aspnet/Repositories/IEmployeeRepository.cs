using aspnet.Models;

namespace aspnet.Repositories;

public interface IEmployeeRepository
{
    List<Employee> GetAll();
    Employee? GetById(int id);
    void Create(Employee employee);
    void Update(Employee employee);
    void Delete(int id);
    List<Employee> Search(string q);
}
