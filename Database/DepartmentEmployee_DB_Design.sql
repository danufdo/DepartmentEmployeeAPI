/* ============================================================
   Department & Employee Management — SQL Server Database Script
   Compatible with SQL Server 2014+
   Run sections top to bottom on a fresh database.
   ============================================================ */

-- ============================================================
-- 0. DATABASE (optional — skip if you already have a target DB)
-- ============================================================
-- CREATE DATABASE EmployeeManagementDB;
-- GO
-- USE EmployeeManagementDB;
-- GO

-- ============================================================
-- 1. TABLE: Departments
-- ============================================================
IF OBJECT_ID('dbo.Departments', 'U') IS NOT NULL DROP TABLE dbo.Departments;
GO

CREATE TABLE dbo.Departments
(
    DepartmentId     INT IDENTITY(1,1) NOT NULL,
    DepartmentCode   VARCHAR(20)       NOT NULL,
    DepartmentName   VARCHAR(100)      NOT NULL,
    Description      VARCHAR(500)      NULL,
    IsActive         BIT               NOT NULL CONSTRAINT DF_Departments_IsActive DEFAULT (1),
    CreatedDate      DATETIME          NOT NULL CONSTRAINT DF_Departments_CreatedDate DEFAULT (GETDATE()),
    ModifiedDate     DATETIME          NULL,

    CONSTRAINT PK_Departments PRIMARY KEY CLUSTERED (DepartmentId),
    CONSTRAINT UQ_Departments_DepartmentCode UNIQUE (DepartmentCode)
);
GO

-- ============================================================
-- 2. TABLE: Employees
-- ============================================================
IF OBJECT_ID('dbo.Employees', 'U') IS NOT NULL DROP TABLE dbo.Employees;
GO

CREATE TABLE dbo.Employees
(
    EmployeeId       INT IDENTITY(1,1) NOT NULL,
    FirstName        VARCHAR(50)       NOT NULL,
    LastName         VARCHAR(50)       NOT NULL,
    Email            VARCHAR(150)      NOT NULL,
    DateOfBirth      DATE              NOT NULL,
    -- Age is intentionally not stored: it's derived purely from DateOfBirth and
    -- is calculated in the React UI at render time instead of in the database.
    Salary           DECIMAL(18,2)     NOT NULL,
    DepartmentId     INT               NOT NULL,
    IsActive         BIT               NOT NULL CONSTRAINT DF_Employees_IsActive DEFAULT (1),
    CreatedDate      DATETIME          NOT NULL CONSTRAINT DF_Employees_CreatedDate DEFAULT (GETDATE()),
    ModifiedDate     DATETIME          NULL,

    CONSTRAINT PK_Employees PRIMARY KEY CLUSTERED (EmployeeId),
    CONSTRAINT UQ_Employees_Email UNIQUE (Email),
    CONSTRAINT FK_Employees_Departments FOREIGN KEY (DepartmentId)
        REFERENCES dbo.Departments (DepartmentId),
    CONSTRAINT CK_Employees_Salary CHECK (Salary >= 0),
    CONSTRAINT CK_Employees_DateOfBirth CHECK (DateOfBirth <= GETDATE()),
    CONSTRAINT CK_Employees_Email_Format CHECK (Email LIKE '_%@_%._%')
);
GO

-- ============================================================
-- 3. INDEXES
-- ============================================================
-- Speeds up employee-by-department lookups (grid filtering, joins)
CREATE NONCLUSTERED INDEX IX_Employees_DepartmentId
    ON dbo.Employees (DepartmentId)
    INCLUDE (FirstName, LastName, Email, Salary);
GO

-- Speeds up active-record filtering on both tables
CREATE NONCLUSTERED INDEX IX_Departments_IsActive ON dbo.Departments (IsActive);
GO
CREATE NONCLUSTERED INDEX IX_Employees_IsActive ON dbo.Employees (IsActive);
GO

-- ============================================================
-- 4. SEED DATA (optional sample rows)
-- ============================================================
INSERT INTO dbo.Departments (DepartmentCode, DepartmentName, Description)
VALUES
    ('HR',   'Human Resources', 'Handles recruitment, onboarding, and employee relations'),
    ('FIN',  'Finance',         'Manages budgeting, payroll, and financial reporting'),
    ('ENG',  'Engineering',     'Product development and technical operations');
GO

INSERT INTO dbo.Employees (FirstName, LastName, Email, DateOfBirth, Salary, DepartmentId)
VALUES
    ('Asha', 'Perera', 'asha.perera@example.com', '1992-04-15', 75000.00, 3),
    ('Nimal', 'Silva', 'nimal.silva@example.com', '1988-11-02', 62000.00, 2);
GO

/* ============================================================
   5. STORED PROCEDURES — Departments
   ============================================================ */

-- Get all active departments
CREATE OR ALTER PROCEDURE dbo.usp_Department_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DepartmentId, DepartmentCode, DepartmentName, Description,
           IsActive, CreatedDate, ModifiedDate
    FROM dbo.Departments
    WHERE IsActive = 1
    ORDER BY DepartmentName;
END
GO

-- Get a single department by id
CREATE OR ALTER PROCEDURE dbo.usp_Department_GetById
    @DepartmentId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DepartmentId, DepartmentCode, DepartmentName, Description,
           IsActive, CreatedDate, ModifiedDate
    FROM dbo.Departments
    WHERE DepartmentId = @DepartmentId;
END
GO

-- Insert a department
CREATE OR ALTER PROCEDURE dbo.usp_Department_Insert
    @DepartmentCode VARCHAR(20),
    @DepartmentName VARCHAR(100),
    @Description    VARCHAR(500) = NULL,
    @NewDepartmentId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Departments WHERE DepartmentCode = @DepartmentCode)
    BEGIN
        RAISERROR('Department code already exists.', 16, 1);
        RETURN;
    END

    INSERT INTO dbo.Departments (DepartmentCode, DepartmentName, Description)
    VALUES (@DepartmentCode, @DepartmentName, @Description);

    SET @NewDepartmentId = SCOPE_IDENTITY();
END
GO

-- Update a department
CREATE OR ALTER PROCEDURE dbo.usp_Department_Update
    @DepartmentId   INT,
    @DepartmentCode VARCHAR(20),
    @DepartmentName VARCHAR(100),
    @Description    VARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Departments
               WHERE DepartmentCode = @DepartmentCode AND DepartmentId <> @DepartmentId)
    BEGIN
        RAISERROR('Department code already in use by another department.', 16, 1);
        RETURN;
    END

    UPDATE dbo.Departments
    SET DepartmentCode = @DepartmentCode,
        DepartmentName = @DepartmentName,
        Description    = @Description,
        ModifiedDate   = GETDATE()
    WHERE DepartmentId = @DepartmentId;
END
GO

-- Soft-delete a department (blocked if active employees still reference it)
CREATE OR ALTER PROCEDURE dbo.usp_Department_Delete
    @DepartmentId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Employees WHERE DepartmentId = @DepartmentId AND IsActive = 1)
    BEGIN
        RAISERROR('Cannot delete department: active employees are still assigned to it.', 16, 1);
        RETURN;
    END

    UPDATE dbo.Departments
    SET IsActive = 0, ModifiedDate = GETDATE()
    WHERE DepartmentId = @DepartmentId;
END
GO

/* ============================================================
   6. STORED PROCEDURES — Employees
   ============================================================ */

-- Get all active employees, with department name joined in
-- @DepartmentId is optional — pass NULL to return employees from every department
CREATE OR ALTER PROCEDURE dbo.usp_Employee_GetAll
    @DepartmentId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  e.EmployeeId, e.FirstName, e.LastName, e.Email, e.DateOfBirth,
            e.Salary, e.DepartmentId, d.DepartmentName, e.IsActive,
            e.CreatedDate, e.ModifiedDate
    FROM dbo.Employees e
    INNER JOIN dbo.Departments d ON d.DepartmentId = e.DepartmentId
    WHERE e.IsActive = 1
      AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
    ORDER BY e.LastName, e.FirstName;
END
GO

-- Get a single employee by id
CREATE OR ALTER PROCEDURE dbo.usp_Employee_GetById
    @EmployeeId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  e.EmployeeId, e.FirstName, e.LastName, e.Email, e.DateOfBirth,
            e.Salary, e.DepartmentId, d.DepartmentName, e.IsActive,
            e.CreatedDate, e.ModifiedDate
    FROM dbo.Employees e
    INNER JOIN dbo.Departments d ON d.DepartmentId = e.DepartmentId
    WHERE e.EmployeeId = @EmployeeId;
END
GO

-- Insert an employee
CREATE OR ALTER PROCEDURE dbo.usp_Employee_Insert
    @FirstName     VARCHAR(50),
    @LastName      VARCHAR(50),
    @Email         VARCHAR(150),
    @DateOfBirth   DATE,
    @Salary        DECIMAL(18,2),
    @DepartmentId  INT,
    @NewEmployeeId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Employees WHERE Email = @Email)
    BEGIN
        RAISERROR('Email address is already in use.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE DepartmentId = @DepartmentId AND IsActive = 1)
    BEGIN
        RAISERROR('Selected department does not exist or is inactive.', 16, 1);
        RETURN;
    END

    INSERT INTO dbo.Employees (FirstName, LastName, Email, DateOfBirth, Salary, DepartmentId)
    VALUES (@FirstName, @LastName, @Email, @DateOfBirth, @Salary, @DepartmentId);

    SET @NewEmployeeId = SCOPE_IDENTITY();
END
GO

-- Update an employee
CREATE OR ALTER PROCEDURE dbo.usp_Employee_Update
    @EmployeeId   INT,
    @FirstName    VARCHAR(50),
    @LastName     VARCHAR(50),
    @Email        VARCHAR(150),
    @DateOfBirth  DATE,
    @Salary       DECIMAL(18,2),
    @DepartmentId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Employees WHERE Email = @Email AND EmployeeId <> @EmployeeId)
    BEGIN
        RAISERROR('Email address is already in use by another employee.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE DepartmentId = @DepartmentId AND IsActive = 1)
    BEGIN
        RAISERROR('Selected department does not exist or is inactive.', 16, 1);
        RETURN;
    END

    UPDATE dbo.Employees
    SET FirstName    = @FirstName,
        LastName     = @LastName,
        Email        = @Email,
        DateOfBirth  = @DateOfBirth,
        Salary       = @Salary,
        DepartmentId = @DepartmentId,
        ModifiedDate = GETDATE()
    WHERE EmployeeId = @EmployeeId;
END
GO

-- Soft-delete an employee
CREATE OR ALTER PROCEDURE dbo.usp_Employee_Delete
    @EmployeeId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Employees
    SET IsActive = 0, ModifiedDate = GETDATE()
    WHERE EmployeeId = @EmployeeId;
END
GO

/* ============================================================
   7. VIEWS — read-only data retrieval
   These sit alongside the stored procedures above, not in place
   of them. The procs remain the path for CRUD calls from the API
   (validation, output params, error raising); the views are for
   straightforward SELECTs — ad hoc queries, reporting tools, or
   a lightweight GET endpoint that doesn't need proc overhead.
   ============================================================ */

-- Active departments, with a live count of active employees in each
CREATE OR ALTER VIEW dbo.vw_Departments
AS
SELECT  d.DepartmentId,
        d.DepartmentCode,
        d.DepartmentName,
        d.Description,
        d.IsActive,
        d.CreatedDate,
        d.ModifiedDate,
        (SELECT COUNT(*) FROM dbo.Employees e
         WHERE e.DepartmentId = d.DepartmentId AND e.IsActive = 1) AS EmployeeCount
FROM dbo.Departments d
WHERE d.IsActive = 1;
GO

-- Active employees, joined to their department. No Age column here —
-- DateOfBirth is returned as-is and age is calculated in the React UI.
CREATE OR ALTER VIEW dbo.vw_Employees
AS
SELECT  e.EmployeeId,
        e.FirstName,
        e.LastName,
        e.FirstName + ' ' + e.LastName AS FullName,
        e.Email,
        e.DateOfBirth,
        e.Salary,
        e.DepartmentId,
        d.DepartmentCode,
        d.DepartmentName,
        e.IsActive,
        e.CreatedDate,
        e.ModifiedDate
FROM dbo.Employees e
INNER JOIN dbo.Departments d ON d.DepartmentId = e.DepartmentId
WHERE e.IsActive = 1;
GO

-- Optional reporting view: headcount and payroll totals per department.
-- Handy for a dashboard widget; not required by the CRUD screens themselves.
CREATE OR ALTER VIEW dbo.vw_DepartmentSalarySummary
AS
SELECT  d.DepartmentId,
        d.DepartmentCode,
        d.DepartmentName,
        COUNT(e.EmployeeId)        AS EmployeeCount,
        ISNULL(SUM(e.Salary), 0)   AS TotalSalary,
        ISNULL(AVG(e.Salary), 0)   AS AverageSalary
FROM dbo.Departments d
LEFT JOIN dbo.Employees e ON e.DepartmentId = d.DepartmentId AND e.IsActive = 1
WHERE d.IsActive = 1
GROUP BY d.DepartmentId, d.DepartmentCode, d.DepartmentName;
GO

-- Sample usage:
-- SELECT * FROM dbo.vw_Departments ORDER BY DepartmentName;
-- SELECT * FROM dbo.vw_Employees WHERE DepartmentId = 3 ORDER BY LastName;
-- SELECT * FROM dbo.vw_DepartmentSalarySummary ORDER BY TotalSalary DESC;
