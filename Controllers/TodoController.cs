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
        private readonly IWebHostEnvironment _env;

        public TodoController(
            ITodoRepository todoRepository, 
            ILogger<TodoController> logger,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context,
            IWebHostEnvironment env)
        {
            _todoRepository = todoRepository;
            _logger = logger;
            _userManager = userManager;
            _context = context;
            _env = env;
            
            // Log the TodoListViewModel properties for debugging
            var todoListType = typeof(TodoListViewModel);
            _logger.LogInformation("TodoListViewModel properties:");
            foreach (var prop in todoListType.GetProperties())
            {
                _logger.LogInformation($"- {prop.Name} : {prop.PropertyType.Name}");
            }
            
            var paginationProp = todoListType.GetProperty("Pagination");
            _logger.LogInformation("\nPagination property found: " + (paginationProp != null ? "Yes" : "No"));
            
            if (paginationProp != null)
            {
                _logger.LogInformation($"Pagination type: {paginationProp.PropertyType.FullName}");
                _logger.LogInformation($"Can read: {paginationProp.CanRead}");
                _logger.LogInformation($"Can write: {paginationProp.CanWrite}");
            }
        }

        private string? GetCurrentUserId()
        {
            return User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        [Route("/debug/todolistviewmodel")]
        [AllowAnonymous]
        public IActionResult DebugTodoListViewModel()
        {
            var model = new TodoListViewModel();
            model.Pagination = new PaginationInfo
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalItems = 100,
                TotalPages = 10
            };
            
            return Json(new 
            {
                PaginationPropertyExists = model.Pagination != null,
                PaginationType = model.Pagination?.GetType().FullName,
                Properties = model.GetType().GetProperties().Select(p => new 
                {
                    Name = p.Name,
                    Type = p.PropertyType.Name,
                    CanRead = p.CanRead,
                    CanWrite = p.CanWrite
                })
            });
        }
        
        public async Task<IActionResult> Index(string? filter = null, int page = 1, int pageSize = 5)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    return RedirectToPage("/Account/Login", new { area = "Identity" });
                }

                // Ensure page size is one of the allowed values (5, 10, 20, 50, 100)
                var allowedPageSizes = new[] { 5, 10, 20, 50, 100 };
                if (!allowedPageSizes.Contains(pageSize))
                {
                    pageSize = 5; // Default to 5 if an invalid page size is provided
                }

                // Get counts for all, active, and completed todos
                var activeCount = await _todoRepository.GetUserTodosCountAsync(userId, false);
                var completedCount = await _todoRepository.GetUserTodosCountAsync(userId, true);
                
                // Get filtered todos with pagination
                bool? isCompleted = filter?.ToLower() switch
                {
                    "active" => false,
                    "completed" => true,
                    _ => null
                };

                // Get todos for the current page
                var todos = await _todoRepository.GetUserTodosAsync(userId, isCompleted, page, pageSize);
                
                // Get total count for the current filter
                var totalItems = await _todoRepository.GetUserTodosCountAsync(userId, isCompleted);
                
                // Calculate total pages and ensure page is within valid range
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

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
                    ActiveCount = activeCount,
                    CompletedCount = completedCount,
                    Filter = filter,
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalItems = totalItems,
                        TotalPages = totalPages > 0 ? totalPages : 1
                    }
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
                        return Json(new { 
                            success = true, 
                            message = $"Todo #{todo.Id} added successfully!",
                            todoId = todo.Id
                        });
                    }
                    
                    TempData["SuccessMessage"] = $"Todo item #{todo.Id} added successfully!";
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
[Produces("application/json")]
public async Task<IActionResult> Delete(int id)
{
    // Set response type to JSON
    Response.ContentType = "application/json";
    
    try
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID not found in claims");
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Json(new { 
                success = false, 
                message = "Your session has expired. Please log in again.",
                requiresLogin = true
            });
        }

        _logger.LogInformation("Attempting to delete todo {TodoId} for user {UserId}", id, userId);
        
        var todo = await _todoRepository.GetTodoByIdAsync(id, userId);
        if (todo == null)
        {
            _logger.LogWarning("Todo item {TodoId} not found for user {UserId}", id, userId);
            Response.StatusCode = StatusCodes.Status404NotFound;
            return Json(new { 
                success = false, 
                message = "The todo item was not found or has already been deleted.",
                notFound = true
            });
        }

        await _todoRepository.DeleteTodoAsync(id, userId);
        await _todoRepository.SaveChangesAsync();
        
        _logger.LogInformation("Successfully deleted todo {TodoId} for user {UserId}", id, userId);
        
        return Json(new { 
            success = true, 
            message = $"Task #{todo.Id} deleted successfully!",
            todoId = id
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error deleting todo {TodoId}", id);
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return Json(new { 
            success = false, 
            message = "An error occurred while deleting the todo item.",
            error = _env.IsDevelopment() ? ex.Message : null
        });
    }
}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCompleted(int? page, string? filter)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    return RedirectToPage("/Account/Login", new { area = "Identity" });
                }

                // Get all completed todos (without pagination)
                var completedTodos = await _todoRepository.GetUserTodosAsync(userId, true, 1, int.MaxValue);
                var completedTodosList = completedTodos.ToList();
                var count = completedTodosList.Count;
                
                if (count > 0)
                {
                    foreach (var todo in completedTodosList)
                    {
                        await _todoRepository.DeleteTodoAsync(todo.Id, userId);
                    }
                    
                    await _todoRepository.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Cleared {count} completed todo(s).";
                }
                
                // Redirect back to the current page and filter
                return RedirectToAction(nameof(Index), new { page, filter });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing completed todos");
                TempData["ErrorMessage"] = "An error occurred while clearing completed todos.";
                return RedirectToAction(nameof(Index), new { page, filter });
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
