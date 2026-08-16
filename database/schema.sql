IF DB_ID(N'TeamTaskManager') IS NULL
BEGIN
    CREATE DATABASE TeamTaskManager;
END;
GO

USE TeamTaskManager;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        UserName NVARCHAR(100) NOT NULL,
        PasswordHash VARBINARY(32) NOT NULL,
        PasswordSalt VARBINARY(32) NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        CONSTRAINT UQ_Users_UserName UNIQUE (UserName)
    );
END;
GO

IF OBJECT_ID(N'dbo.Sessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sessions
    (
        Token CHAR(64) NOT NULL PRIMARY KEY,
        UserName NVARCHAR(100) NOT NULL,
        ExpiresAt DATETIME2 NOT NULL
    );
    CREATE INDEX IX_Sessions_ExpiresAt ON dbo.Sessions(ExpiresAt);
END;
GO

IF OBJECT_ID(N'dbo.Tasks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tasks
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        Description NVARCHAR(2000) NOT NULL CONSTRAINT DF_Tasks_Description DEFAULT '',
        DueDate DATE NULL,
        Status VARCHAR(20) NOT NULL CONSTRAINT DF_Tasks_Status DEFAULT 'open',
        CreatedBy NVARCHAR(100) NOT NULL,
        AssignedTo NVARCHAR(100) NULL,
        CompletedBy NVARCHAR(100) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Tasks_CreatedAt DEFAULT SYSUTCDATETIME(),
        StartedAt DATETIME2 NULL,
        CompletedAt DATETIME2 NULL,
        CONSTRAINT CK_Tasks_Status CHECK (Status IN ('open', 'in-progress', 'completed'))
    );
    CREATE INDEX IX_Tasks_Status_CreatedAt ON dbo.Tasks(Status, CreatedAt DESC);
END;
GO
