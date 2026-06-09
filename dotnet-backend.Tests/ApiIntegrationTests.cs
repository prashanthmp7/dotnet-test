using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace dotnet_backend.Tests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // --- Health ---

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ok", body.GetProperty("status").GetString());
    }

    // --- GET /api/users ---

    [Fact]
    public async Task GetUsers_ReturnsSeededUsers()
    {
        var response = await _client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("count").GetInt32() >= 3);
        Assert.True(body.GetProperty("users").GetArrayLength() >= 3);
    }

    // --- GET /api/users/{id} ---

    [Fact]
    public async Task GetUserById_ExistingId_ReturnsUser()
    {
        var response = await _client.GetAsync("/api/users/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("John Doe", body.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetUserById_NonExistingId_Returns404()
    {
        var response = await _client.GetAsync("/api/users/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("error", out _));
    }

    // --- POST /api/users ---

    [Fact]
    public async Task CreateUser_ValidInput_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            name = "Integration User",
            email = $"integ-{Guid.NewGuid():N}@test.com",
            role = "tester"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Integration User", body.GetProperty("name").GetString());
        Assert.True(body.GetProperty("id").GetInt32() > 0);
    }

    [Fact]
    public async Task CreateUser_MissingName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            email = "test@test.com",
            role = "tester"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("Name is required", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task CreateUser_MissingEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            name = "Test",
            role = "tester"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("Email is required", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task CreateUser_InvalidEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            name = "Test",
            email = "not-an-email",
            role = "tester"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("Invalid email format", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            name = "Dupe",
            email = "john@example.com",
            role = "tester"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("Email already exists", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task CreateUser_MissingRole_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            name = "Test",
            email = $"role-{Guid.NewGuid():N}@test.com"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("Role is required", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task CreateUser_AllFieldsMissing_Returns400WithMultipleErrors()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var error = body.GetProperty("error").GetString()!;
        Assert.Contains("Name is required", error);
        Assert.Contains("Email is required", error);
        Assert.Contains("Role is required", error);
    }

    [Fact]
    public async Task CreateUser_AppearsInGetUsers()
    {
        var email = $"visible-{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/users", new
        {
            name = "Visible User",
            email,
            role = "viewer"
        });

        var response = await _client.GetAsync("/api/users");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var users = body.GetProperty("users").EnumerateArray();
        Assert.Contains(users, u => u.GetProperty("email").GetString() == email);
    }

    // --- GET /api/tasks ---

    [Fact]
    public async Task GetTasks_ReturnsSeededTasks()
    {
        var response = await _client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("count").GetInt32() >= 3);
    }

    [Fact]
    public async Task GetTasks_FilterByStatus_ReturnsFiltered()
    {
        var response = await _client.GetAsync("/api/tasks?status=pending");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        foreach (var task in body.GetProperty("tasks").EnumerateArray())
        {
            Assert.Equal("pending", task.GetProperty("status").GetString());
        }
    }

    [Fact]
    public async Task GetTasks_FilterByUserId_ReturnsFiltered()
    {
        var response = await _client.GetAsync("/api/tasks?userId=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        foreach (var task in body.GetProperty("tasks").EnumerateArray())
        {
            Assert.Equal(1, task.GetProperty("userId").GetInt32());
        }
    }

    // --- POST /api/tasks ---

    [Fact]
    public async Task CreateTask_ValidInput_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Integration Task",
            status = "pending",
            userId = 1
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Integration Task", body.GetProperty("title").GetString());
        Assert.True(body.GetProperty("id").GetInt32() > 0);
    }

    [Fact]
    public async Task CreateTask_MissingTitle_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new
        {
            status = "pending",
            userId = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("Title is required", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task CreateTask_InvalidStatus_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Task",
            status = "invalid-status",
            userId = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("Status must be one of", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task CreateTask_MissingStatus_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Task",
            userId = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("Status is required", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task CreateTask_NonExistentUser_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Task",
            status = "pending",
            userId = 999
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("User not found", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task CreateTask_MissingUserId_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Task",
            status = "pending"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("UserId is required", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task CreateTask_AllFieldsMissing_Returns400WithMultipleErrors()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var error = body.GetProperty("error").GetString()!;
        Assert.Contains("Title is required", error);
        Assert.Contains("Status is required", error);
        Assert.Contains("UserId is required", error);
    }

    [Fact]
    public async Task CreateTask_AppearsInGetTasks()
    {
        var title = $"Visible-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/tasks", new
        {
            title,
            status = "pending",
            userId = 1
        });

        var response = await _client.GetAsync("/api/tasks");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var tasks = body.GetProperty("tasks").EnumerateArray();
        Assert.Contains(tasks, t => t.GetProperty("title").GetString() == title);
    }

    // --- PUT /api/tasks/{id} ---

    [Fact]
    public async Task UpdateTask_ValidPartialUpdate_Returns200()
    {
        var response = await _client.PutAsJsonAsync("/api/tasks/1", new
        {
            title = "Updated via integration"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Updated via integration", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task UpdateTask_StatusOnly_Returns200()
    {
        var response = await _client.PutAsJsonAsync("/api/tasks/2", new
        {
            status = "completed"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("completed", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task UpdateTask_NonExistingId_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/api/tasks/999", new
        {
            title = "Nope"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("Task not found", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task UpdateTask_InvalidStatus_Returns400()
    {
        var response = await _client.PutAsJsonAsync("/api/tasks/1", new
        {
            status = "invalid"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("Status must be one of", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task UpdateTask_NonExistentUser_Returns400()
    {
        var response = await _client.PutAsJsonAsync("/api/tasks/1", new
        {
            userId = 999
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("User not found", body.GetProperty("error").GetString());
    }

    // --- GET /api/stats ---

    [Fact]
    public async Task GetStats_ReturnsValidStructure()
    {
        var response = await _client.GetAsync("/api/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var users = body.GetProperty("users");
        Assert.True(users.GetProperty("total").GetInt32() >= 3);

        var tasks = body.GetProperty("tasks");
        Assert.True(tasks.GetProperty("total").GetInt32() >= 3);
        Assert.True(tasks.TryGetProperty("pending", out _));
        Assert.True(tasks.TryGetProperty("inProgress", out _));
        Assert.True(tasks.TryGetProperty("completed", out _));
    }

    // --- Edge cases ---

    [Fact]
    public async Task NonExistentRoute_Returns404()
    {
        var response = await _client.GetAsync("/api/nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WhitespaceOnlyFields_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            name = "   ",
            email = "   ",
            role = "   "
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_AllValidStatuses_Return201()
    {
        var statuses = new[] { "pending", "in-progress", "completed" };
        foreach (var status in statuses)
        {
            var response = await _client.PostAsJsonAsync("/api/tasks", new
            {
                title = $"Task-{status}",
                status,
                userId = 1
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }

    [Fact]
    public async Task UpdateTask_EmptyBody_Returns200WithNoChanges()
    {
        var getBefore = await _client.GetAsync("/api/tasks");
        var beforeBody = await getBefore.Content.ReadFromJsonAsync<JsonElement>();
        var taskBefore = beforeBody.GetProperty("tasks").EnumerateArray()
            .First(t => t.GetProperty("id").GetInt32() == 3);

        var response = await _client.PutAsJsonAsync("/api/tasks/3", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(taskBefore.GetProperty("title").GetString(), body.GetProperty("title").GetString());
    }
}
