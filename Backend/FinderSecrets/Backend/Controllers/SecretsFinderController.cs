using Backend.DTO;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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
        private readonly DatabaseService _databaseService;
        private readonly IConfiguration _configuration;

        public SecretsFinderController(IConfiguration configuration, ISecretsFinder secretsFinder, ILogger<SecretsFinderController> logger, DatabaseService databaseService)
        {
            _configuration = configuration;
            _secretsFinder = secretsFinder;
            _logger = logger;
            _databaseService = databaseService;
        }
        private async Task<Guid> GetCurrentUserIdAsync()
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                           ?? User.FindFirst("email")?.Value;

            if (string.IsNullOrEmpty(userEmail))
            {
                throw new UnauthorizedAccessException("User not authenticated");
            }

            using var scope = HttpContext.RequestServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                throw new InvalidOperationException($"User with email {userEmail} not found");
            }

            return user.Id;
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
            using var scope = HttpContext.RequestServices.CreateScope();
            var _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found");
            }
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == Guid.Parse(userId));
        
            if (user == null)
            {
                return NotFound("User not found");
            }
    
            return Ok(new {
                id = user.Id,
                email = user.Email,
                username = user.Username,
                createdAt = user.CreatedAt
            });
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
        /// 
        [Authorize]
        [HttpPost("scan-text")]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ScanResultDto>> ScanText([FromBody] ScanTextRequestDto request)
        {
            var stopwatch = Stopwatch.StartNew();
            int requestId = 0;
            int sessionId;
            bool IsUrl = false;
            string url = "";
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

                try { url = UrlValidate(request.Text, out IsUrl); }
                catch (Exception x)
                { return BadRequest(new ScanResultDto { Error = x.Message, FileName = request.Text }); }
                if (IsUrl)
                {
                    sessionId = await _secretsFinder.StartScanAsync(url, 2, 1);
                }
                //Сохраняем запрос в БД
                var userId = await GetCurrentUserIdAsync();
                var scanRequest = new ScanRequestEntity
                {
                    UserId = userId,
                    InputType = IsUrl ? "url" : "text",
                    InputData = IsUrl ? $"URL: {request.Text}" : (request.Text.Length > 1000 ? request.Text.Substring(0, 1000) + "..." : request.Text),
                    SecretsCount = 0,
                    ScanDuration = 0,
                    CreatedAt = DateTime.UtcNow
                };
                 requestId = await _databaseService.SaveScanRequestAsync(scanRequest);
                var client = new HttpClient();
                string content = IsUrl ? await client.GetStringAsync(url) : request.Text;

                var secrets = _secretsFinder.FindSecrets(content);

                stopwatch.Stop();
                var foundSecrets = secrets.Select(s => new FoundSecret
                //Сохраняем найденные секреты в БД
                {
                    RequestId = requestId,
                    SecretType = s.Type,
                    SecretValue = s.Value,
                    VariableName = s.VariableName,
                    LineNumber = s.LineNumber,
                    Position = s.Position,
                    FirstFoundAt = DateTime.UtcNow,
                    LastFoundAt = DateTime.UtcNow,
                    IsActive = s.IsActive
                }).ToList();

                await _databaseService.SaveFoundSecretsAsync(foundSecrets);

                // Обновляем статистику сканирования
                await _databaseService.UpdateScanStatisticsAsync(requestId, secrets.Count, (int)stopwatch.ElapsedMilliseconds);

                // Сохраняем в историю сканирований
                await SaveToScanHistory(userId, IsUrl ? "url" : "text",request.Text.Length > 500 ? request.Text.Substring(0, 500) + "..." : request.Text, secrets.Count);


                return Ok(new ScanResultDto
                {
                    FileName = IsUrl ? url : "text-input",
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
                _logger.LogError(ex, IsUrl ? $"Error scanning file: {url}" : "Error scanning text");
                return StatusCode(500, new ScanResultDto
                {
                    Error = ex.Message,
                    FileName = url ?? "unknown"
                });
            }
        }

        /// <summary>
        /// Запуск сканирования доменов и поиска секретов на сайтах
        /// </summary>
        /// <remarks>
        /// Пример запроса:
        /// POST /api/SecretsFinder/start-scan
        /// {
        ///     "text": "https://example.com"
        /// }
        /// </remarks>
        /// <param name="request">Объект запроса с текстом/URL для сканирования</param>
        /// <returns>Результаты сканирования всех найденных доменов</returns>
        /// <response code="200">Успешное сканирование</response>
        /// <response code="400">Неверный запрос</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize]
        [HttpPost("start-scan")]
        [ProducesResponseType(typeof(DomainScanResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DomainScanResultDto>> StartScan([FromBody] ScanTextRequestDto request)
        {
            var stopwatch = Stopwatch.StartNew();
            int requestId = 0;
            int sessionId;
            bool IsUrl = false;
            string url = "";
            var domains = new List<string>();
            var scanResults = new List<DomainScanResult>();
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

                try { url = UrlValidate(request.Text, out IsUrl); }
                catch (Exception x)
                { return BadRequest(new ScanResultDto { Error = x.Message, FileName = request.Text }); }
                if (IsUrl)
                {
                    sessionId = await _secretsFinder.StartScanAsync(url,2,1);
                    await _secretsFinder.WaitForScanCompletionAndFetchDataAsync(url, sessionId);
                    domains = await _secretsFinder.GetDomainsAsync(url);
                }
                Guid userId = Guid.Empty;
                try
                {
                    userId = await GetCurrentUserIdAsync();
                }
                catch (UnauthorizedAccessException)
                {
                    _logger.LogWarning("User not authenticated for scan");
                }
                if (userId != Guid.Empty)
                {
                    var scanRequest = new ScanRequestEntity
                    {
                        UserId = userId,
                        InputType = IsUrl ? "url" : "text",
                        InputData = IsUrl ? $"URL: {request.Text}" : (request.Text.Length > 1000 ? request.Text.Substring(0, 1000) + "..." : request.Text),
                        SecretsCount = 0,
                        ScanDuration = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    requestId = await _databaseService.SaveScanRequestAsync(scanRequest);
                }
                _logger.LogInformation("Starting secrets scan for {Count} domains", domains.Count);
                var secretsFound = 0;
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                foreach (var domain in domains)
                {
                    try
                    {
                        _logger.LogDebug("Scanning domain: {Domain}", domain);
                        var domainUrl = domain.StartsWith("http") ? domain : $"https://{domain}";
                        var content = await httpClient.GetStringAsync(domainUrl);
                        var domainSecrets = _secretsFinder.FindSecrets(content);
                        if (domainSecrets.Any())
                        {
                            _logger.LogInformation("Found {Count} secrets in domain: {Domain}", domainSecrets.Count(), domain);
                            if (requestId > 0)
                            {
                                var foundSecrets = domainSecrets.Select(s => new FoundSecret
                                {
                                    RequestId = requestId,
                                    SecretType = s.Type,
                                    SecretValue = s.Value,
                                    VariableName = s.VariableName,
                                    LineNumber = s.LineNumber,
                                    Position = s.Position,
                                    FirstFoundAt = DateTime.UtcNow,
                                    LastFoundAt = DateTime.UtcNow,
                                    IsActive = s.IsActive,
                                    SourceUrl = domainUrl,
                                    Domain = domain,
                                    HttpStatusCode = 200
                                }).ToList();
                                await _databaseService.SaveFoundSecretsAsync(foundSecrets);
                            }
                            scanResults.Add(new DomainScanResult
                            {
                                Domain = domain,
                                Url = domainUrl,
                                SecretsFound = domainSecrets.Count(),
                                Secrets = domainSecrets.Select(s => new SecretSummaryDto
                                {
                                    Type = s.Type,
                                    Value = MaskSensitiveValue(s.Value),
                                    VariableName = s.VariableName,
                                    IsActive = s.IsActive
                                }).ToList(),
                                ScanStatus = "Success",
                                ScanTime = DateTime.UtcNow
                            });
                            secretsFound += domainSecrets.Count();
                        }
                        else
                        {
                            scanResults.Add(new DomainScanResult
                            {
                                Domain = domain,
                                Url = domainUrl,
                                SecretsFound = 0,
                                Secrets = new List<SecretSummaryDto>(),
                                ScanStatus = "Success",
                                ScanTime = DateTime.UtcNow
                            });
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogWarning(ex, "Failed to access domain: {Domain}", domain);
                        scanResults.Add(new DomainScanResult
                        {
                            Domain = domain,
                            Url = domain,
                            SecretsFound = 0,
                            ScanStatus = "Failed",
                            ErrorMessage = $"HTTP error: {ex.Message}",
                            ScanTime = DateTime.UtcNow
                        });
                    }
                    catch (TaskCanceledException)
                    {
                        _logger.LogWarning("Timeout scanning domain: {Domain}", domain);
                        scanResults.Add(new DomainScanResult
                        {
                            Domain = domain,
                            Url = domain,
                            SecretsFound = 0,
                            ScanStatus = "Timeout",
                            ErrorMessage = "Request timed out",
                            ScanTime = DateTime.UtcNow
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error scanning domain: {Domain}", domain);
                        scanResults.Add(new DomainScanResult
                        {
                            Domain = domain,
                            Url = domain,
                            SecretsFound = 0,
                            ScanStatus = "Error",
                            ErrorMessage = ex.Message,
                            ScanTime = DateTime.UtcNow
                        });
                    }
                }
                  

                stopwatch.Stop();
                if (requestId > 0)
                {
                    await _databaseService.UpdateScanStatisticsAsync(requestId, secretsFound, (int)stopwatch.ElapsedMilliseconds);
                    await SaveToScanHistory(userId, "domain_scan", $"Scanned {domains.Count} domains", secretsFound);
                }
                _logger.LogInformation("Domain scan completed. Scanned {Total} domains, found {Secrets} secrets in {Elapsed}ms", 
                    domains.Count, secretsFound, stopwatch.ElapsedMilliseconds);
                return Ok(new DomainScanResultDto
                {
                    SourceUrl = url,
                    TotalDomainsScanned = domains.Count,
                    TotalSecretsFound = secretsFound,
                    ScanDurationMs = (int)stopwatch.ElapsedMilliseconds,
                    Results = scanResults,
                    Summary = new ScanSummaryDto
                    {
                        DomainsWithSecrets = scanResults.Count(r => r.SecretsFound > 0),
                        DomainsWithoutSecrets = scanResults.Count(r => r.SecretsFound == 0 && r.ScanStatus == "Success"),
                        SecretTypesSummary = scanResults
                            .Where(r => r.Secrets != null)
                            .SelectMany(r => r.Secrets)
                            .GroupBy(s => s.Type)
                            .ToDictionary(g => g.Key, g => g.Count())
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, IsUrl ? $"Error scanning file: {url}" : "Error scanning text");
                return StatusCode(500, new ScanResultDto
                {
                    Error = ex.Message,
                    FileName = url ?? "unknown"
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
        [Authorize]
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
                    VariableName = s.VariableName,
                    LineNumber = s.LineNumber,
                    Position = s.Position,
                    FirstFoundAt = DateTime.UtcNow,
                    LastFoundAt = DateTime.UtcNow,
                    IsActive = s.IsActive
                }).ToList();
                await _databaseService.SaveFoundSecretsAsync(foundSecrets);
                // Обновляем статистику сканирования
                await _databaseService.UpdateScanStatisticsAsync(requestId, secrets.Count, (int)stopwatch.ElapsedMilliseconds);
                await SaveToScanHistory(userId, "file", file.FileName, secrets.Count);





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


        private string UrlValidate(string input, out bool isUrl)
        {
            input = input.Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                isUrl = false;
                throw new Exception("empty input");
            }
            if (!input.Contains(".") || (!Uri.TryCreate(input, UriKind.Absolute, out Uri? uri)
                && !Uri.TryCreate("https://" + input, UriKind.Absolute, out uri)))
            {
                isUrl = false;
                return "";
            }

            isUrl = true;

            if (uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            {

                var segments = uri.Segments.Select(s => s.Trim('/')).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                if (segments.Length >= 4 && segments[2] == "blob")
                {
                    return $"https://raw.githubusercontent.com/{segments[0]}/{segments[1]}/{segments[3]}/{string.Join("/", segments.Skip(4))}";
                }
            }

            return uri.ToString();
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
        [Authorize]
        [HttpPost("scan-url")]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status413PayloadTooLarge)]
        [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status500InternalServerError)]



        public async Task<ActionResult<ScanResultDto>> ScanURL([FromBody] Backend.DTO.UrlRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            int requestId = 0;
            string url = "";
            try
            {

                try { url = UrlValidate(request.url, out bool IsUrl); }
                catch (Exception x)
                { return BadRequest(new ScanResultDto { Error = x.Message, FileName = request.url }); }
                //var userId = await GetCurrentUserIdAsync();

                //Сохраняем запрос в БД
                //var scanRequest = new ScanRequestEntity
                //{
                //UserId = userId,
                //InputType = "url",
                //InputData = $"URL: {request.url}",
                //SecretsCount = 0,
                //ScanDuration = 0,
                //CreatedAt = DateTime.UtcNow
                //};
                //requestId = await _databaseService.SaveScanRequestAsync(scanRequest);

                var client = new HttpClient();
                string content = await client.GetStringAsync(url);

                var secrets = _secretsFinder.FindSecrets(content);
                stopwatch.Stop();
                var foundSecrets = secrets.Select(s => new FoundSecret
                {
                    RequestId = requestId,
                    SecretType = s.Type,
                    SecretValue = s.Value,
                    VariableName = s.VariableName,
                    LineNumber = s.LineNumber,
                    Position = s.Position,
                    FirstFoundAt = DateTime.UtcNow,
                    LastFoundAt = DateTime.UtcNow,
                    IsActive = s.IsActive
                }).ToList();

                await _databaseService.SaveFoundSecretsAsync(foundSecrets);

                // Обновляем статистику сканирования
                await _databaseService.UpdateScanStatisticsAsync(requestId, secrets.Count, (int)stopwatch.ElapsedMilliseconds);

                // Сохраняем в историю сканирований
                //await SaveToScanHistory(userId, "url", request.url, secrets.Count);


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
        /// Регистрация нового пользователя в системе
        /// </summary>
        /// <remarks>
        /// Создает новую учетную запись пользователя с предоставленными данными:
        /// - Проверяет уникальность email и username
        /// - Хэширует пароль перед сохранением
        /// - Создает запись пользователя в базе данных
        /// - Возвращает токены авторизации
        /// </remarks>
        /// <param name="request">Объект запроса с данными для регистрации</param>
        /// <returns>Данные пользователя и токены авторизации</returns>
        /// <response code="201">Пользователь успешно зарегистрирован</response>
        /// <response code="400">Некорректные данные или пользователь уже существует</response>
        /// <response code="422">Ошибка валидации входных данных</response>
        /// <response code="500">Внутренняя ошибка сервера при создании пользователя</response>
        [Authorize]
        [HttpPost("register")]
        
        public async Task<IActionResult> Register([FromBody] DTO.RegisterRequest request)
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Проверка валидации модели
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Проверка существования пользователя
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return Conflict("Пользователь с таким email уже существует");
            }

            // Создание нового пользователя
            var user = new User
            {
                Id = Guid.NewGuid(), // Генерация UUID
                Username = request.Username,
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password), // Хеширование пароля
                CreatedAt = DateTime.UtcNow,
                
            };

            // Сохранение в базу данных
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Регистрация успешна",
                userId = user.Id
            });
        }
        /// <summary>
        /// Авторизация пользователя в системе
        /// </summary>
        /// <remarks>
        /// Выполняет вход пользователя в систему с проверкой учетных данных:
        /// - Проверяет существование пользователя по email
        /// - Верифицирует пароль с использованием BCrypt
        /// - Генерирует JWT токен доступа
        /// - Создает refresh токен для обновления сессии
        /// - Возвращает токены и базовую информацию о пользователе
        /// </remarks>
        /// <param name="request">Объект запроса с учетными данными</param>
        /// <returns>Данные пользователя и токены авторизации</returns>
        /// <response code="200">Успешная аутентификация</response>
        /// <response code="400">Некорректный формат запроса</response>
        /// <response code="401">Неверные учетные данные</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                return Unauthorized("Invalid credentials");
            }
            
            // генерерация JWT token
            var token = GenerateJwtToken(user);

            // генерерация refresh token
            var refreshToken = GenerateRefreshToken();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                token = token,
                refreshToken = refreshToken,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    username = user.Username
                }
            });

        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT Key is not configured");
            }
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.Now.AddHours(3),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
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
        [Authorize]
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
        /// Получение истории и статистики найденных токенов
        /// </summary>
        /// <remarks>
        /// Пример запроса:
        /// GET /api/SecretsFinder/tokens-history
        /// GET /api/SecretsFinder/tokens-history?statistics=true
        /// GET /api/SecretsFinder/tokens-history?secretType=API_KEY&amp;page=1&amp;pageSize=20
        /// </remarks>
        [Authorize]
        [HttpGet("tokens-history")]
        [ProducesResponseType(typeof(TokenHistoryResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TokenHistoryResponseDto>> GetTokensHistory(
    [FromQuery] TokensHistoryRequestDto request)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                using var scope = HttpContext.RequestServices.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var secretsQuery = context.FoundSecrets
                    .Include(fs => fs.ScanRequest)
                    .Where(fs => fs.ScanRequest.UserId == userId);

                // Применяем фильтры
                if (request.FromDate.HasValue)
                {
                    secretsQuery = secretsQuery.Where(fs => fs.FirstFoundAt >= request.FromDate.Value);
                }

                if (request.ToDate.HasValue)
                {
                    secretsQuery = secretsQuery.Where(fs => fs.FirstFoundAt <= request.ToDate.Value);
                }

                if (!string.IsNullOrEmpty(request.SecretType))
                {
                    secretsQuery = secretsQuery.Where(fs => fs.SecretType == request.SecretType);
                }

                if (request.IsActive.HasValue)
                {
                    secretsQuery = secretsQuery.Where(fs => fs.IsActive == request.IsActive.Value);
                }

                if (request.Statistics)
                {
                    return await GetStatisticsResponse(secretsQuery);
                }
                else
                {
                    return await GetDetailedHistoryResponse(secretsQuery);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tokens history");
                return StatusCode(500, new TokenHistoryResponseDto
                {
                    Statistics = new TokensStatisticsDto(),
                    Items = new List<TokenHistoryItemDto>(),
                    TotalCount = 0,
                    GeneratedAt = DateTime.UtcNow.ToString()
                });
            }
        }

        private async Task<ActionResult<TokenHistoryResponseDto>> GetStatisticsResponse(
            IQueryable<FoundSecret> secretsQuery)
        {
            var secretsByType = await secretsQuery
                .GroupBy(fs => fs.SecretType)
                .Select(g => new TokenTypeStatisticDto
                {
                    Type = g.Key,
                    Category = "Secret",
                    Count = g.Count(),
                    LastFound = g.Max(fs => fs.FirstFoundAt).ToString("dd.MM.yyyy HH:mm:ss")
                })
                .ToListAsync();

            var totalSecrets = await secretsQuery.CountAsync();
            var activeSecrets = await secretsQuery.CountAsync(fs => fs.IsActive);

            return Ok(new TokenHistoryResponseDto
            {
                Statistics = new TokensStatisticsDto
                {
                    TotalSecrets = totalSecrets,
                    TotalTokens = 0, // Теперь всегда 0
                    TotalItems = totalSecrets,
                    ActiveSecrets = activeSecrets,
                    ActiveTokens = 0, // Теперь всегда 0
                    ActiveItems = activeSecrets,
                    StatisticsByType = secretsByType
                },
                Items = new List<TokenHistoryItemDto>(),
                TotalCount = 0,
                GeneratedAt = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm:ss")
            });
        }

        private async Task<ActionResult<TokenHistoryResponseDto>> GetDetailedHistoryResponse(
            IQueryable<FoundSecret> secretsQuery)
        {
            var secretsItems = await secretsQuery
                .Select(fs => new TokenHistoryItemDto
                {
                    Id = fs.Id,
                    Category = "Secret",
                    SecretType = fs.SecretType,
                    SecretValue = MaskSensitiveValue(fs.SecretValue),
                    VariableName = fs.VariableName,
                    LineNumber = fs.LineNumber,
                    Position = fs.Position,
                    FirstFoundAt = fs.FirstFoundAt.ToString("dd.MM.yyyy HH:mm:ss"),
                    LastFoundAt = fs.LastFoundAt.ToString("dd.MM.yyyy HH:mm:ss"),
                    IsActive = fs.IsActive,
                    InputType = "Secret Scan",
                    InputPreview = $"Secret: {fs.SecretType}",
                    ScanDate = fs.FirstFoundAt.ToString("dd.MM.yyyy HH:mm:ss")
                })
                .ToListAsync();

            // Применяем пагинацию
            var totalCount = secretsItems.Count;
            var pagedItems = secretsItems
                .ToList();

            return Ok(new TokenHistoryResponseDto
            {
                Statistics = new TokensStatisticsDto
                {
                    TotalSecrets = totalCount,
                    TotalTokens = 0,
                    TotalItems = totalCount
                },
                Items = pagedItems,
                TotalCount = totalCount,
                GeneratedAt = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm:ss")
            });
        }

        /// <summary>
        /// Проверка здоровья сервиса SecretsFinder
        /// </summary>
        /// <returns>Статус здоровья сервиса</returns>
        /// <response code="200">Сервис работает корректно</response>
        [HttpGet("health")]
        [Authorize]
        [ProducesResponseType(typeof(HealthCheckDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<HealthCheckDto>> HealthCheck()
        {
            var healthCheck = new HealthCheckDto
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm:ss"),
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
        private static string MaskSensitiveValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "***MASKED***";

            if (value.Length <= 50)
                return value;

            // Для строк длиннее 50 символов показываем только начало и конец
            return $"{value.Substring(0, 4)}....{value.Substring(value.Length - 4)}";

        }
        private async Task SaveToScanHistory(Guid userId, string inputType, string inputPreview, int secretsFound)
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