using TeamTaskManager.Api.Models;
using TeamTaskManager.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TaskStore>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration["FrontendUrl"] ?? "http://localhost:5173")
        .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
await app.Services.GetRequiredService<TaskStore>().InitializeAsync();

app.MapPost("/api/auth/register", async (CredentialsRequest request, AuthService auth) =>
{
    var result = await auth.RegisterAsync(request);
    return result is null ? Results.BadRequest(new { message = "Use a unique username (3+ characters) and password (8+ characters)." }) : Results.Ok(result);
});
app.MapPost("/api/auth/login", async (CredentialsRequest request, AuthService auth) =>
{
    var result = await auth.LoginAsync(request);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
});
app.MapPost("/api/auth/logout", async (HttpRequest http, AuthService auth) =>
{
    await auth.LogoutAsync(http.Headers.Authorization);
    return Results.NoContent();
});

app.MapGet("/api/tasks", async (HttpRequest http, TaskStore store, AuthService auth) =>
{
    var userName = await auth.GetUserNameAsync(http.Headers.Authorization);
    return userName is null ? Results.Unauthorized() : Results.Ok(await store.GetAllAsync());
});
app.MapPost("/api/tasks", async (HttpRequest http, CreateTaskRequest request, TaskStore store, AuthService auth) =>
{
    var userName = await auth.GetUserNameAsync(http.Headers.Authorization);
    if (userName is null) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(request.Title)) return Results.BadRequest(new { message = "Title is required." });
    return Results.Created("/api/tasks", await store.AddAsync(request, userName));
});
app.MapPost("/api/tasks/{id:guid}/start", async (Guid id, HttpRequest http, TaskStore store, AuthService auth) =>
{
    var userName = await auth.GetUserNameAsync(http.Headers.Authorization);
    if (userName is null) return Results.Unauthorized();
    var task = await store.StartAsync(id, userName);
    return task is null ? Results.Conflict(new { message = "This task is already being worked on." }) : Results.Ok(task);
});
app.MapPost("/api/tasks/{id:guid}/complete", async (Guid id, HttpRequest http, TaskStore store, AuthService auth) =>
{
    var userName = await auth.GetUserNameAsync(http.Headers.Authorization);
    if (userName is null) return Results.Unauthorized();
    var task = await store.CompleteAsync(id, userName);
    return task is null ? Results.Conflict(new { message = "Only the assigned user can complete this task." }) : Results.Ok(task);
});

app.Run();
