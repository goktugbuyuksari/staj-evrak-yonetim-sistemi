using System.Collections.Generic;

namespace StajTakipSistemi.Server.Services.OOP_RuleEngine
{
    public enum RuleSeverity
    {
        Success,
        Warning,
        Error
    }

    public class RuleResult
    {
        public string RuleName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public RuleSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Suggestion { get; set; }
        public string? FieldName { get; set; }
    }

    public class ValidationReport
    {
        public bool IsOverallValid { get; set; }
        public string FinalStatus { get; set; } = string.Empty;
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public int PassedCount { get; set; }
        public int CalculatedDays { get; set; }
        public int DeclaredDays { get; set; }
        public string? SuggestedSubmissionDate { get; set; }
        public bool NeedsEngineerDepartmentPrompt { get; set; }
        public List<RuleResult> Results { get; set; } = new List<RuleResult>();
        public List<string> DatasetMismatches { get; set; } = new List<string>();
    }
}
