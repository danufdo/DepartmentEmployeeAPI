using System.ComponentModel.DataAnnotations;

namespace DepartmentEmployeeAPI.DTOs;

public class DepartmentCreateDto
{
    [Required(ErrorMessage = "Department code is required.")]
    [StringLength(20, ErrorMessage = "Department code cannot exceed 20 characters.")]
    public string DepartmentCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department name is required.")]
    [StringLength(100, ErrorMessage = "Department name cannot exceed 100 characters.")]
    public string DepartmentName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }
}

public class DepartmentPatchDto
{
    [StringLength(20, ErrorMessage = "Department code cannot exceed 20 characters.")]
    public string? DepartmentCode { get; set; }

    [StringLength(100, ErrorMessage = "Department name cannot exceed 100 characters.")]
    public string? DepartmentName { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }
}