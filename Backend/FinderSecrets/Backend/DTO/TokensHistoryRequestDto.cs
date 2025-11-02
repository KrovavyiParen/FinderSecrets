namespace Backend.DTO
{
    public class TokensHistoryRequestDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SecretType { get; set; }
        public bool? IsActive { get; set; }
        public bool Statistics { get; set; } = false;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
