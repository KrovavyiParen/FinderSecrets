using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<FoundSecret> FoundSecrets { get; set; }
        public DbSet<ScanRequestEntity> ScanRequests { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<FoundToken> FoundTokens { get; set; }
        public DbSet<ScanHistory> ScanHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Настройка имен таблиц и схемы
            modelBuilder.Entity<FoundSecret>().ToTable("found_secrets", "public");
            modelBuilder.Entity<ScanRequestEntity>().ToTable("scan_requests", "public");
            modelBuilder.Entity<User>().ToTable("users", "public");
            modelBuilder.Entity<FoundToken>().ToTable("found_tokens", "public");
            modelBuilder.Entity<ScanHistory>().ToTable("scan_history", "public");
        }
    }

    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("last_login")]
        public DateTime? LastLogin { get; set; }
    }

    // Request models (separate from entity models)
    public class ScanTextRequest
    {
        [Required(ErrorMessage = "Text is required")]
        [StringLength(100000, ErrorMessage = "Text length cannot exceed 100000 characters")]
        public string Text { get; set; } = string.Empty;
    }

    public class UrlRequest
    {
        [Required(ErrorMessage = "URL is required")]
        [Url(ErrorMessage = "Invalid URL format")]
        public string Url { get; set; } = string.Empty;
    }

    public class SecretMatch
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public int Position { get; set; }
        public string FileName { get; set; } = string.Empty;
    }
}