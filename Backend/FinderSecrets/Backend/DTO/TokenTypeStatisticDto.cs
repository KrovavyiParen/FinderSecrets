namespace Backend.DTO
{
    public class TokenTypeStatisticDto
    {
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // "Secret" или "Token"
        public int Count { get; set; }
        public DateTime LastFound { get; set; }
    }
}
