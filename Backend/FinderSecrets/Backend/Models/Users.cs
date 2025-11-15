using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    [Table("users")]
    public class Users
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("username")]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Column("email")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}