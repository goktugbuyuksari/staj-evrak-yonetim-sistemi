using System;
using System.Collections.Generic;

namespace StajTakipSistemi.Server.Services.OOP_RuleEngine
{
    // ENUM: Kural denetim ciddiyet seviyesi
    public enum RuleSeverity
    {
        Success,
        Warning,
        Error
    }

    // MODEL: Tek bir kuralın denetim sonucunu taşıyan sınıf
    public class RuleResult
    {
        public string RuleName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public RuleSeverity Severity { get; set; } = RuleSeverity.Success;
        public string Message { get; set; } = string.Empty;
        public string? Suggestion { get; set; }
        public string? FieldName { get; set; }
    }

    // MODEL: Tüm kuralların sonuçlarını ve genel değerlendirmeyi toplayan rapor sınıfı
    public class ValidationReport
    {
        public bool IsOverallValid { get; set; }
        public string FinalStatus { get; set; } = "İnceleniyor";
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public int PassedCount { get; set; }
        public List<RuleResult> Results { get; set; } = new List<RuleResult>();
        public List<string> DatasetMismatches { get; set; } = new List<string>();
        public string? SuggestedSubmissionDate { get; set; }
        public bool NeedsEngineerDepartmentPrompt { get; set; }
        public int DeclaredDays { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
