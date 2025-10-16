using System.ComponentModel.DataAnnotations;
namespace Backend.DTO
{
    public class ScanTextRequestDto
    {
        [Required(ErrorMessage = "Text is required")]
        [StringLength(100000, ErrorMessage = "Text length cannot exceed 100000 characters")]
        public string Text { get; set; } = string.Empty;
    }
}