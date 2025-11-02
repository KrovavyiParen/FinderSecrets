namespace Backend.DTO
{
    public class TokensStatisticsDto
    {
        public int TotalSecrets { get; set; }
        public int TotalTokens { get; set; }
        public int TotalItems { get; set; }
        public int ActiveSecrets { get; set; }
        public int ActiveTokens { get; set; }
        public int ActiveItems { get; set; }
        public List<TokenTypeStatisticDto> StatisticsByType { get; set; } = new List<TokenTypeStatisticDto>();
    }
}