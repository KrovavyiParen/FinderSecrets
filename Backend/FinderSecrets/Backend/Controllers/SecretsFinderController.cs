using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Backend.Services;
using System.ComponentModel.DataAnnotations;
using Backend.DTO;
using static Backend.Models.Model;

namespace Backend.Controllers
{
    /// <summary>
    /// Контроллер для поиска секретов и конфиденциальных данных
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SecretsFinderController : ControllerBase
    {
        private readonly ISecretsFinder _secretsFinder;
        private readonly ILogger<SecretsFinderController> _logger;

        public SecretsFinderController(ISecretsFinder secretsFinder, ILogger<SecretsFinderController> logger)
        {
            _secretsFinder = secretsFinder;
            _logger = logger;
        }

        /// <summary>
        /// Сканирование текста на наличие секретов
        /// </summary>
        /// <remarks>
        /// Пример запроса:
        /// POST /api/SecretsFinder/scan-text
        /// {
        ///     "text": "password=secret123, api_key=AKIAIOSFODNN7EXAMPLE"
        /// }
        /// </remarks>
        /// <param name="request">Объект запроса с текстом для сканирования</param>
        /// <returns>Результат сканирования с найденными секретами</returns>
        /// <response code="200">Успешное сканирование</response>
        /// <response code="400">Неверный запрос</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
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

                var secrets = await _secretsFinder.FindSecrets(request.Text);

                return Ok(new ScanResultDto
                {
                    FileName = "text-input",
                    Secrets = secrets.Select(s => new SecretResponseDto
                    {
                        Type = s.Type,
                        Value = MaskSensitiveValue(s.Value),
                        LineNumber = s.LineNumber,
                        Position = s.Position,
                        VariableName = s.VariableName,
                        IsActive = s.IsActive,
                        BotName = s.BotName,
                        BotUsername = s.BotUsername,
                        ValidationError = s.ValidationError
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

        /// <summary>
        /// Сканирование файла на наличие секретов
        /// </summary>
        /// <remarks>
        /// Максимальный размер файла: 5MB
        /// Поддерживаемые форматы: любые текстовые файлы
        /// </remarks>
        /// <param name="file">Файл для сканирования</param>
        /// <returns>Результат сканирования файла</returns>
        /// <response code="200">Успешное сканирование файла</response>
        /// <response code="400">Неверный запрос</response>
        /// <response code="413">Превышен максимальный размер файла</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
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

                var secrets = await _secretsFinder.FindSecretsInFile(file);

                return Ok(new ScanResultDto
                {
                    FileName = file.FileName,
                    Secrets = secrets.Select(s => new SecretResponseDto
                    {
                        Type = s.Type,
                        Value = s.Value,
                        VariableName = s.VariableName,
                        LineNumber = s.LineNumber,
                        Position = s.Position,
                        IsActive = s.IsActive,
                        BotName = s.BotName,
                        BotUsername = s.BotUsername,
                        ValidationError = s.ValidationError
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


        /// <summary>
        /// Получение списка поддерживаемых типов секретов
        /// </summary>
        /// <returns>Список типов секретов с описанием и примерами</returns>
        /// <response code="200">Успешное получение списка</response>


        private string UrlValidate(string url)
        {
            Uri uri = new Uri(url);

            if (uri.Host == "github.com")
            {
                return $"https://raw.githubusercontent.com/" + $"{uri.Segments[1]}{uri.Segments[2]}{uri.Segments[4]}{string.Join("", uri.Segments.Skip(5))}";
            }
            else if (uri.Host == "raw.githubusercontent.com")
            { return url; }
            else 
            { throw new Exception("Not a github link"); }

            
        }

        /// <summary>
        /// Сканирует содержимое по URL на наличие секретов и конфиденциальной информации
        /// </summary>
        /// <remarks>
        /// Отправляет HTTP-запрос по указанному URL, загружает содержимое и проверяет его на наличие:
        /// - API ключей и токенов
        /// - Паролей и учетных данных
        /// - Приватных ключей SSH/RSA
        /// - URL подключения к базам данных
        /// - Криптокошельков и seed фраз
        /// </remarks>
        /// <param name="request">Объект запроса с URL для сканирования</param>
        /// <returns>Результат сканирования с найденными секретами или ошибкой</returns>
        /// <response code="200">Возвращает результат сканирования с найденными секретами</response>
        /// <response code="400">Некорректный URL или ошибка валидации</response>
        /// <response code="413">Превышен максимальный размер файла</response>
        /// <response code="500">Внутренняя ошибка сервера при обработке запроса</response>
        [HttpPost("scan-url")]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status413PayloadTooLarge)]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status500InternalServerError)]

        

        public async Task<ActionResult<ScanResultDto>> ScanURL([FromBody] UrlRequest request)
        {
            string url = "";
            try
            {
                
                try {  url = UrlValidate(request.url); }
                catch (Exception x)
                {return BadRequest(new ScanResultDto { Error = x.Message, FileName = request.url });}
                
                var client = new HttpClient();
                string content = await client.GetStringAsync(url);
                var secrets = await _secretsFinder.FindSecrets(content);

                return Ok(new ScanResultDto
                {
                    FileName = url,
                    Secrets = secrets.Select(s => new SecretResponseDto
                    {
                        Type = s.Type,
                        Value = s.Value,
                        VariableName = s.VariableName,
                        LineNumber = s.LineNumber,
                        Position = s.Position,
                        IsActive = s.IsActive,
                        BotName = s.BotName,
                        BotUsername = s.BotUsername,
                        ValidationError = s.ValidationError
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning file: {url}", url);
                return StatusCode(500, new ScanResultDto
                {
                    Error = ex.Message,
                    FileName = url ?? "unknown"
                });
            }
        }

        /// <summary>
        /// Возвращает список поддерживаемых типов секретов для сканирования
        /// </summary>
        /// <remarks>
        /// Предоставляет информацию о всех типах конфиденциальных данных, которые может обнаружить сервис,
        /// включая описание и примеры для каждого типа.
        /// </remarks>
        /// <returns>Список поддерживаемых типов секретов с описанием и примерами</returns>
        /// <response code="200">Возвращает информацию о поддерживаемых типах секретов</response>
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

        /// <summary>
        /// Проверка здоровья сервиса SecretsFinder
        /// </summary>
        /// <returns>Статус здоровья сервиса</returns>
        /// <response code="200">Сервис работает корректно</response>
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
            if (string.IsNullOrEmpty(value))
                return "***MASKED***";

             if (value.Length <= 50)
                return value;
    
            // Для строк длиннее 50 символов показываем только начало и конец
            return $"{value.Substring(0, 4)}....{value.Substring(value.Length - 4)}";

        }
    }
}
