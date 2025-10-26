using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Backend.Services
{
    public interface ISecretsFinder
    {
        List<SecretMatch> FindSecrets(string input);
        List<SecretMatch> FindSecretsInFile(IFormFile file);
    }
    
    public class SecretsFinder : ISecretsFinder
    {
        private readonly List<Pattern> _patterns;
        private readonly ILogger<SecretsFinder> _logger;

        public SecretsFinder(ILogger<SecretsFinder> logger)
        {
            _logger = logger;
            _patterns = new List<Pattern>
            {
                new("API-Key", @"\b([a-zA-Z0-9_]*?)\s*[=:]\s*['""]?([a-zA-Z0-9]{32,45})['""]?"),
                new("Telegram-Token", @"([a-zA-Z0-9_]+)\s*[=:]\s*['""]?(\d{8,10}:[\w_-]{35})['""]?"),
                new("JSON-Key-Value", @"""([a-zA-Z0-9_]+)""\s*[=:]\s*""([a-zA-Z0-9]{32,45})"""),
            };
        }

        public List<SecretMatch> FindSecrets(string input)
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
                            matches.Add(new SecretMatch
                            {
                                Type = pattern.Name,
                                Value = match.Groups[2].Value, // Сам токен
                                VariableName = match.Groups[1].Value.Trim(), // Название переменной
                                LineNumber = i + 1,
                                Position = match.Index
                            });
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
                                matches.Add(new SecretMatch
                                {
                                    Type = pattern.Name,
                                    Value = match.Groups[2].Value, // Сам токен
                                    VariableName = match.Groups[1].Value.Trim(), // Название переменной
                                    LineNumber = i + 1,
                                    Position = match.Index,
                                    FileName = file.FileName
                                });
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
    }
}