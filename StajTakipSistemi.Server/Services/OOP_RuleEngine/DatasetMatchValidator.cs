using System;
using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Services.OOP_RuleEngine
{
    // OOP - STRATEGY PATTERN: Form verilerini admin öğrenci veri setiyle karşılaştıran kural (Doküman 4.3 & 6)
    public class DatasetMatchValidator : BaseValidator
    {
        public override string RuleName => "Öğrenci Ana Veri Seti Eşleşme Kontrolü (Bölüm 4.3)";
        public override string Category => "Veri Seti Eşleşmesi";

        // İŞ KURALI: İsim, TC, bölüm ve telefon verileri veri setindeki orijinal kayıtla doğrulanır
        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            if (dbRecord == null)
            {
                return Warning(
                    RuleName,
                    Category,
                    $"'{document.StudentFullName}' (No: {document.StudentNumber}) için sistemde önceden yüklenmiş ana veri seti kaydı bulunamadı.",
                    "Admin panelinden öğrenci CSV veri setinin yüklendiğinden emin olunuz.",
                    "StudentNumber"
                );
            }

            // İsim karşılaştırması (büyük/küçük harf toleranslı)
            if (!string.Equals(document.StudentFullName?.Trim(), dbRecord.FullName?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return Warning(
                    RuleName,
                    Category,
                    $"Öğrenci adı formda '{document.StudentFullName}' girilmiş, ancak veri setinde '{dbRecord.FullName}' olarak kayıtlı.",
                    $"Doğru isim: {dbRecord.FullName}",
                    "StudentFullName"
                );
            }

            // T.C. Kimlik No kontrolü
            if (!string.Equals(document.StudentTcNo?.Trim(), dbRecord.TcNo?.Trim(), StringComparison.Ordinal))
            {
                return Error(
                    RuleName,
                    Category,
                    $"T.C. Kimlik No veri tabanı kaydıyla uyuşmuyor! (Form: {document.StudentTcNo}, DB: {dbRecord.TcNo})",
                    $"Doğru T.C.: {dbRecord.TcNo}",
                    "StudentTcNo"
                );
            }

            return Success(RuleName, Category, $"Belge verileri sistemdeki öğrenci veri seti kaydıyla ({dbRecord.FullName} - {dbRecord.StudentNumber}) tam uyuşmaktadır.");
        }
    }
}
