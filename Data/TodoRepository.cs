using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TodoApp.Interfaces;
using TodoApp.Models;

namespace TodoApp.Data
{
    public class TodoRepository : ITodoRepository
    {
        private readonly ApplicationDbContext _context;

        public TodoRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TodoItem>> GetUserTodosAsync(string userId, bool? isCompleted = null, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Enumerable.Empty<TodoItem>();
            }

            try
            {
                var query = _context.TodoItems
                    .Include(t => t.User)
                    .Where(t => t.UserId == userId);
                    
                if (isCompleted.HasValue)
                {
                    query = query.Where(t => t.IsCompleted == isCompleted.Value);
                }
                
                return await query
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Log the error (you might want to inject an ILogger in the constructor)
                Console.WriteLine($"Error retrieving todos for user {userId}: {ex.Message}");
                return Enumerable.Empty<TodoItem>();
            }
        }
        
        public async Task<int> GetUserTodosCountAsync(string userId, bool? isCompleted = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return 0;
            }

            try
            {
                var query = _context.TodoItems
                    .Where(t => t.UserId == userId);
                    
                if (isCompleted.HasValue)
                {
                    query = query.Where(t => t.IsCompleted == isCompleted.Value);
                }
                
                return await query.CountAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error counting todos for user {userId}: {ex.Message}");
                return 0;
            }
        }

        public async Task<TodoItem?> GetTodoByIdAsync(int id, string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            try
            {
                return await _context.TodoItems
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Log the error (you might want to inject an ILogger in the constructor)
                Console.WriteLine($"Error retrieving todo {id} for user {userId}: {ex.Message}");
                return null;
            }
        }

        public async Task AddTodoAsync(TodoItem todo)
        {
            if (todo == null)
            {
                throw new ArgumentNullException(nameof(todo));
            }

            await _context.TodoItems.AddAsync(todo).ConfigureAwait(false);
        }

        public async Task UpdateTodoAsync(TodoItem todo)
        {
            if (todo == null)
            {
                throw new ArgumentNullException(nameof(todo));
            }

            try
            {
                var existingTodo = await _context.TodoItems
                    .FirstOrDefaultAsync(t => t.Id == todo.Id);

                if (existingTodo == null)
                {
                    throw new InvalidOperationException($"Todo with ID {todo.Id} not found");
                }

                // Update the properties of the existing entity
                _context.Entry(existingTodo).CurrentValues.SetValues(todo);
                _context.Entry(existingTodo).State = EntityState.Modified;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating todo {todo.Id}: {ex.Message}");
                throw; // Re-throw to allow controller to handle the error
            }
        }

        public async Task DeleteTodoAsync(int id, string userId)
        {
            var todo = await GetTodoByIdAsync(id, userId);
            if (todo != null)
            {
                _context.TodoItems.Remove(todo);
            }
        }

        public async Task<bool> SaveChangesAsync()
        {
            try
            {
                var result = await _context.SaveChangesAsync().ConfigureAwait(false);
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving changes: {ex.Message}");
                return false;
            }
        }
    }
}
