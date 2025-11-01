using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    [Table("scan_requests")]
    public class ScanRequestEntity
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("input_type")]
        [StringLength(20)]
        public string InputType { get; set; } = string.Empty;

        [Column("input_data")]
        public string InputData { get; set; } = string.Empty;

        [Column("secrets_count")]
        public int SecretsCount { get; set; }

        [Column("scan_duration")]
        public int ScanDuration { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}