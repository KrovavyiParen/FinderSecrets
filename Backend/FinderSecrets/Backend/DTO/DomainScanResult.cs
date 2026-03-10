using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Backend.DTO;
namespace Backend.DTO
{
    public class DomainScanResult
    {
        public string Domain { get; set; }
        public string Url { get; set; }
        public int SecretsFound { get; set; }
        public List<SecretSummaryDto> Secrets { get; set; }
        public string ScanStatus { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime ScanTime { get; set; }
    }
}