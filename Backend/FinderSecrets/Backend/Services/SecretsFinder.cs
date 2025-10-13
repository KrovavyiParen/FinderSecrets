using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.IO;

namespace Backend
{
    public class SecretsFinder
    {
        private readonly List<Pattern> _patterns;

        public SecretsFinder()
        {
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

    }
}