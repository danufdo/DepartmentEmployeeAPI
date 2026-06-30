using DepartmentEmployeeAPI.Models;

namespace DepartmentEmployeeAPI.Repositories;

public interface IDepartmentRepository
{
    Task<IEnumerable<Department>> GetAllAsync();
    Task<Department?> GetByIdAsync(int departmentId);
    Task<int> InsertAsync(Department department);
    Task UpdateAsync(Department department);
    Task DeleteAsync(int departmentId);
}
