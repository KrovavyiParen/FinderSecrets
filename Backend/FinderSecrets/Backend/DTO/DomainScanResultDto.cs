using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Backend.DTO;

namespace Backend.DTO
{
    public class DomainScanResultDto
    {
        public string SourceUrl { get; set; }
        public int TotalDomainsScanned { get; set; }
        public int TotalSecretsFound { get; set; }
        public int ScanDurationMs { get; set; }
        public List<DomainScanResult> Results { get; set; }
        public ScanSummaryDto Summary { get; set; }
        public string Error { get; set; }
    }
}