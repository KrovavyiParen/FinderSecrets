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
            public int LineNumber { get; set; }
            public int Position { get; set; }
        }

        public class ScanResult
        {
            public string FileName { get; set; } = string.Empty;
            public List<SecretResponse> Secrets { get; set; } = new();
            public string Error { get; set; } = string.Empty;
        }
    }
}
