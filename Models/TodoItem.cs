using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace TodoApp.Models
{
    public class TodoItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "tinyint(1)")]
        public bool IsCompleted { get; set; } = false;

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime")]
        public DateTime? CompletedAt { get; set; }

        [Required]
        [StringLength(255)]
        public string UserId { get; set; } = string.Empty;
        
        [ForeignKey("UserId")]
        public virtual IdentityUser? User { get; set; }
    }
}
