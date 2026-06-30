using System.Data;
using DepartmentEmployeeAPI.Data;
using DepartmentEmployeeAPI.Exceptions;
using DepartmentEmployeeAPI.Models;
using Microsoft.Data.SqlClient;

namespace DepartmentEmployeeAPI.Repositories;

/// <summary>
/// Pure ADO.NET data access — no ORM, no LINQ-to-SQL. Every method opens a
/// SqlConnection, calls a stored procedure from DepartmentEmployee_DB_Design.sql
/// with typed SqlParameters, and maps the SqlDataReader rows to POCOs by hand.
/// </summary>
public class DepartmentRepository : IDepartmentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DepartmentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Department>> GetAllAsync()
    {
        var departments = new List<Department>();

        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand("dbo.usp_Department_GetAll", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            departments.Add(MapDepartment(reader));
        }

        return departments;
    }

    public async Task<Department?> GetByIdAsync(int departmentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand("dbo.usp_Department_GetById", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add(new SqlParameter("@DepartmentId", SqlDbType.Int) { Value = departmentId });

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapDepartment(reader) : null;
    }

    public async Task<int> InsertAsync(Department department)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand("dbo.usp_Department_Insert", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@DepartmentCode", SqlDbType.VarChar, 20) { Value = department.DepartmentCode });
        command.Parameters.Add(new SqlParameter("@DepartmentName", SqlDbType.VarChar, 100) { Value = department.DepartmentName });
        command.Parameters.Add(new SqlParameter("@Description", SqlDbType.VarChar, 500) { Value = (object?)department.Description ?? DBNull.Value });

        var newIdParam = new SqlParameter("@NewDepartmentId", SqlDbType.Int) { Direction = ParameterDirection.Output };
        command.Parameters.Add(newIdParam);

        await connection.OpenAsync();
        await ExecuteGuardedAsync(command);

        return (int)newIdParam.Value;
    }

    public async Task UpdateAsync(Department department)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand("dbo.usp_Department_Update", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@DepartmentId", SqlDbType.Int) { Value = department.DepartmentId });
        command.Parameters.Add(new SqlParameter("@DepartmentCode", SqlDbType.VarChar, 20) { Value = department.DepartmentCode });
        command.Parameters.Add(new SqlParameter("@DepartmentName", SqlDbType.VarChar, 100) { Value = department.DepartmentName });
        command.Parameters.Add(new SqlParameter("@Description", SqlDbType.VarChar, 500) { Value = (object?)department.Description ?? DBNull.Value });

        await connection.OpenAsync();
        await ExecuteGuardedAsync(command);
    }

    public async Task DeleteAsync(int departmentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand("dbo.usp_Department_Delete", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add(new SqlParameter("@DepartmentId", SqlDbType.Int) { Value = departmentId });

        await connection.OpenAsync();
        await ExecuteGuardedAsync(command);
    }

    // RAISERROR severity 16 from the stored procedures (duplicate code, employees
    // still assigned, etc.) surfaces here as a SqlException — translate it into a
    // BusinessRuleException so the controller/middleware can return a clean 400
    // instead of an unhandled 500.
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

    private static Department MapDepartment(SqlDataReader reader)
    {
        return new Department
        {
            DepartmentId = reader.GetInt32(reader.GetOrdinal("DepartmentId")),
            DepartmentCode = reader.GetString(reader.GetOrdinal("DepartmentCode")),
            DepartmentName = reader.GetString(reader.GetOrdinal("DepartmentName")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                ? null
                : reader.GetString(reader.GetOrdinal("Description")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
            ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("ModifiedDate"))
        };
    }
}
