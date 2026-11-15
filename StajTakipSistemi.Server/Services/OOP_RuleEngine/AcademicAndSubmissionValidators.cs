using System;
using System.Text.RegularExpressions;
using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Services.OOP_RuleEngine
{
    /// <summary>
    /// Doküman Bölüm 6.3:
    /// Öğretim yılı tutarlılığı: Örn. "2025-2026 Bahar" şeklinde ardışık yıl olmalı,
    /// staj tarihleri de bu öğretim yılı ile uyumlu olmalıdır. Teslim edilen yarıyıl bilgisi yer almalıdır.
    /// </summary>
    public class AcademicTermValidator : BaseValidator
    {
        public override string RuleName => "Öğretim Yılı ve Yarıyıl Tutarlılığı Kontrolü";
        public override string Category => "Mantık ve Tarih Kontrolleri";

        private static readonly Regex TermRegex = new Regex(@"^(\d{4})-(\d{4})\s+(Güz|Bahar|Yaz)$", RegexOptions.IgnoreCase);

        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            string academicYear = document.AcademicYear?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(academicYear))
            {
                return Error(RuleName, Category, "Öğretim yılı ve yarıyıl bilgisi boş bırakılamaz.", "Örnek format: '2025-2026 Bahar' veya '2025-2026 Yaz'", "AcademicYear");
            }

            var match = TermRegex.Match(academicYear);
            if (!match.Success)
            {
                return Error(RuleName, Category, $"Öğretim yılı formatı geçersiz: '{academicYear}'. Beklenen format: 'YYYY-YYYY Dönem' (Örn: 2025-2026 Bahar)", "Örnek: 2025-2026 Bahar", "AcademicYear");
            }

            int startYear = int.Parse(match.Groups[1].Value);
            int endYear = int.Parse(match.Groups[2].Value);
            string term = match.Groups[3].Value;

            // Ardışık yıl kontrolü: 2025 ve 2026 ardışık olmalı (endYear == startYear + 1)
            if (endYear != startYear + 1)
            {
                return Error(RuleName, Category, $"Öğretim yılları ardışık olmalıdır: {startYear}-{endYear} ardışık değil.", $"Örnek: {startYear}-{startYear + 1} {term}", "AcademicYear");
            }

            // Staj başlangıç tarihinin öğretim yılı ile uyumu
            if (document.StartDate != default)
            {
                int internStartYear = document.StartDate.Year;
                // Staj genellikle startYear veya endYear içinde başlar
                if (internStartYear < startYear || internStartYear > endYear + 1)
                {
                    return Error(RuleName, Category, $"Staj başlangıç tarihi ({document.StartDate:dd.MM.yyyy}), beyan edilen öğretim yılı ({academicYear}) ile uyuşmuyor.", $"Staj tarihini {startYear}-{endYear} dönemine uygun seçiniz.", "StartDate");
                }
            }

            return Success(RuleName, Category, $"Öğretim yılı ({academicYear}) ardışık yıl ve staj tarihleri ile uyumludur.", "AcademicYear");
        }
    }

    /// <summary>
    /// Doküman Bölüm 6.3:
    /// Teslim tarihi: Belge, staj başlangıç tarihinden en az 30 gün önce teslim edilmelidir.
    /// Sistem uygun değilse önerilen teslim tarihini gösterir.
    /// </summary>
    public class SubmissionDateValidator : BaseValidator
    {
        public override string RuleName => "Teslim Tarihi ve 30 Gün Kuralı Kontrolü";
        public override string Category => "Mantık ve Tarih Kontrolleri";

        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            if (document.StartDate == default)
            {
                return Error(RuleName, Category, "Staj başlangıç tarihi belirtilmemiştir.", "Staj başlangıç tarihini seçiniz.", "StartDate");
            }

            DateTime submissionDate = document.SubmissionDate != default ? document.SubmissionDate.Date : DateTime.Today;
            DateTime startDate = document.StartDate.Date;

            // En az 30 gün önce kuralı: (startDate - submissionDate).TotalDays >= 30
            TimeSpan difference = startDate - submissionDate;
            int daysBefore = (int)difference.TotalDays;

            // Önerilen teslim tarihi: Staj başlangıcından 30 gün öncesi
            DateTime recommendedSubmissionDate = startDate.AddDays(-30);

            if (daysBefore < 30)
            {
                string recStr = recommendedSubmissionDate.ToString("dd.MM.yyyy");
                return Error(RuleName, Category,
                    $"Belge teslim tarihi ({submissionDate:dd.MM.yyyy}), staj başlangıç tarihinden ({startDate:dd.MM.yyyy}) yalnızca {daysBefore} gün öncedir! Kural gereği belge en az 30 gün önce teslim edilmelidir.",
                    $"Sistem Tarafından Önerilen En Geç Teslim Tarihi: {recStr}",
                    "SubmissionDate");
            }

            return Success(RuleName, Category,
                $"Teslim tarihi kuralı sağlandı. Belge staj başlangıcından {daysBefore} gün önce (en az 30 gün kuralına uygun) teslim edilmiştir.",
                "SubmissionDate");
        }
    }
}
