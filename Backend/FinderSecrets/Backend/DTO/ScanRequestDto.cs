using System.ComponentModel.DataAnnotations;
namespace Backend.DTO
{
    public class ScanRequestDto
    {
        public string? Text { get; set; }
        public IFormFile? File { get; set; }
        public string? FilePath { get; set; }
    }
}