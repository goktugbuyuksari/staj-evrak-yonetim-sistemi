using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Services.OOP_RuleEngine
{
    /// <summary>
    /// Doküman Bölüm 6.4:
    /// İşveren / İmza / Onay Alanları:
    /// Öğrenci onay kısmı: Boş olmalı
    /// Staj danışmanı onay kısmı: Boş olmalı
    /// İşveren kısmı: Tamamen dolu, kaşe + imza dahil
    /// </summary>
    public class EmployerApprovalValidator : BaseValidator
    {
        public override string RuleName => "İmza, Kaşe ve Onay Alanları Kontrolü";
        public override string Category => "Onay ve İmza Denetimi";

        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            // Öğrenci onay kısmı boş olmalı
            if (document.HasStudentSignature)
            {
                return Error(RuleName, Category,
                    "Başvuru aşamasındaki Ek-16 belgesinde 'Öğrenci Onay Kısmı' boş olmalıdır! Önceden imzalanmamalıdır.",
                    "Öğrenci onay imzasını kaldırınız.",
                    "HasStudentSignature");
            }

            // Staj danışmanı onay kısmı boş olmalı
            if (document.HasAdvisorSignature)
            {
                return Error(RuleName, Category,
                    "Başvuru aşamasındaki Ek-16 belgesinde 'Staj Danışmanı Onay Kısmı' boş olmalıdır!",
                    "Danışman imza alanını boş bırakınız.",
                    "HasAdvisorSignature");
            }

            // İşveren kısmı tamamen dolu, kaşe + imza dahil olmalıdır
            if (!document.HasEmployerSignatureAndStamp)
            {
                return Error(RuleName, Category,
                    "İşveren onay alanı eksik! Belgede işverenin onayı, yetkili imzası ve şirket kaşesi zorunludur.",
                    "Kurum yetkilisinden kaşe ve imza alarak belgeyi yeniden yükleyiniz.",
                    "HasEmployerSignatureAndStamp");
            }

            if (string.IsNullOrWhiteSpace(document.CompanyName))
            {
                return Error(RuleName, Category, "İşyeri / Kurum ticari unvanı boş bırakılamaz.", "Şirket adını yazınız.", "CompanyName");
            }

            return Success(RuleName, Category,
                "İmza ve onay alanları kurala uygundur (Öğrenci ve Danışman onayı boş, İşveren imza ve kaşesi tam).",
                "HasEmployerSignatureAndStamp");
        }
    }

    /// <summary>
    /// Doküman Bölüm 6.5:
    /// Mühendis Sayısı Kuralı:
    /// - Mühendis sayısı en az 1 olmalıdır.
    /// - Mühendis sayısı 5'in altında ise, mühendisin mezun olduğu bölümü soran uyarı kutucuğu gösterilir.
    /// </summary>
    public class EngineerCountValidator : BaseValidator
    {
        public override string RuleName => "Mühendis Sayısı ve Mezuniyet Kontrolü";
        public override string Category => "İşyeri Kriterleri";

        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            int count = document.EngineerCount;

            if (count < 1)
            {
                return Error(RuleName, Category,
                    "Staj yapılacak kurumda en az 1 mühendis bulunması zorunludur! Mevcut mühendis sayısı: 0",
                    "En az 1 mühendisin görev yaptığı bir kurum seçilmelidir.",
                    "EngineerCount");
            }

            if (count < 5)
            {
                // 5'in altında ise mezun olduğu bölüm uyarısı
                if (string.IsNullOrWhiteSpace(document.EngineerGraduationDepartment))
                {
                    return Warning(RuleName, Category,
                        $"Kurumdaki mühendis sayısı 5'in altındadır ({count} mühendis). Kural gereği kurumdaki mühendisin mezun olduğu bölümün belirtilmesi zorunludur.",
                        "Lütfen kurumdaki sorumlu mühendisin mezun olduğu bölümü belirtiniz (Örn: Bilgisayar Mühendisliği, Yazılım Mühendisliği).",
                        "EngineerGraduationDepartment");
                }

                return Success(RuleName, Category,
                    $"Mühendis sayısı ({count}) 5'in altında olduğu için ek mezuniyet alanı sorgulanmış ve doldurulmuştur: '{document.EngineerGraduationDepartment}'.",
                    "EngineerGraduationDepartment");
            }

            return Success(RuleName, Category, $"Kurumda {count} mühendis bulunmaktadır (Kriter başarıyla sağlandı).", "EngineerCount");
        }
    }
}
