using System.ComponentModel.DataAnnotations;

namespace DepartmentEmployeeAPI.DTOs;

public class EmployeeCreateDto : IValidatableObject
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Email address is not valid.")]
    [StringLength(150, ErrorMessage = "Email address cannot exceed 150 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of birth is required.")]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    [Required(ErrorMessage = "Salary is required.")]
    [Range(0, double.MaxValue, ErrorMessage = "Salary must be zero or greater.")]
    public decimal Salary { get; set; }

    [Required(ErrorMessage = "Department is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "A valid department must be selected.")]
    public int DepartmentId { get; set; }

    // Mirrors the CK_Employees_DateOfBirth check constraint and adds a sensible
    // minimum working age — caught here so the user sees the message instantly
    // instead of round-tripping to the database to find out.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateOfBirth.Date > DateTime.Today)
        {
            yield return new ValidationResult(
                "Date of birth cannot be in the future.",
                new[] { nameof(DateOfBirth) });
            yield break;
        }

        var age = DateTime.Today.Year - DateOfBirth.Year;
        if (DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;

        if (age < 18)
        {
            yield return new ValidationResult(
                "Employee must be at least 18 years old.",
                new[] { nameof(DateOfBirth) });
        }
    }
}

public class EmployeePatchDto
{
    [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
    public string? FirstName { get; set; }

    [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
    public string? LastName { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    public string? Email { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Salary must be a positive value.")]
    public decimal? Salary { get; set; }

    public int? DepartmentId { get; set; }
}