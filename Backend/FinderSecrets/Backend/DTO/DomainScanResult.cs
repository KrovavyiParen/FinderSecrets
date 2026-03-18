using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Backend.DTO;
namespace Backend.DTO
{
    public class DomainScanResult
    {
        public string Domain { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public int SecretsFound { get; set; } 
        public List<SecretSummaryDto> Secrets { get; set; } = new List<SecretSummaryDto>();
        public string ScanStatus { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime ScanTime { get; set; }
    }
}