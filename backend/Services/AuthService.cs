using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using TeamTaskManager.Api.Models;

namespace TeamTaskManager.Api.Services;

public sealed class AuthService
{
    private readonly string _connectionString;
    private const int Iterations = 210_000;

    public AuthService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("TaskManager")
            ?? throw new InvalidOperationException("The TaskManager connection string is missing.");
    }

    public async Task<AuthResponse?> RegisterAsync(CredentialsRequest request)
    {
        var userName = request.UserName.Trim();
        if (userName.Length < 3 || request.Password.Length < 8) return null;
        var salt = RandomNumberGenerator.GetBytes(32);
        var hash = Rfc2898DeriveBytes.Pbkdf2(request.Password, salt, Iterations, HashAlgorithmName.SHA256, 32);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        const string sql = """
            INSERT INTO dbo.Users (Id, UserName, PasswordHash, PasswordSalt, CreatedAt)
            VALUES (@Id, @UserName, @PasswordHash, @PasswordSalt, SYSUTCDATETIME())
            """;
        try
        {
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", Guid.NewGuid());
            command.Parameters.AddWithValue("@UserName", userName);
            command.Parameters.AddWithValue("@PasswordHash", hash);
            command.Parameters.AddWithValue("@PasswordSalt", salt);
            await command.ExecuteNonQueryAsync();
            return await CreateSessionAsync(connection, userName);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627) { return null; }
    }

    public async Task<AuthResponse?> LoginAsync(CredentialsRequest request)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "SELECT UserName, PasswordHash, PasswordSalt FROM dbo.Users WHERE LOWER(UserName) = LOWER(@UserName)", connection);
        command.Parameters.AddWithValue("@UserName", request.UserName.Trim());
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        var userName = reader.GetString(0);
        var expectedHash = (byte[])reader[1];
        var salt = (byte[])reader[2];
        await reader.CloseAsync();
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(request.Password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash)
            ? await CreateSessionAsync(connection, userName)
            : null;
    }

    public async Task<string?> GetUserNameAsync(string? authorization)
    {
        if (authorization is null || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;
        var token = authorization[7..].Trim();
        if (token.Length == 0) return null;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "SELECT UserName FROM dbo.Sessions WHERE Token = @Token AND ExpiresAt > SYSUTCDATETIME()", connection);
        command.Parameters.AddWithValue("@Token", token);
        return await command.ExecuteScalarAsync() as string;
    }

    public async Task LogoutAsync(string? authorization)
    {
        if (authorization is null || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("DELETE FROM dbo.Sessions WHERE Token = @Token", connection);
        command.Parameters.AddWithValue("@Token", authorization[7..].Trim());
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<AuthResponse> CreateSessionAsync(SqlConnection connection, string userName)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await using var command = new SqlCommand(
            "INSERT INTO dbo.Sessions (Token, UserName, ExpiresAt) VALUES (@Token, @UserName, DATEADD(day, 7, SYSUTCDATETIME()))", connection);
        command.Parameters.AddWithValue("@Token", token);
        command.Parameters.AddWithValue("@UserName", userName);
        await command.ExecuteNonQueryAsync();
        return new AuthResponse(token, userName);
    }
}
