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
        List<SecretMatch> FindSecretsInFilePath(string filePath);
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
                new("API-Key", @"\b[a-zA-Z0-9]{32,45}\b"),
                new("Telegram-Token", @"\d{8,10}:[\w_-]{35}")
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
                        matches.Add(new SecretMatch
                        {
                            Type = pattern.Name,
                            Value = match.Value,
                            LineNumber = i + 1,
                            Position = match.Index
                        });
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
                _logger.LogInformation($"Сканирующийся файл: {file.FileName}, размер файла: {file.Length} Б");
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
                            matches.Add(new SecretMatch
                            {
                                Type = pattern.Name,
                                Value = match.Value,
                                LineNumber = i+1,
                                Position = match.Index,
                                FileName = file.FileName
                            });
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
        public List<SecretMatch> FindSecretsInFilePath(string filePath)
        {
            var matches = new List<SecretMatch>();
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"Файл не найден: {filePath}");
                    return matches;
                }
                _logger.LogInformation($"Сканируется файл, находящийся: {filePath}");
                var lines = File.ReadAllLines(filePath);
                var fileName = Path.GetFileName(filePath);
                for (int i = 0; i < lines.Length; i++)
                {
                    foreach (var pattern in _patterns)
                    {
                        var regexMatches = Regex.Matches(lines[i], pattern.RegexPattern);
                        foreach (Match match in regexMatches)
                        {
                            matches.Add(new SecretMatch
                            {
                                Type = pattern.Name,
                                Value = match.Value,
                                LineNumber = i+1,
                                Position = match.Index,
                                FileName = fileName
                            });
                        }
                    }
                }
                _logger.LogInformation($"Найдено {matches.Count} секретов в файле: {fileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при сканировании файла, находящегося {filePath}");
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
        public int LineNumber { get; set; }
        public int Position { get; set; }
        public string FileName {get; set; } = string.Empty;
    }
}