namespace DepartmentEmployeeAPI.Exceptions;

/// <summary>
/// Thrown when a stored procedure raises a business-rule error via RAISERROR
/// (duplicate department code, duplicate email, department still has active
/// employees, etc). The repositories catch the resulting SqlException and
/// rethrow it as this type so the global exception middleware can turn it
/// into a 400 Bad Request with the original message intact, instead of a
/// generic 500.
/// </summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}
