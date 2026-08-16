using TeamTaskManager.Api.Models;

namespace TeamTaskManager.Api.Repositories;

public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> GetAllAsync();
    Task<TaskItem> AddAsync(CreateTaskRequest request, string userName);
    Task<TaskItem?> StartAsync(Guid id, string userName);
    Task<TaskItem?> CompleteAsync(Guid id, string userName);
}
