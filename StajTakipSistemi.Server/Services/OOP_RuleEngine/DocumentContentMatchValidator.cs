using System;
using System.Collections.Generic;
using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Services.OOP_RuleEngine
{
    /// <summary>
    /// Hoca Soru-Cevap 1. Madde:
    /// "Belgenin taranmasıyla elde edilen bilgiler ile kullanıcının sisteme girdiği bilgiler
    /// karşılaştırılarak belgenin teslime hazır olup olmadığı kontrol edilmelidir."
    /// </summary>
    public class DocumentContentMatchValidator : BaseValidator
    {
        public override string RuleName => "Belge İçi Metin ile Form Beyanı Karşılaştırması (Hoca 1. Madde)";
        public override string Category => "Belge & Form Eşleşmesi";

        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            var text = document.ExtractedText ?? string.Empty;
            var matches = new List<string>();
            var mismatches = new List<string>();

            // 1. Öğrenci Adı Soyadı Kontrolü
            if (!string.IsNullOrWhiteSpace(document.StudentFullName))
            {
                if (text.IndexOf(document.StudentFullName, StringComparison.OrdinalIgnoreCase) >= 0)
                    matches.Add($"Öğrenci Adı: {document.StudentFullName}");
                else
                    mismatches.Add($"Öğrenci Adı ({document.StudentFullName})");
            }

            // 2. TC Kimlik No Kontrolü
            if (!string.IsNullOrWhiteSpace(document.StudentTcNo))
            {
                if (text.Contains(document.StudentTcNo))
                    matches.Add($"TC No: {document.StudentTcNo}");
                else
                    mismatches.Add($"TC No ({document.StudentTcNo})");
            }

            // 3. Öğrenci No Kontrolü
            if (!string.IsNullOrWhiteSpace(document.StudentNumber))
            {
                if (text.Contains(document.StudentNumber))
                    matches.Add($"Öğrenci No: {document.StudentNumber}");
                else
                    mismatches.Add($"Öğrenci No ({document.StudentNumber})");
            }

            // 4. Kurum Adı Kontrolü
            if (!string.IsNullOrWhiteSpace(document.CompanyName))
            {
                if (text.IndexOf(document.CompanyName, StringComparison.OrdinalIgnoreCase) >= 0)
                    matches.Add($"Kurum Adı: {document.CompanyName}");
                else
                    mismatches.Add($"Kurum Adı ({document.CompanyName})");
            }

            if (mismatches.Count > 0)
            {
                return Warning(RuleName, Category,
                    $"Belgeden taranan metin ile formda beyan edilen veriler arasında uyuşmazlıklar tespit edildi: [{string.Join(", ", mismatches)}] belge metninde bulunamadı.",
                    "Formda girdiğiniz bilgilerin yüklenen Ek-16 belgesiyle birebir eşleştiğinden emin olunuz.",
                    "ExtractedText");
            }

            return Success(RuleName, Category,
                $"Belgenin taranmasıyla elde edilen dijital metin bilgileri ile formda beyan edilen veriler ({string.Join(", ", matches)}) birebir eşleşti ve doğrulandı. Belge teslime hazırdır.",
                "ExtractedText");
        }
    }
}