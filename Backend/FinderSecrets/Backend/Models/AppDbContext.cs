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

    [Table("found_secrets")]
    public class FoundSecret
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("request_id")]
        public int RequestId { get; set; }

        [Column("secret_type")]
        [StringLength(50)]
        public required string SecretType { get; set; }

        [Column("secret_value")]
        public required string SecretValue { get; set; }

        [Column("variable_name")]
        public required string VariableName { get; set; }

        [Column("line_number")]
        public int LineNumber { get; set; }

        [Column("position")]
        public int Position { get; set; }

        [Column("first_found_at")]
        public DateTime FirstFoundAt { get; set; }

        [Column("last_found_at")]
        public DateTime LastFoundAt { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }

    [Table("found_tokens")]
    public class FoundToken
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("history_id")]
        public int HistoryId { get; set; }

        [Column("token_type")]
        [StringLength(20)]
        public string TokenType { get; set; } = string.Empty;

        [Column("token_preview")]
        [StringLength(100)]
        public string TokenPreview { get; set; } = string.Empty;

        [Column("last_seen")]
        public DateTime LastSeen { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }

    [Table("scan_history")]
    public class ScanHistory
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("input_type")]
        [StringLength(10)]
        public string InputType { get; set; } = string.Empty;

        [Column("input_preview")]
        public string InputPreview { get; set; } = string.Empty;

        [Column("secrets_found")]
        public int SecretsFound { get; set; }

        [Column("scanned_at")]
        public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("scan_requests")]
    public class ScanRequestEntity
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public required int UserId { get; set; }

        [Column("input_type")]
        [StringLength(20)]
        public required string InputType { get; set; }

        [Column("input_data")]
        public required string InputData { get; set; }

        [Column("secrets_count")]
        public int SecretsCount { get; set; }

        [Column("scan_duration")]
        public int ScanDuration { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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