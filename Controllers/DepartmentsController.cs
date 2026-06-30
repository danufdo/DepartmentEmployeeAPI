using DepartmentEmployeeAPI.DTOs;
using DepartmentEmployeeAPI.Models;
using DepartmentEmployeeAPI.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DepartmentEmployeeAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentRepository _repository;

    public DepartmentsController(IDepartmentRepository repository)
    {
        _repository = repository;
    }

    // GET api/departments
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Department>>> GetAll()
    {
        var departments = await _repository.GetAllAsync();
        return Ok(departments);
    }

    // GET api/departments/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Department>> GetById(int id)
    {
        var department = await _repository.GetByIdAsync(id);
        if (department is null)
            return NotFound(new { message = $"Department {id} was not found." });

        return Ok(department);
    }

    // POST api/departments
    [HttpPost]
    public async Task<ActionResult<Department>> Create(DepartmentCreateDto dto)
    {
        var department = new Department
        {
            DepartmentCode = dto.DepartmentCode.Trim(),
            DepartmentName = dto.DepartmentName.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim()
        };

        department.DepartmentId = await _repository.InsertAsync(department);
        var created = await _repository.GetByIdAsync(department.DepartmentId);

        return CreatedAtAction(nameof(GetById), new { id = department.DepartmentId }, created);
    }

    // PATCH api/departments/5
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Patch(int id, DepartmentPatchDto dto)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
            return NotFound(new { message = $"Department {id} was not found." });

        if (dto.DepartmentCode is not null)
            existing.DepartmentCode = dto.DepartmentCode.Trim();

        if (dto.DepartmentName is not null)
            existing.DepartmentName = dto.DepartmentName.Trim();

        if (dto.Description is not null)
            existing.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

        await _repository.UpdateAsync(existing);
        return NoContent();
    }

    // DELETE api/departments/5  (soft delete — blocked if active employees remain)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
            return NotFound(new { message = $"Department {id} was not found." });

        await _repository.DeleteAsync(id);
        return NoContent();
    }
}
