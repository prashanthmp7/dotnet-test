using System.Collections.Concurrent;
using DotnetBackend.Models;

namespace DotnetBackend.Data;

/// <summary>
/// In-memory data store. Thread-safe via ReaderWriterLockSlim.
/// Seeded with sample users and tasks on startup.
/// </summary>
public class DataStore
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly List<User> _users;
    private readonly List<TaskItem> _tasks;

    public DataStore()
    {
        // Seed some sample data so the API isn't empty on first run
        _users = new List<User>
        {
            new() { Id = 1, Name = "John Doe", Email = "john@example.com", Role = "developer" },
            new() { Id = 2, Name = "Jane Smith", Email = "jane@example.com", Role = "designer" },
            new() { Id = 3, Name = "Bob Johnson", Email = "bob@example.com", Role = "manager" }
        };

        _tasks = new List<TaskItem>
        {
            new() { Id = 1, Title = "Implement authentication", Status = "pending", UserId = 1 },
            new() { Id = 2, Title = "Design user interface", Status = "in-progress", UserId = 2 },
            new() { Id = 3, Title = "Review code changes", Status = "completed", UserId = 3 }
        };
    }

    public List<User> GetUsers()
    {
        _lock.EnterReadLock();
        try
        {
            // Return a copy so callers can't modify the internal list
            return _users.ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public User? GetUserById(int id)
    {
        _lock.EnterReadLock();
        try
        {
            return _users.FirstOrDefault(u => u.Id == id);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public User AddUser(string name, string email, string role)
    {
        _lock.EnterWriteLock();
        try
        {
            // Pick the next ID based on the current max
            var newId = _users.Count > 0 ? _users.Max(u => u.Id) + 1 : 1;
            var user = new User { Id = newId, Name = name, Email = email, Role = role };
            _users.Add(user);
            return user;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool EmailExists(string email)
    {
        _lock.EnterReadLock();
        try
        {
            // Case-insensitive so "John@Example.com" matches "john@example.com"
            return _users.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public bool UserExists(int id)
    {
        _lock.EnterReadLock();
        try
        {
            return _users.Any(u => u.Id == id);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public TaskItem AddTask(string title, string status, int userId)
    {
        _lock.EnterWriteLock();
        try
        {
            var newId = _tasks.Count > 0 ? _tasks.Max(t => t.Id) + 1 : 1;
            var task = new TaskItem { Id = newId, Title = title, Status = status, UserId = userId };
            _tasks.Add(task);
            return task;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public TaskItem? GetTaskById(int id)
    {
        _lock.EnterReadLock();
        try
        {
            return _tasks.FirstOrDefault(t => t.Id == id);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public TaskItem? UpdateTask(int id, string? title, string? status, int? userId)
    {
        _lock.EnterWriteLock();
        try
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task is null) return null;

            // Only overwrite fields that were actually provided
            if (title is not null) task.Title = title;
            if (status is not null) task.Status = status;
            if (userId is not null) task.UserId = userId.Value;

            return task;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public List<TaskItem> GetTasks(string? status, string? userId)
    {
        _lock.EnterReadLock();
        try
        {
            // Start with all tasks, then narrow down by any filters provided
            IEnumerable<TaskItem> query = _tasks;

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(userId) && int.TryParse(userId, out var uid))
            {
                query = query.Where(t => t.UserId == uid);
            }

            return query.ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public StatsResponse GetStats()
    {
        _lock.EnterReadLock();
        try
        {
            // Tally up task counts by status
            var stats = new StatsResponse
            {
                Users = { Total = _users.Count },
                Tasks = { Total = _tasks.Count }
            };

            foreach (var task in _tasks)
            {
                switch (task.Status)
                {
                    case "pending":
                        stats.Tasks.Pending++;
                        break;
                    case "in-progress":
                        stats.Tasks.InProgress++;
                        break;
                    case "completed":
                        stats.Tasks.Completed++;
                        break;
                }
            }

            return stats;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}
