using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Interfaces;
using TodoApp.Models;
using TodoApp.ViewModels;

namespace TodoApp.Controllers
{
    [Authorize]
    public class TodoController : Controller
    {
        private readonly ITodoRepository _todoRepository;
        private readonly ILogger<TodoController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public TodoController(
            ITodoRepository todoRepository, 
            ILogger<TodoController> logger,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context)
        {
            _todoRepository = todoRepository;
            _logger = logger;
            _userManager = userManager;
            _context = context;
        }

        private string? GetCurrentUserId()
        {
            return User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public async Task<IActionResult> Index(string? filter = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    return RedirectToPage("/Account/Login", new { area = "Identity" });
                }
                
                // Apply filter if provided
                var todos = string.IsNullOrEmpty(filter) 
                    ? await _todoRepository.GetUserTodosAsync(userId)
                    : filter.ToLower() switch
                    {
                        "active" => await _todoRepository.GetUserTodosAsync(userId, false),
                        "completed" => await _todoRepository.GetUserTodosAsync(userId, true),
                        _ => await _todoRepository.GetUserTodosAsync(userId)
                    };

                // Get all user IDs from the todos
                var userIds = todos?.Where(t => t.UserId != null).Select(t => t.UserId).Distinct().ToList();
                var users = userIds?.Any() == true 
                    ? await _userManager.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u)
                    : new Dictionary<string, IdentityUser>();

                var viewModel = new TodoListViewModel
                {
                    Todos = todos?.Select(t => 
                    {
                        var user = t.UserId != null && users.TryGetValue(t.UserId, out var u) ? u : null;
                        return new TodoItemViewModel
                        {
                            Id = t.Id,
                            Title = t.Title ?? string.Empty,
                            Description = t.Description,
                            IsCompleted = t.IsCompleted,
                            CreatedAt = t.CreatedAt,
                            CompletedAt = t.CompletedAt,
                            AuthorName = user?.UserName,
                            AuthorEmail = user?.Email
                        };
                    }) ?? Enumerable.Empty<TodoItemViewModel>(),
                    ActiveCount = todos?.Count(t => !t.IsCompleted) ?? 0,
                    CompletedCount = todos?.Count(t => t.IsCompleted) ?? 0,
                    Filter = filter
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving todo list");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TodoListViewModel model)
        {
            _logger.LogInformation("Create action called with model: {Model}", model);
            
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                var errorMessage = "Validation failed: " + string.Join("; ", errors);
                _logger.LogWarning("Model state is invalid. Errors: {Errors}", string.Join("; ", errors));
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = errorMessage });
                }
                
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "User not authenticated. Please log in again." });
                    }
                    
                    return RedirectToPage("/Account/Login", new { area = "Identity" });
                }

                try
                {
                    var todo = new TodoItem
                    {
                        Title = model.NewTodoTitle ?? string.Empty,
                        Description = model.NewTodoDescription,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        IsCompleted = false
                    };

                    _logger.LogInformation("Creating new todo item: {Todo}", todo);
                    await _todoRepository.AddTodoAsync(todo);
                    var saveResult = await _todoRepository.SaveChangesAsync();
                    _logger.LogInformation("Save changes result: {SaveResult}", saveResult);

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = "Todo added successfully!" });
                    }
                    
                    TempData["SuccessMessage"] = "Todo item added successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating todo item");
                    
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "An error occurred while creating the todo item." });
                    }
                    
                    TempData["ErrorMessage"] = "An error occurred while creating the todo item.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Create action");
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "An unexpected error occurred." });
                }
                
                TempData["ErrorMessage"] = "An unexpected error occurred.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleComplete(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "User not authenticated. Please log in again." });
                    }
                    
                    return RedirectToPage("/Account/Login", new { area = "Identity" });
                }

                var todo = await _todoRepository.GetTodoByIdAsync(id, userId);
                if (todo == null)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "Todo item not found." });
                    }
                    return NotFound();
                }

                todo.IsCompleted = !todo.IsCompleted;
                todo.CompletedAt = todo.IsCompleted ? DateTime.UtcNow : null;

                await _todoRepository.UpdateTodoAsync(todo);
                await _todoRepository.SaveChangesAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var action = todo.IsCompleted ? "completed" : "marked as incomplete";
                    return Json(new { 
                        success = true, 
                        message = $"Todo '{todo.Title}' {action} successfully!",
                        isCompleted = todo.IsCompleted
                    });
                }

                TempData["SuccessMessage"] = $"Todo '{todo.Title}' {(todo.IsCompleted ? "completed" : "marked as incomplete")} successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling completion for todo item {TodoId}", id);
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "An error occurred while updating the todo item." });
                }
                
                TempData["ErrorMessage"] = "An error occurred while updating the todo item.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    return RedirectToPage("/Account/Login", new { area = "Identity" });
                }

                await _todoRepository.DeleteTodoAsync(id, userId);
                await _todoRepository.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Todo item deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting todo item {TodoId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the todo item.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCompleted()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    return RedirectToPage("/Account/Login", new { area = "Identity" });
                }

                var completedTodos = await _todoRepository.GetUserTodosAsync(userId, true);
                var completedTodosList = completedTodos?.ToList() ?? new List<TodoItem>();
                
                foreach (var todo in completedTodosList)
                {
                    await _todoRepository.DeleteTodoAsync(todo.Id, userId);
                }
                
                await _todoRepository.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Cleared {completedTodosList.Count} completed todo(s).";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing completed todos");
                TempData["ErrorMessage"] = "An error occurred while clearing completed todos.";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task<TodoListViewModel> GetTodoListViewModel(string? filter = null)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found in claims");
                return new TodoListViewModel
                {
                    Todos = Enumerable.Empty<TodoItemViewModel>(),
                    ActiveCount = 0,
                    CompletedCount = 0,
                    Filter = filter
                };
            }

            var todos = await _todoRepository.GetUserTodosAsync(userId);
            
            // Get all user IDs from the todos
            var userIds = todos?.Where(t => t.UserId != null).Select(t => t.UserId).Distinct().ToList();
            var users = userIds?.Any() == true 
                ? await _userManager.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u)
                : new Dictionary<string, IdentityUser>();

            return new TodoListViewModel
            {
                Todos = todos?.Select(t => 
                {
                    var user = t.UserId != null && users.TryGetValue(t.UserId, out var u) ? u : null;
                    return new TodoItemViewModel
                    {
                        Id = t.Id,
                        Title = t.Title ?? string.Empty,
                        Description = t.Description,
                        IsCompleted = t.IsCompleted,
                        CreatedAt = t.CreatedAt,
                        CompletedAt = t.CompletedAt,
                        AuthorName = user?.UserName,
                        AuthorEmail = user?.Email
                    };
                }) ?? Enumerable.Empty<TodoItemViewModel>(),
                ActiveCount = todos?.Count(t => !t.IsCompleted) ?? 0,
                CompletedCount = todos?.Count(t => t.IsCompleted) ?? 0,
                Filter = filter
            };
        }
    }
}
