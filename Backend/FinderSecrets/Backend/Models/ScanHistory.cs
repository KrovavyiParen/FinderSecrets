using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
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
}