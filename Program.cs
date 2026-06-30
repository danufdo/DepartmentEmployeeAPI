using DepartmentEmployeeAPI.Data;
using DepartmentEmployeeAPI.Middleware;
using DepartmentEmployeeAPI.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Controllers — [ApiController] auto-validates DataAnnotations / IValidatableObject
// on the DTOs and returns a 400 with the validation errors before the action runs.
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ADO.NET wiring: one connection factory, scoped repositories (no DbContext,
// no ORM — every repository method opens/closes its own SqlConnection).
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();

// CORS for the React frontend (Vite default :5173, CRA default :3000 — adjust
// AllowedOrigins in appsettings.json to match whatever you actually run).
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Global exception handler first, so it can catch anything below it.
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseCors("ReactApp");
app.UseAuthorization();
app.MapControllers();

app.Run();
