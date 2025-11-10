namespace Backend.DTO
{
    public class TokenTypeStatisticDto
    {
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public string LastFound { get; set; } = string.Empty;
    }
}
