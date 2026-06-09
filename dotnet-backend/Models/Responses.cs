using System.Text.Json.Serialization;
using DotnetBackend.Models;

namespace DotnetBackend.Models;

// ---- API response wrappers ----

public class UsersResponse
{
    [JsonPropertyName("users")]
    public List<User> Users { get; set; } = new();

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class TasksResponse
{
    [JsonPropertyName("tasks")]
    public List<TaskItem> Tasks { get; set; } = new();

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

// ---- Stats breakdown for the dashboard ----

public class UsersStats
{
    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public class TasksStats
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("pending")]
    public int Pending { get; set; }

    [JsonPropertyName("inProgress")]
    public int InProgress { get; set; }

    [JsonPropertyName("completed")]
    public int Completed { get; set; }
}

public class StatsResponse
{
    [JsonPropertyName("users")]
    public UsersStats Users { get; set; } = new();

    [JsonPropertyName("tasks")]
    public TasksStats Tasks { get; set; } = new();
}

public class HealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

// ---- Incoming request bodies ----

public class CreateUserRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }
}

public class CreateTaskRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("userId")]
    public int? UserId { get; set; }
}

// All fields nullable so clients can send only what they want to change
public class UpdateTaskRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("userId")]
    public int? UserId { get; set; }
}
