using System;
using System.Text.RegularExpressions;
using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Services.OOP_RuleEngine
{
    /// <summary>
    /// NYP - SOYUTLAMA (Abstraction): Tüm iş kuralı ve doğrulama motorları bu arayüzü uygular.
    /// Kural motoru kuralların iç detaylarını bilmeden polimorfik olarak çalıştırır.
    /// </summary>
    public interface IValidationRule
    {
        string RuleName { get; }
        string Category { get; }
        RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord);
    }

    /// <summary>
    /// NYP - SOYUTLAMA (Abstraction) & KALITIM (Inheritance):
    /// Tüm somut doğrulayıcı sınıflar için ortak yardımcı metotlar ve taban şablon sağlar.
    /// </summary>
    public abstract class BaseValidator : IValidationRule
    {
        public abstract string RuleName { get; }
        public abstract string Category { get; }

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
