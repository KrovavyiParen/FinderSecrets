using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace Backend.Services
{
    public interface ISecretsFinder
    {
        List<SecretMatch> FindSecrets(string input);
        List<SecretMatch> FindSecretsInFile(IFormFile file);
        Task<bool> CheckDatabaseConnection();
        Task SaveSecretsToDatabaseAsync(List<SecretMatch> secrets, string source = "text");
    }
    
    public class SecretsFinder : ISecretsFinder
    {
        private readonly List<Pattern> _patterns;
        private readonly ILogger<SecretsFinder> _logger;
        private readonly string _connectionString;
        private readonly HttpClient _httpClient;

        public SecretsFinder(ILogger<SecretsFinder> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new ArgumentNullException("DefaultConnection string is null");
            _httpClient = httpClient;
            _ = InitializeDatabaseConnection(); 

            _patterns = new List<Pattern>
            {
                new("Viber/Skype-Bot-Token", @"([a-zA-Z0-9_]*?)\s*[=:]\s*['""]?([a-zA-Z0-9]{32,50})(?![a-zA-Z0-9])['""]?"),
                new("Telegram-Token", @"([a-zA-Z0-9_]+)\s*[=:]\s*['""]?(\d{8,10}:[\w_-]{35})['""]?"),
                new("JSON-Key-Value", @"""([a-zA-Z0-9_]+)""\s*[=:]\s*""([a-zA-Z0-9]{32,45})"""),
                new("WhatsApp-Business-API-Token", @"['""]?([a-zA-Z0-9_]+)['""]?\s*[=:]\s*['""]?(EAA[BDF][a-zA-Z0-9]{100,})['""]?"),
                new("Discord-Bot-Token", @"['""]?([a-zA-Z0-9_]+)['""]?\s*[=:]\s*['""]?([a-zA-Z0-9]{24}\.[a-zA-Z0-9_-]{6}\.[a-zA-Z0-9_-]{27})['""]?"),
                new("Facebook-User-Token", @"['""]?([a-zA-Z0-9_]+)['""]?\s*[=:]\s*['""]?((?:EAAD|EAAB)[a-zA-Z0-9]{211})['""]?"),
                new("Facebook-App-Token", @"['""]?([a-zA-Z0-9_]+)['""]?\s*[=:]\s*['""]?((?:EAAD|EAAB)[a-zA-Z0-9]{36})['""]?"),
                new("Zoom-Access-Token", @"['""]?([a-zA-Z0-9_]+)['""]?\s*[=:]\s*['""]?(eyJ[a-zA-Z0-9]{100,150})['""]?"),
                new("Slack-Bot-Token", @"['""]?([a-zA-Z0-9_]+)['""]?\s*[=:]\s*['""]?(xoxb-[a-zA-Z0-9]{12}-[a-zA-Z0-9]{13}-[a-zA-Z0-9]{24})['""]?"),
                new("Slack-User-Token", @"['""]?([a-zA-Z0-9_]+)['""]?\s*[=:]\s*['""]?(xoxp-[a-zA-Z0-9]{12}-[a-zA-Z0-9]{13}-[a-zA-Z0-9]{24})['""]?"),
                new("Slack-WorkspaceApp-Token", @"['""]?([a-zA-Z0-9_]+)['""]?\s*[=:]\s*['""]?(xapp-\d-[a-zA-Z0-9]{9,10}-[a-zA-Z0-9]{13}-[a-zA-Z0-9]{24})['""]?"),
            };
        }

        public List<SecretMatch> FindSecrets(string input)
        {
            var matches = new List<SecretMatch>();
            if (string.IsNullOrEmpty(input))
                return matches;

            var lines = input.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                foreach (var pattern in _patterns)
                {
                    var regexMatches = Regex.Matches(lines[i], pattern.RegexPattern);
                    foreach (Match match in regexMatches)
                    {
                        if (match.Groups.Count >= 3)
                        {
                            var secretMatch = new SecretMatch
                            {
                                Type = pattern.Name,
                                Value = match.Groups[2].Value,
                                VariableName = match.Groups[1].Value.Trim(),
                                LineNumber = i + 1,
                                Position = match.Index,
                                IsActive = false
                            };

                            if (pattern.Name == "Telegram-Token")
                            {
                                ValidateTelegramToken(secretMatch).GetAwaiter().GetResult();
                            }

                            matches.Add(secretMatch);
                        }
                    }
                }
            }
            return matches;
        }

        public List<SecretMatch> FindSecretsInFile(IFormFile file)
        {
            var matches = new List<SecretMatch>();
            try
            {
                _logger.LogInformation($"Сканирующийся файл: {file.FileName}, размер файла: {file.Length}");
                
                using var stream = new StreamReader(file.OpenReadStream());
                var content = stream.ReadToEnd();
                matches = FindSecrets(content);
                
                // Устанавливаем имя файла для всех найденных секретов
                foreach (var match in matches)
                {
                    match.FileName = file.FileName;
                }
                
                _logger.LogInformation($"Найдено {matches.Count} секретов в файле: {file.FileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при сканировании файла: {file.FileName}");
                throw;
            }
            return matches;
        }

        public async Task SaveSecretsToDatabaseAsync(List<SecretMatch> secrets, string source = "text")
        {
            if (secrets == null || !secrets.Any())
            {
                _logger.LogInformation("Нет секретов для сохранения в базу данных");
                return;
            }

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var insertSql = @"
                    INSERT INTO found_secrets (type, value, line_number, position, file_name, source)
                    VALUES (@type, @value, @line_number, @position, @file_name, @source)";

                foreach (var secret in secrets)
                {
                    using var insertCommand = new NpgsqlCommand(insertSql, connection);
                    insertCommand.Parameters.AddWithValue("@type", secret.Type);
                    insertCommand.Parameters.AddWithValue("@value", secret.Value);
                    insertCommand.Parameters.AddWithValue("@line_number", secret.LineNumber);
                    insertCommand.Parameters.AddWithValue("@position", secret.Position);
                    insertCommand.Parameters.AddWithValue("@file_name", 
                        string.IsNullOrEmpty(secret.FileName) ? DBNull.Value : secret.FileName);
                    insertCommand.Parameters.AddWithValue("@source", source);

                    await insertCommand.ExecuteNonQueryAsync();
                }

                _logger.LogInformation($"Успешно сохранено {secrets.Count} секретов в базу данных");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении секретов в базу данных");
                throw;
            }
        }

        public async Task<bool> CheckDatabaseConnection()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                _logger.LogInformation("Подключение к PostgreSQL успешно установлено");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка подключения к PostgreSQL");
                return false;
            }
        }
    
        private async Task InitializeDatabaseConnection()
        {
            try
            {
                var isConnected = await CheckDatabaseConnection();
                if (isConnected)
                {
                    _logger.LogInformation("База данных PostgreSQL подключена успешно");
                }
                else
                {
                    _logger.LogWarning("Не удалось подключиться к базе данных");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при инициализации подключения к базе данных");
            }
        }

        private async Task ValidateTelegramToken(SecretMatch secretMatch)
        {
            try
            {
                var url = $"https://api.telegram.org/bot{secretMatch.Value}/getMe";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(jsonResponse);
                    
                    if (document.RootElement.GetProperty("ok").GetBoolean())
                    {
                        var result = document.RootElement.GetProperty("result");
                        secretMatch.IsActive = true;
                        secretMatch.BotName = result.GetProperty("first_name").GetString()  ?? "BotName";
                        secretMatch.BotUsername = result.GetProperty("username").GetString() ?? "BotUsername";
                    }
                    else
                    {
                        secretMatch.IsActive = false;
                        secretMatch.ValidationError = "Токен недействителен";
                    }
                }
                else
                {
                    secretMatch.IsActive = false;
                    secretMatch.ValidationError = $"HTTP ошибка: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при проверке Telegram токена: {secretMatch.Value}");
                secretMatch.IsActive = false;
                secretMatch.ValidationError = "Ошибка при проверке токена";
            }
        }
    }

    public class Pattern
    {
        public string Name { get; }
        public string RegexPattern { get; }

        public Pattern(string name, string regexPattern)
        {
            Name = name;
            RegexPattern = regexPattern;
        }
    }

    public class SecretMatch
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string VariableName { get; set; } = string.Empty; 
        public int LineNumber { get; set; }
        public int Position { get; set; }
        public string FileName { get; set; } = string.Empty;
        
        // Дополнительные поля для Telegram токенов
        public bool IsActive { get; set; }
        public string BotName { get; set; } = string.Empty;
        public string BotUsername { get; set; } = string.Empty;
        public string ValidationError { get; set; } = string.Empty;
    }
}