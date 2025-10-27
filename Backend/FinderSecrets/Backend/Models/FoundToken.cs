using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
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
}