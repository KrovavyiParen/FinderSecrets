using System.ComponentModel.DataAnnotations;
namespace Backend.DTO
{
    public class UrlRequest
    {
        [Required(ErrorMessage = "URL is required")]
        public string url { get; set; } = string.Empty;
    }
}