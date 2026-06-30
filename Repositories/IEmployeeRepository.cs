using DepartmentEmployeeAPI.Models;

namespace DepartmentEmployeeAPI.Repositories;

public interface IEmployeeRepository
{
    Task<IEnumerable<Employee>> GetAllAsync(int? departmentId = null);
    Task<Employee?> GetByIdAsync(int employeeId);
    Task<int> InsertAsync(Employee employee);
    Task UpdateAsync(Employee employee);
    Task DeleteAsync(int employeeId);
}
