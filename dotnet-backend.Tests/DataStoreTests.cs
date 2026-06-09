using DotnetBackend.Data;

namespace dotnet_backend.Tests;

public class DataStoreTests
{
    private DataStore CreateStore() => new();

    // --- GetUsers ---

    [Fact]
    public void GetUsers_ReturnsSeededUsers()
    {
        var store = CreateStore();
        var users = store.GetUsers();

        Assert.Equal(3, users.Count);
        Assert.Contains(users, u => u.Name == "John Doe");
        Assert.Contains(users, u => u.Name == "Jane Smith");
        Assert.Contains(users, u => u.Name == "Bob Johnson");
    }

    [Fact]
    public void GetUsers_ReturnsCopy_NotReference()
    {
        var store = CreateStore();
        var users1 = store.GetUsers();
        var users2 = store.GetUsers();

        Assert.NotSame(users1, users2);
    }

    // --- GetUserById ---

    [Fact]
    public void GetUserById_ExistingId_ReturnsUser()
    {
        var store = CreateStore();
        var user = store.GetUserById(1);

        Assert.NotNull(user);
        Assert.Equal("John Doe", user.Name);
        Assert.Equal("john@example.com", user.Email);
    }

    [Fact]
    public void GetUserById_NonExistingId_ReturnsNull()
    {
        var store = CreateStore();
        var user = store.GetUserById(999);

        Assert.Null(user);
    }

    [Fact]
    public void GetUserById_ZeroId_ReturnsNull()
    {
        var store = CreateStore();
        Assert.Null(store.GetUserById(0));
    }

    [Fact]
    public void GetUserById_NegativeId_ReturnsNull()
    {
        var store = CreateStore();
        Assert.Null(store.GetUserById(-1));
    }

    // --- AddUser ---

    [Fact]
    public void AddUser_AssignsUniqueId()
    {
        var store = CreateStore();
        var user = store.AddUser("Test User", "test@example.com", "tester");

        Assert.Equal(4, user.Id);
        Assert.Equal("Test User", user.Name);
        Assert.Equal("test@example.com", user.Email);
        Assert.Equal("tester", user.Role);
    }

    [Fact]
    public void AddUser_AppearsInGetUsers()
    {
        var store = CreateStore();
        store.AddUser("New User", "new@example.com", "admin");

        var users = store.GetUsers();
        Assert.Equal(4, users.Count);
        Assert.Contains(users, u => u.Email == "new@example.com");
    }

    [Fact]
    public void AddUser_MultipleAdds_IncrementIds()
    {
        var store = CreateStore();
        var u1 = store.AddUser("A", "a@test.com", "r");
        var u2 = store.AddUser("B", "b@test.com", "r");

        Assert.Equal(u1.Id + 1, u2.Id);
    }

    // --- EmailExists ---

    [Fact]
    public void EmailExists_ExistingEmail_ReturnsTrue()
    {
        var store = CreateStore();
        Assert.True(store.EmailExists("john@example.com"));
    }

    [Fact]
    public void EmailExists_CaseInsensitive()
    {
        var store = CreateStore();
        Assert.True(store.EmailExists("JOHN@EXAMPLE.COM"));
        Assert.True(store.EmailExists("John@Example.Com"));
    }

    [Fact]
    public void EmailExists_NonExistingEmail_ReturnsFalse()
    {
        var store = CreateStore();
        Assert.False(store.EmailExists("nobody@example.com"));
    }

    [Fact]
    public void EmailExists_EmptyString_ReturnsFalse()
    {
        var store = CreateStore();
        Assert.False(store.EmailExists(""));
    }

    // --- UserExists ---

    [Fact]
    public void UserExists_ExistingId_ReturnsTrue()
    {
        var store = CreateStore();
        Assert.True(store.UserExists(1));
        Assert.True(store.UserExists(2));
        Assert.True(store.UserExists(3));
    }

    [Fact]
    public void UserExists_NonExistingId_ReturnsFalse()
    {
        var store = CreateStore();
        Assert.False(store.UserExists(999));
    }

    [Fact]
    public void UserExists_NewlyAdded_ReturnsTrue()
    {
        var store = CreateStore();
        var user = store.AddUser("X", "x@test.com", "r");
        Assert.True(store.UserExists(user.Id));
    }

    // --- GetTasks ---

    [Fact]
    public void GetTasks_NoFilters_ReturnsAll()
    {
        var store = CreateStore();
        var tasks = store.GetTasks(null, null);

        Assert.Equal(3, tasks.Count);
    }

    [Fact]
    public void GetTasks_FilterByStatus_ReturnsMatching()
    {
        var store = CreateStore();
        var tasks = store.GetTasks("pending", null);

        Assert.Single(tasks);
        Assert.Equal("pending", tasks[0].Status);
    }

    [Fact]
    public void GetTasks_FilterByUserId_ReturnsMatching()
    {
        var store = CreateStore();
        var tasks = store.GetTasks(null, "2");

        Assert.Single(tasks);
        Assert.Equal(2, tasks[0].UserId);
    }

    [Fact]
    public void GetTasks_FilterByBoth_ReturnsMatching()
    {
        var store = CreateStore();
        var tasks = store.GetTasks("pending", "1");

        Assert.Single(tasks);
        Assert.Equal("pending", tasks[0].Status);
        Assert.Equal(1, tasks[0].UserId);
    }

    [Fact]
    public void GetTasks_NoMatch_ReturnsEmpty()
    {
        var store = CreateStore();
        var tasks = store.GetTasks("pending", "2");

        Assert.Empty(tasks);
    }

    [Fact]
    public void GetTasks_InvalidUserId_IgnoresFilter()
    {
        var store = CreateStore();
        var tasks = store.GetTasks(null, "notanumber");

        Assert.Equal(3, tasks.Count);
    }

    [Fact]
    public void GetTasks_EmptyStatus_IgnoresFilter()
    {
        var store = CreateStore();
        var tasks = store.GetTasks("", null);

        Assert.Equal(3, tasks.Count);
    }

    // --- AddTask ---

    [Fact]
    public void AddTask_AssignsUniqueId()
    {
        var store = CreateStore();
        var task = store.AddTask("New Task", "pending", 1);

        Assert.Equal(4, task.Id);
        Assert.Equal("New Task", task.Title);
        Assert.Equal("pending", task.Status);
        Assert.Equal(1, task.UserId);
    }

    [Fact]
    public void AddTask_AppearsInGetTasks()
    {
        var store = CreateStore();
        store.AddTask("Added Task", "completed", 2);

        var tasks = store.GetTasks(null, null);
        Assert.Equal(4, tasks.Count);
        Assert.Contains(tasks, t => t.Title == "Added Task");
    }

    // --- GetTaskById ---

    [Fact]
    public void GetTaskById_ExistingId_ReturnsTask()
    {
        var store = CreateStore();
        var task = store.GetTaskById(1);

        Assert.NotNull(task);
        Assert.Equal("Implement authentication", task.Title);
    }

    [Fact]
    public void GetTaskById_NonExistingId_ReturnsNull()
    {
        var store = CreateStore();
        Assert.Null(store.GetTaskById(999));
    }

    // --- UpdateTask ---

    [Fact]
    public void UpdateTask_AllFields_UpdatesAll()
    {
        var store = CreateStore();
        var updated = store.UpdateTask(1, "Updated Title", "completed", 3);

        Assert.NotNull(updated);
        Assert.Equal("Updated Title", updated.Title);
        Assert.Equal("completed", updated.Status);
        Assert.Equal(3, updated.UserId);
    }

    [Fact]
    public void UpdateTask_PartialUpdate_TitleOnly()
    {
        var store = CreateStore();
        var original = store.GetTaskById(1)!;
        var originalStatus = original.Status;
        var originalUserId = original.UserId;

        var updated = store.UpdateTask(1, "New Title", null, null);

        Assert.NotNull(updated);
        Assert.Equal("New Title", updated.Title);
        Assert.Equal(originalStatus, updated.Status);
        Assert.Equal(originalUserId, updated.UserId);
    }

    [Fact]
    public void UpdateTask_PartialUpdate_StatusOnly()
    {
        var store = CreateStore();
        var original = store.GetTaskById(2)!;

        var updated = store.UpdateTask(2, null, "completed", null);

        Assert.NotNull(updated);
        Assert.Equal(original.Title, updated.Title);
        Assert.Equal("completed", updated.Status);
    }

    [Fact]
    public void UpdateTask_PartialUpdate_UserIdOnly()
    {
        var store = CreateStore();
        var original = store.GetTaskById(1)!;

        var updated = store.UpdateTask(1, null, null, 3);

        Assert.NotNull(updated);
        Assert.Equal(original.Title, updated.Title);
        Assert.Equal(original.Status, updated.Status);
        Assert.Equal(3, updated.UserId);
    }

    [Fact]
    public void UpdateTask_NonExistingId_ReturnsNull()
    {
        var store = CreateStore();
        Assert.Null(store.UpdateTask(999, "X", "pending", 1));
    }

    [Fact]
    public void UpdateTask_PersistsChanges()
    {
        var store = CreateStore();
        store.UpdateTask(1, "Persisted", null, null);

        var task = store.GetTaskById(1);
        Assert.NotNull(task);
        Assert.Equal("Persisted", task.Title);
    }

    // --- GetStats ---

    [Fact]
    public void GetStats_ReturnsCorrectCounts()
    {
        var store = CreateStore();
        var stats = store.GetStats();

        Assert.Equal(3, stats.Users.Total);
        Assert.Equal(3, stats.Tasks.Total);
        Assert.Equal(1, stats.Tasks.Pending);
        Assert.Equal(1, stats.Tasks.InProgress);
        Assert.Equal(1, stats.Tasks.Completed);
    }

    [Fact]
    public void GetStats_ReflectsAddedData()
    {
        var store = CreateStore();
        store.AddUser("New", "new@test.com", "r");
        store.AddTask("Task", "pending", 1);

        var stats = store.GetStats();
        Assert.Equal(4, stats.Users.Total);
        Assert.Equal(4, stats.Tasks.Total);
        Assert.Equal(2, stats.Tasks.Pending);
    }
}
