using System;
using System.Text.RegularExpressions;
using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Services.OOP_RuleEngine
{
    // OOP - STRATEGY PATTERN & SOYUTLAMA: Her bir kural bağımsız bir strateji arayüzünü uygular
    public interface IValidationRule
    {
        string RuleName { get; }
        string Category { get; }
        RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord);
    }

    // OOP - KALITIM & SOYUTLAMA: Tüm somut doğrulayıcı sınıflar için ortak şablon ve yardımcı metotlar sunar
    public abstract class BaseValidator : IValidationRule
    {
        public abstract string RuleName { get; }
        public abstract string Category { get; }

        // OOP - ÇOK BİÇİMLİLİK: Her kural kendi doğrulama mantığını bu metotta uygular
        public abstract RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord);

        protected static bool MatchRegex(string? input, string pattern)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;
            return Regex.IsMatch(input.Trim(), pattern, RegexOptions.IgnoreCase);
        }

        protected static RuleResult Success(string ruleName, string category, string message, string? field = null)
        {
            return new RuleResult
            {
                RuleName = ruleName,
                Category = category,
                IsValid = true,
                Severity = RuleSeverity.Success,
                Message = message,
                FieldName = field
            };
        }

        protected static RuleResult Warning(string ruleName, string category, string message, string? suggestion = null, string? field = null)
        {
            return new RuleResult
            {
                RuleName = ruleName,
                Category = category,
                IsValid = false,
                Severity = RuleSeverity.Warning,
                Message = message,
                Suggestion = suggestion,
                FieldName = field
            };
        }

        protected static RuleResult Error(string ruleName, string category, string message, string? suggestion = null, string? field = null)
        {
            return new RuleResult
            {
                RuleName = ruleName,
                Category = category,
                IsValid = false,
                Severity = RuleSeverity.Error,
                Message = message,
                Suggestion = suggestion,
                FieldName = field
            };
        }
    }
}
