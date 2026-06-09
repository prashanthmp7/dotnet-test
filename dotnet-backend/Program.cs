using System.Diagnostics;
using DotnetBackend.Data;
using DotnetBackend.Models;

var builder = WebApplication.CreateBuilder(args);

// Single instance shared across all requests (in-memory store)
builder.Services.AddSingleton<DataStore>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Allow all origins so the React frontend and Node.js proxy can talk to us
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

// Log every request: method, path, status code, and how long it took
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var method = context.Request.Method;
    var path = context.Request.Path;
    var stopwatch = Stopwatch.StartNew();

    try
    {
        await next();
        stopwatch.Stop();
        logger.LogInformation("{Method} {Path} {StatusCode} {Duration}ms",
            method, path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        logger.LogError(ex, "{Method} {Path} 500 {Duration}ms - {Error}",
            method, path, stopwatch.ElapsedMilliseconds, ex.Message);
        throw;
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use PORT env var if set, otherwise fall back to 8080
const int defaultPort = 8080;
var portEnv = Environment.GetEnvironmentVariable("PORT");
if (!int.TryParse(portEnv, out var port))
{
    port = defaultPort;
}

// ---- Health check ----

app.MapGet("/health", () =>
{
    return Results.Json(new HealthResponse
    {
        Status = "ok",
        Message = ".NET backend is running"
    });
});

// ---- User endpoints ----

app.MapGet("/api/users", (DataStore store) =>
{
    var users = store.GetUsers();
    var response = new UsersResponse
    {
        Users = users,
        Count = users.Count
    };
    return Results.Json(response);
});

app.MapPost("/api/users", (CreateUserRequest request, DataStore store) =>
{
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(request.Name))
        errors.Add("Name is required");

    // Check presence first, then format, then uniqueness
    if (string.IsNullOrWhiteSpace(request.Email))
        errors.Add("Email is required");
    else if (!System.Text.RegularExpressions.Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        errors.Add("Invalid email format");
    else if (store.EmailExists(request.Email))
        errors.Add("Email already exists");
    if (string.IsNullOrWhiteSpace(request.Role))
        errors.Add("Role is required");

    if (errors.Count > 0)
        return Results.BadRequest(new { error = string.Join("; ", errors) });

    var user = store.AddUser(request.Name!, request.Email!, request.Role!);
    return Results.Created($"/api/users/{user.Id}", user);
});

app.MapGet("/api/users/{id:int}", (int id, DataStore store) =>
{
    var user = store.GetUserById(id);
    return user is null
        ? Results.NotFound(new { error = "User not found" })
        : Results.Json(user);
});

// ---- Task endpoints ----

app.MapGet("/api/tasks", (string? status, string? userId, DataStore store) =>
{
    var tasks = store.GetTasks(status, userId);
    var response = new TasksResponse
    {
        Tasks = tasks,
        Count = tasks.Count
    };
    return Results.Json(response);
});

app.MapPost("/api/tasks", (CreateTaskRequest request, DataStore store) =>
{
    var validStatuses = new[] { "pending", "in-progress", "completed" };
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(request.Title))
        errors.Add("Title is required");
    if (string.IsNullOrWhiteSpace(request.Status))
        errors.Add("Status is required");
    else if (!validStatuses.Contains(request.Status))
        errors.Add("Status must be one of: pending, in-progress, completed");
    if (request.UserId is null)
        errors.Add("UserId is required");
    else if (!store.UserExists(request.UserId.Value))
        errors.Add("User not found");

    if (errors.Count > 0)
        return Results.BadRequest(new { error = string.Join("; ", errors) });

    var task = store.AddTask(request.Title!, request.Status!, request.UserId!.Value);
    return Results.Created($"/api/tasks/{task.Id}", task);
});

// Supports partial updates — only provided fields get changed
app.MapPut("/api/tasks/{id:int}", (int id, UpdateTaskRequest request, DataStore store) =>
{
    var validStatuses = new[] { "pending", "in-progress", "completed" };
    var errors = new List<string>();

    // Only validate fields that were actually sent
    if (request.Status is not null && !validStatuses.Contains(request.Status))
        errors.Add("Status must be one of: pending, in-progress, completed");
    if (request.UserId is not null && !store.UserExists(request.UserId.Value))
        errors.Add("User not found");

    if (errors.Count > 0)
        return Results.BadRequest(new { error = string.Join("; ", errors) });

    var task = store.UpdateTask(id, request.Title, request.Status, request.UserId);
    return task is null
        ? Results.NotFound(new { error = "Task not found" })
        : Results.Json(task);
});

// ---- Stats ----

app.MapGet("/api/stats", (DataStore store) =>
{
    var stats = store.GetStats();
    return Results.Json(stats);
});

app.Run($"http://0.0.0.0:{port}");

// Needed so the test project can access the auto-generated Program class
public partial class Program { }
