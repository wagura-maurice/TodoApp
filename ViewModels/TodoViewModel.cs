using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TodoApp.ViewModels
{
    public class TodoViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot be longer than 200 characters")]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000, ErrorMessage = "Description cannot be longer than 1000 characters")]
        public string? Description { get; set; }
        
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        // For displaying in views
        public string StatusClass => IsCompleted ? "completed" : "";
        public string StatusIcon => IsCompleted ? "bi-check-circle-fill text-success" : "bi-circle";
        public string CreatedAtFormatted => CreatedAt.ToString("g");
        public string? CompletedAtFormatted => CompletedAt?.ToString("g");
        
        // Author information
        public string? AuthorName { get; set; }
        public string? AuthorEmail { get; set; }
        public string AuthorInitials => !string.IsNullOrEmpty(AuthorName) && AuthorName.Contains(' ') 
            ? $"{AuthorName.Split(' ')[0][0]}{AuthorName.Split(' ')[1][0]}" 
            : !string.IsNullOrEmpty(AuthorName) && AuthorName.Length > 0 ? AuthorName[0].ToString() : "?";
    }

    public class TodoItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? AuthorName { get; set; }
        public string? AuthorEmail { get; set; }
        
        // Computed properties
        public string StatusClass => IsCompleted ? "completed" : "";
        public string StatusIcon => IsCompleted ? "bi-check-circle-fill text-success" : "bi-circle";
        public string CreatedAtFormatted => CreatedAt.ToString("g");
        public string? CompletedAtFormatted => CompletedAt?.ToString("g");
        public string AuthorInitials => !string.IsNullOrEmpty(AuthorName) && AuthorName.Contains(' ') 
            ? $"{AuthorName.Split(' ')[0][0]}{AuthorName.Split(' ')[1][0]}" 
            : !string.IsNullOrEmpty(AuthorName) && AuthorName.Length > 0 ? AuthorName[0].ToString() : "?";
    }

    public class TodoListViewModel
    {
        public IEnumerable<TodoItemViewModel> Todos { get; set; } = new List<TodoItemViewModel>();
        public int ActiveCount { get; set; }
        public int CompletedCount { get; set; }
        public string? Filter { get; set; }
        
        [Display(Name = "Title")]
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot be longer than 200 characters")]
        public string? NewTodoTitle { get; set; }
        
        [Display(Name = "Description")]
        [StringLength(1000, ErrorMessage = "Description cannot be longer than 1000 characters")]
        public string? NewTodoDescription { get; set; }
    }
}
