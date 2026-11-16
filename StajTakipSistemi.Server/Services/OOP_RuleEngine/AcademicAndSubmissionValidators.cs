using System;
using System.Text.RegularExpressions;
using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Services.OOP_RuleEngine
{
    // OOP - STRATEGY PATTERN: Öğretim yılı ve dönemi formatını doğrulayan kural (Doküman 6.1)
    public class AcademicTermValidator : BaseValidator
    {
        public override string RuleName => "Öğretim Yılı ve Dönem Kontrolü (Kısıt 6.1)";
        public override string Category => "Akademik";

        // İŞ KURALI: 2025-2026 gibi ardışık yıl ve Güz/Bahar/Yaz dönemi kontrolü
        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            if (string.IsNullOrWhiteSpace(document.AcademicYear))
            {
                return Error(RuleName, Category, "Öğretim yılı ve dönemi boş bırakılamaz.", "Örn: 2025-2026 Bahar", "AcademicYear");
            }

            var match = Regex.Match(document.AcademicYear.Trim(), @"^(\d{4})\s*-\s*(\d{4})\s+(Güz|Bahar|Yaz)$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return Error(RuleName, Category, "Öğretim yılı formatı 'YYYY-YYYY Dönem' şeklinde olmalıdır.", "Örn: 2025-2026 Bahar", "AcademicYear");
            }

            int y1 = int.Parse(match.Groups[1].Value);
            int y2 = int.Parse(match.Groups[2].Value);
            if (y2 != y1 + 1)
            {
                return Error(RuleName, Category, "Öğretim yılı yılları ardışık olmalıdır (Örn: 2025-2026).", "Yılları ardışık giriniz.", "AcademicYear");
            }

            return Success(RuleName, Category, $"Öğretim yılı ve dönem kurala uygundur ({document.AcademicYear}).", "AcademicYear");
        }
    }

    // OOP - STRATEGY PATTERN: Staj başvurusunun 30 gün önceden yapılma kuralını doğrulayan sınıf (Doküman 6.2)
    public class SubmissionDateValidator : BaseValidator
    {
        public override string RuleName => "30 Gün Önceden Teslim Kuralı (Kısıt 6.2)";
        public override string Category => "Tarih";

        // İŞ KURALI: Teslim tarihi staj başlangıcından en az 30 gün önce olmalıdır
        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            if (document.StartDate == default)
            {
                return Error(RuleName, Category, "Staj başlangıç tarihi belirtilmelidir.", field: "StartDate");
            }

            DateTime submission = document.SubmissionDate != default ? document.SubmissionDate.Date : DateTime.Today;
            DateTime start = document.StartDate.Date;

            int daysDifference = (start - submission).Days;
            if (daysDifference < 30)
            {
                string suggestedDate = start.AddDays(-30).ToString("dd.MM.yyyy");
                return Error(
                    RuleName, 
                    Category, 
                    $"Staj evrakı staj başlangıç tarihinden en az 30 gün önce teslim edilmelidir! (Mevcut fark: {daysDifference} gün)", 
                    $"Önerilen en geç teslim tarihi: {suggestedDate}",
                    "SubmissionDate"
                );
            }

            return Success(RuleName, Category, $"Evrak teslim tarihi staj başlangıcından {daysDifference} gün öncedir (30 gün şartı sağlandı).", "SubmissionDate");
        }
    }
}
