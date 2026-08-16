using System.Security.Cryptography;
using TeamTaskManager.Api.Models;
using TeamTaskManager.Api.Repositories;

namespace TeamTaskManager.Api.Services;

public sealed class AuthService(IAuthRepository repository)
{
    private const int Iterations = 210_000;

    public async Task<AuthResponse?> RegisterAsync(CredentialsRequest request)
    {
        var userName = request.UserName.Trim();
        if (userName.Length < 3 || request.Password.Length < 8 || await repository.UserExistsAsync(userName)) return null;

        var salt = RandomNumberGenerator.GetBytes(32);
        var user = new UserAccount
        {
            UserName = userName,
            PasswordSalt = salt,
            PasswordHash = Rfc2898DeriveBytes.Pbkdf2(request.Password, salt, Iterations, HashAlgorithmName.SHA256, 32)
        };
        await repository.AddUserAsync(user);
        return await CreateSessionAsync(userName);
    }

    public async Task<AuthResponse?> LoginAsync(CredentialsRequest request)
    {
        var user = await repository.FindUserAsync(request.UserName.Trim());
        if (user is null) return null;
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            request.Password, user.PasswordSalt, Iterations, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(user.PasswordHash, actualHash)
            ? await CreateSessionAsync(user.UserName)
            : null;
    }

    public Task<string?> GetUserNameAsync(string? authorization)
    {
        var token = GetToken(authorization);
        return token is null ? Task.FromResult<string?>(null) : repository.FindSessionUserAsync(token);
    }

    public async Task LogoutAsync(string? authorization)
    {
        var token = GetToken(authorization);
        if (token is not null) await repository.DeleteSessionAsync(token);
    }

    private async Task<AuthResponse> CreateSessionAsync(string userName)
    {
        var session = new UserSession
        {
            Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            UserName = userName,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        await repository.AddSessionAsync(session);
        return new AuthResponse(session.Token, userName);
    }

    private static string? GetToken(string? authorization) =>
        authorization is not null && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization[7..].Trim()
            : null;
}
