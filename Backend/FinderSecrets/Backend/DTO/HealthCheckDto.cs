using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
namespace Backend.DTO
{
    public class HealthCheckDto
    {
        public string Status { get; set; } = "Healthy";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Version { get; set; } = "1.0.0";
        public Dictionary<string, string> Services { get; set; } = new Dictionary<string, string>();
        public string Error { get; set; } = string.Empty;
    }
}