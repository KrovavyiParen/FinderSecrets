using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Backend.Services;
using System.ComponentModel.DataAnnotations;
using Backend.DTO;
using static Backend.Models.Model;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SecretsFinderController : ControllerBase
    {
        private readonly ISecretsFinder _secretsFinder;
        private readonly ILogger<SecretsFinderController> _logger;

        public SecretsFinderController(ISecretsFinder secretsFinder, ILogger<SecretsFinderController> logger)
        {
            _secretsFinder = secretsFinder;
            _logger = logger;
        }

        [HttpPost("scan-text")]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ScanResultDto>> ScanText([FromBody] ScanTextRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Text))
                {
                    return BadRequest(new ScanResultDto
                    {
                        Error = "Text is required",
                        FileName = "text-input"
                    });
                }

                var secrets = _secretsFinder.FindSecrets(request.Text);

                return Ok(new ScanResultDto
                {
                    FileName = "text-input",
                    Secrets = secrets.Select(s => new SecretResponseDto
                    {
                        Type = s.Type,
                        Value = MaskSensitiveValue(s.Value),
                        LineNumber = s.LineNumber,
                        Position = s.Position
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning text");
                return StatusCode(500, new ScanResultDto
                {
                    Error = ex.Message,
                    FileName = "text-input"
                });
            }
        }

        [HttpPost("scan-file")]
        [RequestSizeLimit(5_242_880)]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status413PayloadTooLarge)]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ScanResultDto>> ScanFile(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new ScanResultDto
                    {
                        Error = "File is required",
                        FileName = "unknown"
                    });
                }
                if (file.Length > 5_242_880) // 5MB
                {
                    return StatusCode(413, new ScanResultDto
                    {
                        Error = "File size cannot exceed 5MB",
                        FileName = file.FileName
                    });
                }

                using var stream = new StreamReader(file.OpenReadStream());
                var content = await stream.ReadToEndAsync();
                var secrets = _secretsFinder.FindSecrets(content);

                return Ok(new ScanResultDto
                {
                    FileName = file.FileName,
                    Secrets = secrets.Select(s => new SecretResponseDto
                    {
                        Type = s.Type,
                        Value = s.Value,
                        LineNumber = s.LineNumber,
                        Position = s.Position
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning file: {FileName}", file?.FileName);
                return StatusCode(500, new ScanResultDto
                {
                    Error = ex.Message,
                    FileName = file?.FileName ?? "unknown"
                });
            }
        }
        [HttpGet("supported-types")]
        [ProducesResponseType(typeof(SupportedTypesResponseDto), StatusCodes.Status200OK)]
        public ActionResult<SupportedTypesResponseDto> GetSupportedSecretTypes()
        {
            var supportedTypes = new List<SecretTypeInfoDto>
            {
                new() { Type = "API_KEY", Description = "API ключи и токены", Examples = new[] { "AKIAIOSFODNN7EXAMPLE", "sk_live_123456789" } },
                new() { Type = "PASSWORD", Description = "Пароли и учетные данные", Examples = new[] { "password123", "secret" } },
                new() { Type = "PRIVATE_KEY", Description = "Приватные ключи SSH/RSA", Examples = new[] { "-----BEGIN PRIVATE KEY-----" } },
                new() { Type = "DATABASE_URL", Description = "URL подключения к базам данных", Examples = new[] { "postgresql://user:pass@localhost:5432/db" } },
                new() { Type = "CRYPTO_WALLET", Description = "Криптокошельки и seed фразы", Examples = new[] { "0x742d35Cc6634C0532925a3b8D" } }
            };

            return Ok(new SupportedTypesResponseDto
            {
                Success = true,
                SupportedTypes = supportedTypes,
                TotalTypes = supportedTypes.Count
            });
        }
        [HttpGet("health")]
        [ProducesResponseType(typeof(HealthCheckDto), StatusCodes.Status200OK)]
        public ActionResult<HealthCheckResponse> HealthCheck()
        {
            return Ok(new HealthCheckDto
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0"
            });
        }
        private string MaskSensitiveValue(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= 8)
                return "***MASKED***";

            return value.Length > 16 
                ? $"{value.Substring(0, 4)}...{value.Substring(value.Length - 4)}" 
                : "***MASKED***";
        }
    }
    
}
