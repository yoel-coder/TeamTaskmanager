using TeamTaskManager.Api.Models;
using TeamTaskManager.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TaskStore>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration["FrontendUrl"] ?? "http://localhost:5173")
        .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

app.MapGet("/api/tasks", async (TaskStore store) => Results.Ok(await store.GetAllAsync()));
app.MapPost("/api/tasks", async (CreateTaskRequest request, TaskStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.UserName))
        return Results.BadRequest(new { message = "Title and user name are required." });
    return Results.Created("/api/tasks", await store.AddAsync(request));
});
app.MapPost("/api/tasks/{id:guid}/start", async (Guid id, UserActionRequest request, TaskStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.UserName)) return Results.BadRequest();
    var task = await store.StartAsync(id, request.UserName);
    return task is null ? Results.Conflict(new { message = "This task is already being worked on." }) : Results.Ok(task);
});
app.MapPost("/api/tasks/{id:guid}/complete", async (Guid id, UserActionRequest request, TaskStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.UserName)) return Results.BadRequest();
    var task = await store.CompleteAsync(id, request.UserName);
    return task is null ? Results.Conflict(new { message = "Only the assigned user can complete this task." }) : Results.Ok(task);
});

app.Run();
