using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
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
}