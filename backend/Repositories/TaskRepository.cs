using Microsoft.EntityFrameworkCore;
using TeamTaskManager.Api.Data;
using TeamTaskManager.Api.Models;

namespace TeamTaskManager.Api.Repositories;

public sealed class TaskRepository(TaskManagerDbContext database) : ITaskRepository
{
    public async Task<IReadOnlyList<TaskItem>> GetAllAsync() =>
        await database.Tasks.AsNoTracking().OrderByDescending(task => task.CreatedAt).ToListAsync();

    public async Task<TaskItem> AddAsync(CreateTaskRequest request, string userName)
    {
        var task = new TaskItem
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            DueDate = request.DueDate,
            CreatedBy = userName
        };
        database.Tasks.Add(task);
        await database.SaveChangesAsync();
        return task;
    }

    public async Task<TaskItem?> StartAsync(Guid id, string userName)
    {
        var task = await database.Tasks.SingleOrDefaultAsync(item => item.Id == id);
        if (task is null || task.Status != "open") return null;
        task.Status = "in-progress";
        task.AssignedTo = userName;
        task.StartedAt = DateTime.UtcNow;
        await database.SaveChangesAsync();
        return task;
    }

    public async Task<TaskItem?> CompleteAsync(Guid id, string userName)
    {
        var task = await database.Tasks.SingleOrDefaultAsync(item => item.Id == id);
        if (task is null || task.Status != "in-progress" ||
            !string.Equals(task.AssignedTo, userName, StringComparison.OrdinalIgnoreCase)) return null;
        task.Status = "completed";
        task.CompletedBy = userName;
        task.CompletedAt = DateTime.UtcNow;
        await database.SaveChangesAsync();
        return task;
    }
}
