namespace TeamTaskManager.Api.Models;

public sealed class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public string Status { get; set; } = "open";
    public string CreatedBy { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public string? CompletedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public sealed record CreateTaskRequest(string Title, string? Description, DateOnly? DueDate);
public sealed record CredentialsRequest(string UserName, string Password);
public sealed record AuthResponse(string Token, string UserName);
