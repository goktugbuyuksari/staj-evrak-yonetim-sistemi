using System;
using System.Collections.Generic;
using System.Linq;
using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Services.OOP_RuleEngine
{
    public interface IDocumentValidationEngine
    {
        ValidationReport ValidateDocument(InternshipDocument document, StudentDatasetRecord? dbRecord);
    }

    /// <summary>
    /// NYP - ÇOK BİÇİMLİLİK (Polymorphism) & SOYUTLAMA (Abstraction) Kural Motoru:
    /// Birbirinden farklı doğrulayıcı sınıfları IValidationRule listesi üzerinden
    /// tip bağımsız olarak polimorfik biçimde çalıştırır ve tek bir standart rapor üretir.
    /// </summary>
    public class DocumentValidationEngine : IDocumentValidationEngine
    {
        private readonly List<IValidationRule> _rules;
        private readonly IHolidayService _holidayService;

        public DocumentValidationEngine(IHolidayService holidayService)
        {
            _holidayService = holidayService;
            _rules = new List<IValidationRule>
            {
                new TcIdentityValidator(),
                new ForeignIdentityValidator(),
                new AddressFormatValidator(),
                new ContactInfoValidator(),
                new AcademicTermValidator(),
                new SubmissionDateValidator(),
                new InternshipWorkDaysValidator(_holidayService),
                new EmployerApprovalValidator(),
                new EngineerCountValidator(),
                new DatasetMatchValidator(),
                new DocumentContentMatchValidator()
            };
        }

        public ValidationReport ValidateDocument(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            var report = new ValidationReport
            {
                DeclaredDays = document.DeclaredDaysCount
            };

            // Önerilen teslim tarihini hesapla (Staj başlangıcından 30 gün öncesi)
            if (document.StartDate != default)
            {
                report.SuggestedSubmissionDate = document.StartDate.AddDays(-30).ToString("dd.MM.yyyy");
            }

            // Mühendis sayısı kontrolü
            report.NeedsEngineerDepartmentPrompt = document.EngineerCount < 5;

            // NYP - ÇOK BİÇİMLİLİK: Her bir kural IValidationRule arayüzü üzerinden polimorfik olarak çağrılır
            foreach (var rule in _rules)
            {
                var result = rule.Evaluate(document, dbRecord);
                report.Results.Add(result);

                if (result.Severity == RuleSeverity.Error)
                {
                    report.ErrorCount++;
                    if (rule is DatasetMatchValidator)
                    {
                        report.DatasetMismatches.Add(result.Message);
                    }
                }
                else if (result.Severity == RuleSeverity.Warning)
                {
                    report.WarningCount++;
                }
                else
                {
                    report.PassedCount++;
                }
            }

            // Sonuç değerlendirmesi (Doküman Bölüm 5: Belge Statü Akışı)
            report.IsOverallValid = report.ErrorCount == 0;
            if (report.IsOverallValid)
            {
                // Hata yoksa belge onaylanır ve Tamamlandı aşamasına geçebilir
                report.FinalStatus = DocumentStatuses.Tamamlandi;
            }
            else
            {
                // Hata veya eksik varsa statü Hatalı / Eksik olur
                report.FinalStatus = DocumentStatuses.HataliEksik;
            }

            return report;
        }
    }
}
