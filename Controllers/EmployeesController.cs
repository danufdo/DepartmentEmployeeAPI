using DepartmentEmployeeAPI.DTOs;
using DepartmentEmployeeAPI.Models;
using DepartmentEmployeeAPI.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DepartmentEmployeeAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeRepository _repository;
    private readonly IDepartmentRepository _departmentRepository;

    public EmployeesController(IEmployeeRepository repository, IDepartmentRepository departmentRepository)
    {
        _repository = repository;
        _departmentRepository = departmentRepository;
    }

    // GET api/employees
    // GET api/employees?departmentId=3
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Employee>>> GetAll([FromQuery] int? departmentId)
    {
        var employees = await _repository.GetAllAsync(departmentId);
        return Ok(employees);
    }

    // GET api/employees/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Employee>> GetById(int id)
    {
        var employee = await _repository.GetByIdAsync(id);
        if (employee is null)
            return NotFound(new { message = $"Employee {id} was not found." });

        return Ok(employee);
    }

    // POST api/employees
    [HttpPost]
    public async Task<ActionResult<Employee>> Create(EmployeeCreateDto dto)
    {
        var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId);
        if (department is null || !department.IsActive)
            return BadRequest(new { message = "Selected department does not exist or is inactive." });

        var employee = new Employee
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            DateOfBirth = dto.DateOfBirth.Date,
            Salary = dto.Salary,
            DepartmentId = dto.DepartmentId
        };

        employee.EmployeeId = await _repository.InsertAsync(employee);
        var created = await _repository.GetByIdAsync(employee.EmployeeId);

        return CreatedAtAction(nameof(GetById), new { id = employee.EmployeeId }, created);
    }

    // PATCH api/employees/5
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Patch(int id, EmployeePatchDto dto)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
            return NotFound(new { message = $"Employee {id} was not found." });

        if (dto.DepartmentId.HasValue)
        {
            var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId.Value);
            if (department is null || !department.IsActive)
                return BadRequest(new { message = "Selected department does not exist or is inactive." });

            existing.DepartmentId = dto.DepartmentId.Value;
        }

        if (dto.FirstName is not null)
            existing.FirstName = dto.FirstName.Trim();

        if (dto.LastName is not null)
            existing.LastName = dto.LastName.Trim();

        if (dto.Email is not null)
            existing.Email = dto.Email.Trim().ToLowerInvariant();

        if (dto.DateOfBirth.HasValue)
            existing.DateOfBirth = dto.DateOfBirth.Value.Date;

        if (dto.Salary.HasValue)
            existing.Salary = dto.Salary.Value;

        await _repository.UpdateAsync(existing);
        return NoContent();
    }

    // DELETE api/employees/5  (soft delete)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
            return NotFound(new { message = $"Employee {id} was not found." });

        await _repository.DeleteAsync(id);
        return NoContent();
    }
}
