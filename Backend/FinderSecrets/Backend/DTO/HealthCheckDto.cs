using System.ComponentModel.DataAnnotations;
namespace Backend.DTO
{
    public class HealthCheckDto
    {
        public string Status { get; set; } = "Healthy";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Version { get; set; } = "1.0.0";
    }
}