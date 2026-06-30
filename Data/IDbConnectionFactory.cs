using Microsoft.Data.SqlClient;

namespace DepartmentEmployeeAPI.Data;

public interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
}
