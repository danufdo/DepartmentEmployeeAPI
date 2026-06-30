namespace DepartmentEmployeeAPI.Models;

public class Employee
{
    public int EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public decimal Salary { get; set; }
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }

    // Intentionally no Age property. The database does not store it and this
    // API does not derive it — DateOfBirth goes out as-is and the React UI
    // calculates age client-side, the same place it'll be displayed.
}
