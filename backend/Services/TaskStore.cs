using System.Text.Json;
using TeamTaskManager.Api.Models;

namespace TeamTaskManager.Api.Services;

public sealed class TaskStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public TaskStore(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, "tasks.json");
    }

    public async Task<IReadOnlyList<TaskItem>> GetAllAsync()
    {
        await _gate.WaitAsync();
        try { return (await ReadAsync()).OrderByDescending(t => t.CreatedAt).ToList(); }
        finally { _gate.Release(); }
    }

    public async Task<TaskItem> AddAsync(CreateTaskRequest request)
    {
        var task = new TaskItem
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            DueDate = request.DueDate,
            CreatedBy = request.UserName.Trim()
        };
        await _gate.WaitAsync();
        try { var tasks = await ReadAsync(); tasks.Add(task); await WriteAsync(tasks); return task; }
        finally { _gate.Release(); }
    }

    public Task<TaskItem?> StartAsync(Guid id, string userName) => UpdateAsync(id, task =>
    {
        if (task.Status != "open") return false;
        task.Status = "in-progress";
        task.AssignedTo = userName.Trim();
        task.StartedAt = DateTime.UtcNow;
        return true;
    });

    public Task<TaskItem?> CompleteAsync(Guid id, string userName) => UpdateAsync(id, task =>
    {
        if (task.Status != "in-progress" || !string.Equals(task.AssignedTo, userName.Trim(), StringComparison.OrdinalIgnoreCase)) return false;
        task.Status = "completed";
        task.CompletedBy = userName.Trim();
        task.CompletedAt = DateTime.UtcNow;
        return true;
    });

    private async Task<TaskItem?> UpdateAsync(Guid id, Func<TaskItem, bool> update)
    {
        await _gate.WaitAsync();
        try
        {
            var tasks = await ReadAsync();
            var task = tasks.FirstOrDefault(t => t.Id == id);
            if (task is null || !update(task)) return null;
            await WriteAsync(tasks);
            return task;
        }
        finally { _gate.Release(); }
    }

    private async Task<List<TaskItem>> ReadAsync()
    {
        if (!File.Exists(_filePath)) return [];
        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<List<TaskItem>>(stream, _jsonOptions) ?? [];
    }

    private async Task WriteAsync(List<TaskItem> tasks)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, tasks, _jsonOptions);
    }
}
