using System.Data;
using DepartmentEmployeeAPI.Data;
using DepartmentEmployeeAPI.Exceptions;
using DepartmentEmployeeAPI.Models;
using Microsoft.Data.SqlClient;

namespace DepartmentEmployeeAPI.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EmployeeRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Employee>> GetAllAsync(int? departmentId = null)
    {
        var employees = new List<Employee>();

        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand("dbo.usp_Employee_GetAll", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add(new SqlParameter("@DepartmentId", SqlDbType.Int)
        {
            Value = (object?)departmentId ?? DBNull.Value
        });

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            employees.Add(MapEmployee(reader));
        }

        return employees;
    }

    public async Task<Employee?> GetByIdAsync(int employeeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand("dbo.usp_Employee_GetById", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add(new SqlParameter("@EmployeeId", SqlDbType.Int) { Value = employeeId });

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapEmployee(reader) : null;
    }

    public async Task<int> InsertAsync(Employee employee)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand("dbo.usp_Employee_Insert", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@FirstName", SqlDbType.VarChar, 50) { Value = employee.FirstName });
        command.Parameters.Add(new SqlParameter("@LastName", SqlDbType.VarChar, 50) { Value = employee.LastName });
        command.Parameters.Add(new SqlParameter("@Email", SqlDbType.VarChar, 150) { Value = employee.Email });
        command.Parameters.Add(new SqlParameter("@DateOfBirth", SqlDbType.Date) { Value = employee.DateOfBirth });
        command.Parameters.Add(new SqlParameter("@Salary", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = employee.Salary });
        command.Parameters.Add(new SqlParameter("@DepartmentId", SqlDbType.Int) { Value = employee.DepartmentId });

        var newIdParam = new SqlParameter("@NewEmployeeId", SqlDbType.Int) { Direction = ParameterDirection.Output };
        command.Parameters.Add(newIdParam);

        await connection.OpenAsync();
        await ExecuteGuardedAsync(command);

        return (int)newIdParam.Value;
    }

    public async Task UpdateAsync(Employee employee)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand("dbo.usp_Employee_Update", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@EmployeeId", SqlDbType.Int) { Value = employee.EmployeeId });
        command.Parameters.Add(new SqlParameter("@FirstName", SqlDbType.VarChar, 50) { Value = employee.FirstName });
        command.Parameters.Add(new SqlParameter("@LastName", SqlDbType.VarChar, 50) { Value = employee.LastName });
        command.Parameters.Add(new SqlParameter("@Email", SqlDbType.VarChar, 150) { Value = employee.Email });
        command.Parameters.Add(new SqlParameter("@DateOfBirth", SqlDbType.Date) { Value = employee.DateOfBirth });
        command.Parameters.Add(new SqlParameter("@Salary", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = employee.Salary });
        command.Parameters.Add(new SqlParameter("@DepartmentId", SqlDbType.Int) { Value = employee.DepartmentId });

        await connection.OpenAsync();
        await ExecuteGuardedAsync(command);
    }

    public async Task DeleteAsync(int employeeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand("dbo.usp_Employee_Delete", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add(new SqlParameter("@EmployeeId", SqlDbType.Int) { Value = employeeId });

        await connection.OpenAsync();
        await ExecuteGuardedAsync(command);
    }

    private static async Task ExecuteGuardedAsync(SqlCommand command)
    {
        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (SqlException ex) when (ex.Class == 16)
        {
            throw new BusinessRuleException(ex.Message);
        }
    }

    private static Employee MapEmployee(SqlDataReader reader)
    {
        return new Employee
        {
            EmployeeId = reader.GetInt32(reader.GetOrdinal("EmployeeId")),
            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
            LastName = reader.GetString(reader.GetOrdinal("LastName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            DateOfBirth = reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
            Salary = reader.GetDecimal(reader.GetOrdinal("Salary")),
            DepartmentId = reader.GetInt32(reader.GetOrdinal("DepartmentId")),
            DepartmentName = reader.GetString(reader.GetOrdinal("DepartmentName")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
            ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("ModifiedDate"))
        };
    }
}
