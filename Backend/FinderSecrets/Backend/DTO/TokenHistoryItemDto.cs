namespace Backend.DTO
{
    public class TokenHistoryItemDto
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public string SecretType { get; set; } = string.Empty;
        public string SecretValue { get; set; } = string.Empty;
        public string VariableName { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public int Position { get; set; }
        public string FirstFoundAt { get; set; } = string.Empty;
        public string LastFoundAt { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string InputType { get; set; } = string.Empty;
        public string InputPreview { get; set; } = string.Empty;
        public string ScanDate { get; set; } = string.Empty;
    }
}
