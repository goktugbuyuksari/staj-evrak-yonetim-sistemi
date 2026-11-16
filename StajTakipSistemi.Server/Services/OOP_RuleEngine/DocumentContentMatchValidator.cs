using System;
using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Services.OOP_RuleEngine
{
    // OOP - STRATEGY PATTERN & ÖN-OCR: Form verilerinin yüklenen belgenin metninde geçip geçmediğini denetler
    public class DocumentContentMatchValidator : BaseValidator
    {
        public override string RuleName => "Belge Metin / Ön-OCR İçerik Eşleşme Kontrolü (Hoca Soru 1)";
        public override string Category => "Metin Çıkarma";

        // İŞ KURALI: Öğrenci adı, TC, öğrenci no ve kurum adı belgeden çıkarılan metin içinde aranır
        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            if (string.IsNullOrWhiteSpace(document.ExtractedText))
            {
                return Warning(
                    RuleName,
                    Category,
                    "Yüklenen belgeden metin ayıklanamadı veya belge taranmış/görsel formatta.",
                    "Word (.docx) veya metin içeren PDF yükleyerek anlık içerik doğrulamasından yararlanabilirsiniz.",
                    "ExtractedText"
                );
            }

            string text = document.ExtractedText.ToLowerInvariant();
            int matchedTokens = 0;
            int totalTokens = 0;

            // Öğrenci Adı token kontrolü
            if (!string.IsNullOrWhiteSpace(document.StudentFullName))
            {
                totalTokens++;
                if (text.Contains(document.StudentFullName.ToLowerInvariant()))
                {
                    matchedTokens++;
                }
            }

            // TC Kimlik No kontrolü
            if (!string.IsNullOrWhiteSpace(document.StudentTcNo))
            {
                totalTokens++;
                if (text.Contains(document.StudentTcNo.ToLowerInvariant()))
                {
                    matchedTokens++;
                }
            }

            // Öğrenci Numarası kontrolü
            if (!string.IsNullOrWhiteSpace(document.StudentNumber))
            {
                totalTokens++;
                if (text.Contains(document.StudentNumber.ToLowerInvariant()))
                {
                    matchedTokens++;
                }
            }

            // Kurum Adı kontrolü
            if (!string.IsNullOrWhiteSpace(document.CompanyName))
            {
                totalTokens++;
                if (text.Contains(document.CompanyName.ToLowerInvariant()))
                {
                    matchedTokens++;
                }
            }

            if (totalTokens > 0 && matchedTokens == totalTokens)
            {
                return Success(
                    RuleName,
                    Category,
                    $"Yüklenen belgenin metin içeriğinde girilen tüm form bilgileri ({matchedTokens}/{totalTokens}) başarıyla tespit edildi.",
                    "ExtractedText"
                );
            }

            return Warning(
                RuleName,
                Category,
                $"Belge metninde {matchedTokens}/{totalTokens} alan eşleşti. Bazı alanlar görsel/imza şeklinde olabilir.",
                "Belgenin net ve okunabilir bir kopyasını yüklediğinizden emin olunuz.",
                "ExtractedText"
            );
        }
    }
}
