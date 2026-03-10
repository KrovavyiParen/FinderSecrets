using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Backend.DTO;
namespace Backend.DTO
{
    public class ScanSummaryDto
    {
        public int DomainsWithSecrets { get; set; }
        public int DomainsWithoutSecrets { get; set; }
        public int FailedDomains { get; set; }
        public Dictionary<string, int> SecretTypesSummary { get; set; }
    }
}