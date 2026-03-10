using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Backend.DTO;

namespace Backend.DTO
{
    public class SecretSummaryDto
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public string VariableName { get; set; }
        public bool IsActive { get; set; }
    }
}