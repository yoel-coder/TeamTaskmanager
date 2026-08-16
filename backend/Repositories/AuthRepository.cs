using Microsoft.EntityFrameworkCore;
using TeamTaskManager.Api.Data;
using TeamTaskManager.Api.Models;

namespace TeamTaskManager.Api.Repositories;

public sealed class AuthRepository(TaskManagerDbContext database) : IAuthRepository
{
    public Task<UserAccount?> FindUserAsync(string userName) =>
        database.Users.AsNoTracking().SingleOrDefaultAsync(user => user.UserName.ToLower() == userName.ToLower());

    public Task<bool> UserExistsAsync(string userName) =>
        database.Users.AnyAsync(user => user.UserName.ToLower() == userName.ToLower());

    public async Task AddUserAsync(UserAccount user)
    {
        database.Users.Add(user);
        await database.SaveChangesAsync();
    }

    public async Task AddSessionAsync(UserSession session)
    {
        database.Sessions.Add(session);
        await database.SaveChangesAsync();
    }

    public async Task<string?> FindSessionUserAsync(string token) =>
        await database.Sessions.AsNoTracking()
            .Where(session => session.Token == token && session.ExpiresAt > DateTime.UtcNow)
            .Select(session => session.UserName)
            .SingleOrDefaultAsync();

    public async Task DeleteSessionAsync(string token)
    {
        await database.Sessions.Where(session => session.Token == token).ExecuteDeleteAsync();
    }
}
