using System.Collections.Generic;
using Backend.Models;
namespace Backend.DTO
{
    public class SupportedTypesResponseDto
    {
        public bool Success { get; set; }
        public List<SecretTypeInfoDto> SupportedTypes { get; set; } = new();
        public int TotalTypes { get; set; }
    }
    public class SecretTypeInfoDto
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[] Examples { get; set; } = Array.Empty<string>();
    }
}