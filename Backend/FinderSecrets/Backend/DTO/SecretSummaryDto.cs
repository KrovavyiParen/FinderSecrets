using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Backend.DTO;

namespace Backend.DTO
{
    public class SecretSummaryDto
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string VariableName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}