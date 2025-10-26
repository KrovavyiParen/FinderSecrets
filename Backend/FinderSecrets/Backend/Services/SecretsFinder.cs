using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Backend.Services
{
    public interface ISecretsFinder
    {
        Task<List<SecretMatch>> FindSecrets(string input);
        Task<List<SecretMatch>> FindSecretsInFile(IFormFile file);
    }
    
    public class SecretsFinder : ISecretsFinder
    {
        private readonly List<Pattern> _patterns;
        private readonly ILogger<SecretsFinder> _logger;
        private readonly HttpClient _httpClient;

        public SecretsFinder(ILogger<SecretsFinder> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _patterns = new List<Pattern>
            {
                new("API-Key", @"\b([a-zA-Z0-9_]*?)\s*[=:]\s*['""]?([a-zA-Z0-9]{32,45})['""]?"),
                new("Telegram-Token", @"([a-zA-Z0-9_]+)\s*[=:]\s*['""]?(\d{8,10}:[\w_-]{35})['""]?"),
                new("JSON-Key-Value", @"""([a-zA-Z0-9_]+)""\s*[=:]\s*""([a-zA-Z0-9]{32,45})"""),
            };
        }

        public async Task<List<SecretMatch>> FindSecrets(string input)
        {
            var matches = new List<SecretMatch>();
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
                                Position = match.Index
                            };

                            // Проверяем только Telegram токены
                            if (pattern.Name == "Telegram-Token")
                            {
                                await ValidateTelegramToken(secretMatch);
                            }

                            matches.Add(secretMatch);
                        }
                    }
                }
            }
            return matches;
        }

        public async Task<List<SecretMatch>> FindSecretsInFile(IFormFile file)
        {
            var matches = new List<SecretMatch>();
            try
            {
                _logger.LogInformation($"Сканирующийся файл: {file.FileName}, размер файла: {file.Length}");
                using var stream = new StreamReader(file.OpenReadStream());
                var content = await stream.ReadToEndAsync();
                var lines = content.Split('\n');
                
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
                                    FileName = file.FileName
                                };

                                // Проверяем только Telegram токены
                                if (pattern.Name == "Telegram-Token")
                                {
                                    await ValidateTelegramToken(secretMatch);
                                }

                                matches.Add(secretMatch);
                            }
                        }
                    }
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
                        secretMatch.BotName = result.GetProperty("first_name").GetString();
                        secretMatch.BotUsername = result.GetProperty("username").GetString();
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