using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Backend.Models.Model;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SecretsFinderController : ControllerBase
    {
        private readonly SecretsFinder _secretsFinder;
        private readonly ILogger<SecretsFinderController> _logger;

        public SecretsFinderController(SecretsFinder secretsFinder, ILogger<SecretsFinderController> logger)
        {
            _secretsFinder = secretsFinder;
            _logger = logger;
        }

        [HttpPost("scan-text")]
        public async Task<ActionResult<ScanResult>> ScanText([FromBody] ScanRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Text))
                {
                    return BadRequest(new ScanResult
                    {
                        Error = "Text is required"
                    });
                }

                var secrets = _secretsFinder.FindSecrets(request.Text);

                return Ok(new ScanResult
                {
                    FileName = "text-input",
                    Secrets = secrets.Select(s => new SecretResponse
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
                _logger.LogError(ex, "Error scanning text");
                return StatusCode(500, new ScanResult
                {
                    Error = ex.Message
                });
            }
        }

        [HttpPost("scan-file")]
        public async Task<ActionResult<ScanResult>> ScanFile(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new ScanResult
                    {
                        Error = "File is required"
                    });
                }

                using var stream = new StreamReader(file.OpenReadStream());
                var content = await stream.ReadToEndAsync();
                var secrets = _secretsFinder.FindSecrets(content);

                return Ok(new ScanResult
                {
                    FileName = file.FileName,
                    Secrets = secrets.Select(s => new SecretResponse
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
                return StatusCode(500, new ScanResult
                {
                    Error = ex.Message
                });
            }
        }
    }
}
