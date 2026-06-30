# Department & Employee Management API

ASP.NET Core Web API using raw ADO.NET (`Microsoft.Data.SqlClient`) against the
stored procedures defined in `DepartmentEmployee_DB_Design.sql`. No Entity
Framework, no ORM — every data access call is an explicit `SqlConnection` /
`SqlCommand` / `SqlDataReader`.

## Prerequisites

- .NET 8 SDK
- SQL Server 2014 or later, reachable from your machine
- The database script from earlier in this conversation, run first

## Setup

1. **Create the database.** Run `DepartmentEmployee_DB_Design.sql` against your
   SQL Server instance. This creates the `Departments` and `Employees` tables,
   all stored procedures, and the reporting views.

2. **Point the API at your database.** Edit `appsettings.json`:

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=EmployeeManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```

   Swap in SQL auth (`User Id=...;Password=...;`) instead of `Trusted_Connection`
   if that's how your instance is configured.

3. **Restore and run:**

   ```bash
   cd DepartmentEmployeeAPI
   dotnet restore
   dotnet run
   ```

   The API starts on `http://localhost:5236` (see `Properties/launchSettings.json`)
   and opens Swagger at `/swagger` automatically in Development.

4. **Update CORS origins** in `appsettings.json` (`AllowedOrigins`) to match
   wherever the React dev server actually runs — defaults are `5173` (Vite) and
   `3000` (Create React App).

## Project structure

```
Controllers/    DepartmentsController, EmployeesController — thin, HTTP-only
DTOs/           Request models with DataAnnotations + IValidatableObject validation
Models/         Plain POCOs returned to the client (no ORM attributes)
Data/           IDbConnectionFactory / SqlConnectionFactory — the only place
                the connection string is read
Repositories/   ADO.NET data access — SqlCommand calls into the stored
                procedures, manual SqlDataReader → POCO mapping
Exceptions/     BusinessRuleException — wraps RAISERROR messages from the DB
Middleware/     ExceptionHandlingMiddleware — global handler, turns
                BusinessRuleException into 400s, everything else into 500
```

## Endpoints

| Method | Route                          | Notes                                  |
|--------|---------------------------------|-----------------------------------------|
| GET    | `/api/departments`              | All active departments                  |
| GET    | `/api/departments/{id}`         | 404 if not found                        |
| POST   | `/api/departments`               | 201 + Location header on success        |
| PUT    | `/api/departments/{id}`         | 204 on success                          |
| DELETE | `/api/departments/{id}`         | Soft delete; 400 if employees remain    |
| GET    | `/api/employees`                | All active employees                    |
| GET    | `/api/employees?departmentId=3` | Filtered to one department              |
| GET    | `/api/employees/{id}`           | 404 if not found                        |
| POST   | `/api/employees`                 | 201 + Location header on success        |
| PUT    | `/api/employees/{id}`           | 204 on success                          |
| DELETE | `/api/employees/{id}`           | Soft delete                             |

## Validation

Each `Create`/`Update` DTO validates the mandatory fields (required, max
length, valid email format) via DataAnnotations, plus a custom
`IValidatableObject` check on `EmployeeCreateDto`/`EmployeeUpdateDto` that
rejects a future date of birth or an age under 18 — this mirrors the
`CK_Employees_DateOfBirth` check constraint in the database, so the user sees
the error immediately rather than after a round trip. `[ApiController]`
returns these as a 400 automatically before the action method even runs.

Database-level rules (duplicate department code, duplicate email, deleting a
department that still has active employees) are enforced by the stored
procedures via `RAISERROR`. The repository layer catches that as a
`SqlException` and rethrows it as `BusinessRuleException`, which the global
middleware turns into a 400 with the original message — so both layers of
validation end up looking the same to the frontend.

## Note on this sandbox

This project was generated as source files only — `dotnet restore`/`build`
wasn't run here because this sandbox has no network access (NuGet needs to
reach nuget.org) and no .NET SDK installed. Run the restore/build/run steps
above on your own machine to verify it compiles.
