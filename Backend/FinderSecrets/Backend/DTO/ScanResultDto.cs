using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Backend.Models;

namespace Backend.DTO
{
    public class ScanResultDto
    {
        public string FileName { get; set; } = string.Empty;
        public List<SecretResponseDto> Secrets { get; set; } = new();
        public string Error { get; set; } = string.Empty;
    }
    public class SecretResponseDto
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public int Position { get; set; }
    }
}