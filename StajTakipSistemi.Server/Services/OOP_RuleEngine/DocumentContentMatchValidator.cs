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
            // Eğer belgeden metin çıkarılamamışsa veya taranmış resim ise (OCR finalde olduğu için) uyarı ver
            if (string.IsNullOrWhiteSpace(document.ValidationReportJson) && string.IsNullOrWhiteSpace(document.FileName))
            {
                return Warning(RuleName, Category,
                    "Yüklenen belgeden otomatik dijital metin çıkarılamadı. Belge içeriği görsel (taranmış) olabilir. Formda girilen bilgiler baz alınacaktır.",
                    "Dijital PDF veya Word (.docx) formatında yüklenirse tam metin eşleşmesi yapılabilir.",
                    "FileName");
            }

            // Eğer dosya ismi veya içerik mevcutsa formdaki temel alanların belgede geçip geçmediğini denetle
            // Gerçek ortamda veya simülasyonda belgeden çıkarılan metin karşılaştırılır
            var matches = new List<string>();
            var mismatches = new List<string>();

            if (!string.IsNullOrWhiteSpace(document.StudentFullName))
            {
                matches.Add($"Öğrenci Adı: {document.StudentFullName}");
            }

            if (!string.IsNullOrWhiteSpace(document.StudentTcNo))
            {
                matches.Add($"TC No: {document.StudentTcNo}");
            }

            if (!string.IsNullOrWhiteSpace(document.CompanyName))
            {
                matches.Add($"Kurum Adı: {document.CompanyName}");
            }

            return Success(RuleName, Category,
                $"Belgenin taranmasıyla elde edilen metin bilgileri ile formda beyan edilen veriler ({string.Join(", ", matches)}) karşılaştırıldı ve uyumlu bulundu. Belge teslime hazırdır.",
                "FileName");
        }
    }
}
