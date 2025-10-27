using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Backend.Services;
using System.ComponentModel.DataAnnotations;
using Backend.DTO;
using System.Collections.Generic;
using static Backend.Models.Model;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
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
        private readonly DatabaseService _databaseService;

        public SecretsFinderController(ISecretsFinder secretsFinder, ILogger<SecretsFinderController> logger, DatabaseService databaseService)
        {
            _secretsFinder = secretsFinder;
            _logger = logger;
            _databaseService = databaseService;
        }
        private async Task<int> GetCurrentUserIdAsync()
        {
            // Временное решение - возвращаем ID первого пользователя или создаем нового
            // В реальном приложении здесь должна быть аутентификация
            try
            {
                using var scope = HttpContext.RequestServices.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                var user = await context.Users.FirstOrDefaultAsync();
                if (user != null)
                {
                    return user.Id;
                }
                
                // Если пользователей нет, создаем временного
                var newUser = new User
                {
                    Username = "anonymous",
                    Email = "anonymous@example.com",
                    CreatedAt = DateTime.UtcNow
                };
                
                context.Users.Add(newUser);
                await context.SaveChangesAsync();
                return newUser.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return 1; // fallback
            }
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
            var stopwatch = Stopwatch.StartNew();
            int requestId = 0;

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
                var userId = await GetCurrentUserIdAsync();
                 // Сохраняем запрос в БД
                var scanRequest = new ScanRequestEntity
                {
                    UserId = await GetCurrentUserIdAsync(), //1, // Временное значение, можно получить из аутентификации
                    InputType = "text",
                    InputData = request.Text.Length > 1000 ? request.Text.Substring(0, 1000) + "..." : request.Text,
                    SecretsCount = 0,
                    ScanDuration = 0,
                    CreatedAt = DateTime.UtcNow
                };
                requestId = await _databaseService.SaveScanRequestAsync(scanRequest);

                var secrets = _secretsFinder.FindSecrets(request.Text);
                stopwatch.Stop();
                // Сохраняем найденные секреты в БД
                var foundSecrets = secrets.Select(s => new FoundSecret
                {
                    RequestId = requestId,
                    SecretType = s.Type,
                    SecretValue = s.Value,
                    VariableName = "", // Можно добавить извлечение имени переменной
                    LineNumber = s.LineNumber,
                    Position = s.Position,
                    FirstFoundAt = DateTime.UtcNow,
                    LastFoundAt = DateTime.UtcNow,
                    IsActive = true
                }).ToList();

                await _databaseService.SaveFoundSecretsAsync(foundSecrets);

                // Обновляем статистику сканирования
                await _databaseService.UpdateScanStatisticsAsync(requestId, secrets.Count, (int)stopwatch.ElapsedMilliseconds);

                // Сохраняем в историю сканирований
                await SaveToScanHistory(1, "text", request.Text.Length > 500 ? request.Text.Substring(0, 500) + "..." : request.Text, secrets.Count);

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
            var stopwatch = Stopwatch.StartNew();
            int requestId = 0;
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
                var userId = await GetCurrentUserIdAsync();
                var scanRequest = new ScanRequestEntity
                {
                    UserId = userId,
                    InputType = "file",
                    InputData = $"File: {file.FileName}, Size: {file.Length} bytes",
                    SecretsCount = 0,
                    ScanDuration = 0,
                    CreatedAt = DateTime.UtcNow
                };
                requestId = await _databaseService.SaveScanRequestAsync(scanRequest);
                var secrets = _secretsFinder.FindSecretsInFile(file);
                stopwatch.Stop();
                var foundSecrets = secrets.Select(s => new FoundSecret
                {
                    RequestId = requestId,
                    SecretType = s.Type,
                    SecretValue = s.Value,
                    VariableName = "",
                    LineNumber = s.LineNumber,
                    Position = s.Position,
                    FirstFoundAt = DateTime.UtcNow,
                    LastFoundAt = DateTime.UtcNow,
                    IsActive = true
                }).ToList();
                await _databaseService.SaveFoundSecretsAsync(foundSecrets);
                // Обновляем статистику сканирования
                await _databaseService.UpdateScanStatisticsAsync(requestId, secrets.Count, (int)stopwatch.ElapsedMilliseconds);
                await SaveToScanHistory(1, "file", file.FileName, secrets.Count);

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
        [HttpPost("scan-url")]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status413PayloadTooLarge)]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status500InternalServerError)]

        

        public async Task<ActionResult<ScanResultDto>> ScanURL([FromBody] UrlRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            int requestId = 0;
            string url = "";
            try
            {
                
                try {  url = UrlValidate(request.url); }
                catch (Exception x)
                {return BadRequest(new ScanResultDto { Error = x.Message, FileName = request.url });}
                // Сохраняем запрос в БД
                var userId = await GetCurrentUserIdAsync();
                var scanRequest = new ScanRequestEntity
                {
                    UserId = userId,
                    InputType = "url",
                    InputData = $"URL: {request.url}",
                    SecretsCount = 0,
                    ScanDuration = 0,
                    CreatedAt = DateTime.UtcNow
                };
                requestId = await _databaseService.SaveScanRequestAsync(scanRequest);
                var client = new HttpClient();
                string content = await client.GetStringAsync(url);
                var secrets = _secretsFinder.FindSecrets(content);
                stopwatch.Stop();
                var foundSecrets = secrets.Select(s => new FoundSecret
                {
                    RequestId = requestId,
                    SecretType = s.Type,
                    SecretValue = s.Value,
                    VariableName = "",
                    LineNumber = s.LineNumber,
                    Position = s.Position,
                    FirstFoundAt = DateTime.UtcNow,
                    LastFoundAt = DateTime.UtcNow,
                    IsActive = true
                }).ToList();

                await _databaseService.SaveFoundSecretsAsync(foundSecrets);

                // Обновляем статистику сканирования
                await _databaseService.UpdateScanStatisticsAsync(requestId, secrets.Count, (int)stopwatch.ElapsedMilliseconds);

                // Сохраняем в историю сканирований
                await SaveToScanHistory(userId, "url", request.url, secrets.Count);

                return Ok(new ScanResultDto
                {
                    FileName = url,
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
                _logger.LogError(ex, "Error scanning file: {url}", url);
                return StatusCode(500, new ScanResultDto
                {
                    Error = ex.Message,
                    FileName = url ?? "unknown"
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

        /// <summary>
        /// Проверка здоровья сервиса SecretsFinder
        /// </summary>
        /// <returns>Статус здоровья сервиса</returns>
        /// <response code="200">Сервис работает корректно</response>
        [HttpGet("health")]
        [ProducesResponseType(typeof(HealthCheckDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<HealthCheckDto>> HealthCheck()
        {
            var healthCheck = new HealthCheckDto
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                Services = new Dictionary<string, string>()
            };

            // Проверка подключения к базе данных
            try
            {
                // Предполагая, что _secretsFinder имеет метод проверки БД
                var dbStatus = await _secretsFinder.CheckDatabaseConnection();
                //healthCheck.Services.Add("Database", dbStatus ? "Connected" : "Disconnected");
                
                if (!dbStatus)
                {
                    healthCheck.Status = "Unhealthy";
                    healthCheck.Error = "Database connection failed";
                }
            }
            catch (Exception ex)
            {
                healthCheck.Status = "Unhealthy";
                healthCheck.Error = $"Database error: {ex.Message}";
                //healthCheck.Services.Add("Database", "Error");
            }

            return Ok(healthCheck);
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
        private async Task SaveToScanHistory(int userId, string inputType, string inputPreview, int secretsFound)
        {
            try
            {
                var scanHistory = new ScanHistory
                {
                    UserId = userId,
                    InputType = inputType,
                    InputPreview = inputPreview,
                    SecretsFound = secretsFound,
                    ScannedAt = DateTime.UtcNow
                };

                // Используем контекст базы данных для сохранения
                using var scope = HttpContext.RequestServices.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.ScanHistory.Add(scanHistory);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving to scan history");
            }
        }
    }
}
