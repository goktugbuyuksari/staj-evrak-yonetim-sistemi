using System;
using System.Collections.Generic;
using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Services.OOP_RuleEngine
{
    /// <summary>
    /// Doküman Bölüm 4.3:
    /// Öğrenci giriş yaptığında, yüklenen Ek-16'daki öğrenci bilgileri ile veritabanındaki (Admin tarafından yüklenen)
    /// bilgiler karşılaştırılır. Uyuşmazlık varsa raporda açıkça belirtilir.
    /// </summary>
    public class DatasetMatchValidator : BaseValidator
    {
        public override string RuleName => "Veritabanı Öğrenci Bilgileri Çapraz Karşılaştırması";
        public override string Category => "Veri Doğrulama";

        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            if (dbRecord == null)
            {
                return Warning(RuleName, Category,
                    "Giriş yapan öğrencinin admin tarafından yüklenen ana veri setinde kaydı bulunamadı. Karşılaştırma yapılamadı.",
                    "Admin panelinden öğrenci test veri setinin yüklendiğinden emin olunuz.",
                    "StudentNumber");
            }

            var mismatches = new List<string>();

            // Ad Soyad karşılaştırması
            if (!string.Equals(document.StudentFullName?.Trim(), dbRecord.FullName?.Trim(), StringComparison.InvariantCultureIgnoreCase))
            {
                mismatches.Add($"Ad Soyad Uyuşmazlığı: Belgedeki '{document.StudentFullName}' != DB'deki '{dbRecord.FullName}'");
            }

            // TC Kimlik No karşılaştırması
            if (!string.Equals(document.StudentTcNo?.Trim(), dbRecord.TcNo?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                mismatches.Add($"TC/Kimlik No Uyuşmazlığı: Belgedeki '{document.StudentTcNo}' != DB'deki '{dbRecord.TcNo}'");
            }

            // Öğrenci Numarası karşılaştırması
            if (!string.IsNullOrWhiteSpace(document.StudentNumber) &&
                !string.Equals(document.StudentNumber?.Trim(), dbRecord.StudentNumber?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                mismatches.Add($"Öğrenci No Uyuşmazlığı: Belgedeki '{document.StudentNumber}' != DB'deki '{dbRecord.StudentNumber}'");
            }

            // Telefon karşılaştırması (boşluksuz normalize ederek)
            string docPhone = NormalizePhone(document.PhoneNumber);
            string dbPhone = NormalizePhone(dbRecord.PhoneNumber);
            if (!string.IsNullOrEmpty(docPhone) && !string.IsNullOrEmpty(dbPhone) && docPhone != dbPhone)
            {
                mismatches.Add($"Telefon No Uyuşmazlığı: Belgedeki '{document.PhoneNumber}' != DB'deki '{dbRecord.PhoneNumber}'");
            }

            // E-posta karşılaştırması
            if (!string.IsNullOrWhiteSpace(document.Email) &&
                !string.Equals(document.Email?.Trim(), dbRecord.Email?.Trim(), StringComparison.InvariantCultureIgnoreCase))
            {
                mismatches.Add($"E-posta Uyuşmazlığı: Belgedeki '{document.Email}' != DB'deki '{dbRecord.Email}'");
            }

            // Bölüm karşılaştırması
            if (!string.IsNullOrWhiteSpace(document.Department) &&
                !string.Equals(document.Department?.Trim(), dbRecord.Department?.Trim(), StringComparison.InvariantCultureIgnoreCase))
            {
                mismatches.Add($"Bölüm Uyuşmazlığı: Belgedeki '{document.Department}' != DB'deki '{dbRecord.Department}'");
            }

            if (mismatches.Count > 0)
            {
                string details = string.Join(" | ", mismatches);
                return Error(RuleName, Category,
                    $"Ek-16 belgesindeki bilgiler ile üniversite sistemindeki kayıtlar arasında {mismatches.Count} adet uyuşmazlık bulundu! [{details}]",
                    "Belgedeki öğrenci bilgilerini resmi kayıtlarınıza uygun olacak şekilde düzeltiniz.",
                    "StudentNumber");
            }

            return Success(RuleName, Category,
                "Belgedeki öğrenci bilgileri (Ad Soyad, TC No, Öğrenci No, Bölüm, İletişim) üniversite veri tabanı ile %100 uyuşmaktadır.",
                "StudentNumber");
        }

        private static string NormalizePhone(string? p)
        {
            if (string.IsNullOrWhiteSpace(p)) return string.Empty;
            return new string(p.Where(char.IsDigit).ToArray());
        }
    }
}
