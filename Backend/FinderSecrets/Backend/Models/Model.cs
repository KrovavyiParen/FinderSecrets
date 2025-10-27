using System.ComponentModel.DataAnnotations;
namespace Backend.Models
{
    public class Model
    {
        public class ScanRequest
        {
            public string? Text { get; set; }
            public IFormFile? File { get; set; }
            public string? FilePath { get; set; }
        }

        public class SecretResponse
        {
            public string Type { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
            public string VariableName { get; set; } = string.Empty;
            public int LineNumber { get; set; }
            public int Position { get; set; }

            // Дополнительные поля для Telegram токенов
            public bool IsActive { get; set; }
            public string BotName { get; set; } = string.Empty;
            public string BotUsername { get; set; } = string.Empty;
            public string ValidationError { get; set; } = string.Empty;
        }

        public class ScanResult
        {
            public string FileName { get; set; } = string.Empty;
            public List<SecretResponse> Secrets { get; set; } = new();
            public string Error { get; set; } = string.Empty;
        }
        public class ScanTextRequest
        {
            [Required(ErrorMessage = "Text is required")]
            [StringLength(100000, ErrorMessage = "Text length cannot exceed 100000 characters")]
            public string Text { get; set; } = string.Empty;
        }

        public class SupportedTypesResponse
        {
            public bool Success { get; set; }
            public List<SecretTypeInfo> SupportedTypes { get; set; } = new();
            public int TotalTypes { get; set; }
        }

        public class SecretTypeInfo
        {
            public string Type { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string[] Examples { get; set; } = Array.Empty<string>();
        }
        public class HealthCheckResponse
        {
            public string Status { get; set; } = "Healthy";
            public DateTime Timestamp { get; set; } = DateTime.UtcNow;
            public string Version { get; set; } = "1.0.0";
        }
    }
    
}
