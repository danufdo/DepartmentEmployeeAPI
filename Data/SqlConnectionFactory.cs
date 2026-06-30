using Microsoft.Data.SqlClient;

namespace DepartmentEmployeeAPI.Data;

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found in configuration.");
    }

    public SqlConnection CreateConnection() => new(_connectionString);
}
