using Microsoft.Data.SqlClient;
using TeamTaskManager.Api.Models;

namespace TeamTaskManager.Api.Services;

public sealed class TaskStore
{
    private readonly string _connectionString;

    public TaskStore(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("TaskManager")
            ?? throw new InvalidOperationException("The TaskManager connection string is missing.");
    }

    public async Task InitializeAsync()
    {
        var builder = new SqlConnectionStringBuilder(_connectionString);
        var databaseName = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        await using (var master = new SqlConnection(builder.ConnectionString))
        {
            await master.OpenAsync();
            var safeName = databaseName.Replace("]", "]]");
            await using var createDatabase = new SqlCommand(
                $"IF DB_ID(@databaseName) IS NULL EXEC('CREATE DATABASE [{safeName}]')", master);
            createDatabase.Parameters.AddWithValue("@databaseName", databaseName);
            await createDatabase.ExecuteNonQueryAsync();
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        const string sql = """
            IF OBJECT_ID('dbo.Users', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Users (
                    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    UserName NVARCHAR(100) NOT NULL UNIQUE,
                    PasswordHash VARBINARY(32) NOT NULL,
                    PasswordSalt VARBINARY(32) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL
                );
            END
            IF OBJECT_ID('dbo.Sessions', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Sessions (
                    Token CHAR(64) NOT NULL PRIMARY KEY,
                    UserName NVARCHAR(100) NOT NULL,
                    ExpiresAt DATETIME2 NOT NULL
                );
                CREATE INDEX IX_Sessions_ExpiresAt ON dbo.Sessions(ExpiresAt);
            END
            IF OBJECT_ID('dbo.Tasks', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Tasks (
                    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    Title NVARCHAR(200) NOT NULL,
                    Description NVARCHAR(2000) NOT NULL CONSTRAINT DF_Tasks_Description DEFAULT '',
                    DueDate DATE NULL,
                    Status VARCHAR(20) NOT NULL CONSTRAINT DF_Tasks_Status DEFAULT 'open',
                    CreatedBy NVARCHAR(100) NOT NULL,
                    AssignedTo NVARCHAR(100) NULL,
                    CompletedBy NVARCHAR(100) NULL,
                    CreatedAt DATETIME2 NOT NULL,
                    StartedAt DATETIME2 NULL,
                    CompletedAt DATETIME2 NULL,
                    CONSTRAINT CK_Tasks_Status CHECK (Status IN ('open', 'in-progress', 'completed'))
                );
            END
            """;
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<TaskItem>> GetAllAsync()
    {
        var tasks = new List<TaskItem>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT * FROM dbo.Tasks ORDER BY CreatedAt DESC", connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) tasks.Add(ReadTask(reader));
        return tasks;
    }

    public async Task<TaskItem> AddAsync(CreateTaskRequest request, string userName)
    {
        var task = new TaskItem
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            DueDate = request.DueDate,
            CreatedBy = userName
        };
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        const string sql = """
            INSERT INTO dbo.Tasks (Id, Title, Description, DueDate, Status, CreatedBy, CreatedAt)
            VALUES (@Id, @Title, @Description, @DueDate, @Status, @CreatedBy, @CreatedAt)
            """;
        await using var command = new SqlCommand(sql, connection);
        AddParameter(command, "@Id", task.Id);
        AddParameter(command, "@Title", task.Title);
        AddParameter(command, "@Description", task.Description);
        AddParameter(command, "@DueDate", task.DueDate?.ToDateTime(TimeOnly.MinValue));
        AddParameter(command, "@Status", task.Status);
        AddParameter(command, "@CreatedBy", task.CreatedBy);
        AddParameter(command, "@CreatedAt", task.CreatedAt);
        await command.ExecuteNonQueryAsync();
        return task;
    }

    public Task<TaskItem?> StartAsync(Guid id, string userName) =>
        UpdateAsync(id, userName.Trim(), "open", "in-progress", false);

    public Task<TaskItem?> CompleteAsync(Guid id, string userName) =>
        UpdateAsync(id, userName.Trim(), "in-progress", "completed", true);

    private async Task<TaskItem?> UpdateAsync(Guid id, string userName, string currentStatus, string nextStatus, bool complete)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        var sql = complete
            ? """
              UPDATE dbo.Tasks SET Status = @NextStatus, CompletedBy = @UserName, CompletedAt = SYSUTCDATETIME()
              WHERE Id = @Id AND Status = @CurrentStatus AND LOWER(AssignedTo) = LOWER(@UserName)
              """
            : """
              UPDATE dbo.Tasks SET Status = @NextStatus, AssignedTo = @UserName, StartedAt = SYSUTCDATETIME()
              WHERE Id = @Id AND Status = @CurrentStatus
              """;
        await using var command = new SqlCommand(sql, connection);
        AddParameter(command, "@Id", id);
        AddParameter(command, "@UserName", userName);
        AddParameter(command, "@CurrentStatus", currentStatus);
        AddParameter(command, "@NextStatus", nextStatus);
        if (await command.ExecuteNonQueryAsync() == 0) return null;

        await using var select = new SqlCommand("SELECT * FROM dbo.Tasks WHERE Id = @Id", connection);
        AddParameter(select, "@Id", id);
        await using var reader = await select.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadTask(reader) : null;
    }

    private static TaskItem ReadTask(SqlDataReader reader) => new()
    {
        Id = reader.GetGuid(reader.GetOrdinal("Id")),
        Title = reader.GetString(reader.GetOrdinal("Title")),
        Description = reader.GetString(reader.GetOrdinal("Description")),
        DueDate = reader.IsDBNull(reader.GetOrdinal("DueDate")) ? null : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("DueDate"))),
        Status = reader.GetString(reader.GetOrdinal("Status")),
        CreatedBy = reader.GetString(reader.GetOrdinal("CreatedBy")),
        AssignedTo = GetNullableString(reader, "AssignedTo"),
        CompletedBy = GetNullableString(reader, "CompletedBy"),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
        StartedAt = GetNullableDateTime(reader, "StartedAt"),
        CompletedAt = GetNullableDateTime(reader, "CompletedAt")
    };

    private static string? GetNullableString(SqlDataReader reader, string name) =>
        reader.IsDBNull(reader.GetOrdinal(name)) ? null : reader.GetString(reader.GetOrdinal(name));

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string name) =>
        reader.IsDBNull(reader.GetOrdinal(name)) ? null : reader.GetDateTime(reader.GetOrdinal(name));

    private static void AddParameter(SqlCommand command, string name, object? value) =>
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);
}
