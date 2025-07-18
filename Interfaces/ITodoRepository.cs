using System.Collections.Generic;
using System.Threading.Tasks;
using TodoApp.Models;

namespace TodoApp.Interfaces
{
    public interface ITodoRepository
    {
        Task<IEnumerable<TodoItem>> GetUserTodosAsync(string userId, bool? isCompleted = null, int page = 1, int pageSize = 10);
        Task<int> GetUserTodosCountAsync(string userId, bool? isCompleted = null);
        Task<TodoItem?> GetTodoByIdAsync(int id, string userId);
        Task AddTodoAsync(TodoItem todo);
        Task UpdateTodoAsync(TodoItem todo);
        Task DeleteTodoAsync(int id, string userId);
        Task<bool> SaveChangesAsync();
    }
}
