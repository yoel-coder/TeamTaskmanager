using TeamTaskManager.Api.Models;

namespace TeamTaskManager.Api.Repositories;

public interface IAuthRepository
{
    Task<UserAccount?> FindUserAsync(string userName);
    Task<bool> UserExistsAsync(string userName);
    Task AddUserAsync(UserAccount user);
    Task AddSessionAsync(UserSession session);
    Task<string?> FindSessionUserAsync(string token);
    Task DeleteSessionAsync(string token);
}
