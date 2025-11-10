namespace Backend.DTO
{
    public class TokenHistoryResponseDto
    {
        public TokensStatisticsDto Statistics { get; set; } = new TokensStatisticsDto();
        public List<TokenHistoryItemDto> Items { get; set; } = new List<TokenHistoryItemDto>();
        public int TotalCount { get; set; }
        public string GeneratedAt { get; set; } = string.Empty;
    }
}
