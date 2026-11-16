using System;
using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Services.OOP_RuleEngine
{
    // OOP - STRATEGY PATTERN: İşveren, danışman ve öğrenci imza/onay durumunu denetleyen kural (Doküman 6.4)
    public class EmployerApprovalValidator : BaseValidator
    {
        public override string RuleName => "İşveren Onay ve İmza Kontrolü (Kısıt 6.4)";
        public override string Category => "Onay";

        // İŞ KURALI: İşveren imza/kaşesi zorunludur; öğrenci ve danışman imzası başvuru anında boş olmalıdır
        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            if (!document.HasEmployerSignatureAndStamp)
            {
                return Error(
                    RuleName, 
                    Category, 
                    "Kurum/İşveren yetkilisinin imza ve kaşesi olmadan Ek-16 staj belgesi kabul edilemez!", 
                    "Belgenin kurum yetkilisi tarafından kaşelenip imzalandığından emin olunuz.",
                    "HasEmployerSignatureAndStamp"
                );
            }

            if (document.HasStudentSignature)
            {
                return Warning(
                    RuleName, 
                    Category, 
                    "Öğrenci imza alanı işaretli. Ek-16 teslim aşamasında öğrenci imzasının daha sonra atılması tavsiye edilir.",
                    suggestion: null,
                    field: "HasStudentSignature"
                );
            }

            if (document.HasAdvisorSignature)
            {
                return Warning(
                    RuleName, 
                    Category, 
                    "Bölüm staj komisyonu/danışman onay alanı işaretli. Bu alan teslim anında boş olmalıdır.",
                    "Danışman imza alanını boş bırakınız.",
                    "HasAdvisorSignature"
                );
            }

            return Success(RuleName, Category, "İşveren onay ve imza kuralı başarıyla sağlandı.", "HasEmployerSignatureAndStamp");
        }
    }

    // OOP - STRATEGY PATTERN: Kurumdaki mühendis sayısını ve mezuniyet alanını denetleyen kural (Doküman 6.5)
    public class EngineerCountValidator : BaseValidator
    {
        public override string RuleName => "Kurum Mühendis Sayısı ve Bölüm Kuralı (Kısıt 6.5)";
        public override string Category => "Kurum";

        // İŞ KURALI: Mühendis sayısı en az 1 olmalı; 5'in altındaysa mühendisin mezun olduğu bölüm zorunludur
        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            if (document.EngineerCount < 1)
            {
                return Error(
                    RuleName, 
                    Category, 
                    "Staj yapılacak kurumda en az 1 mühendis bulunmalıdır!", 
                    "Kurumdaki mühendis sayısını belirtiniz.",
                    "EngineerCount"
                );
            }

            if (document.EngineerCount < 5)
            {
                if (string.IsNullOrWhiteSpace(document.EngineerGraduationDepartment))
                {
                    return Error(
                        RuleName, 
                        Category, 
                        "Kurumdaki mühendis sayısı 5'in altında olduğu için mühendisin mezun olduğu bölümün belirtilmesi zorunludur! (Kural 6.5)", 
                        "Örn: Bilgisayar Mühendisliği veya Yazılım Mühendisliği",
                        "EngineerGraduationDepartment"
                    );
                }

                return Warning(
                    RuleName, 
                    Category, 
                    $"Kurumdaki mühendis sayısı 5'in altında ({document.EngineerCount}). Mühendis mezuniyet bölümü: {document.EngineerGraduationDepartment}.",
                    "Staj komisyonunun mühendislik alanı uygunluğunu onaylaması gerekmektedir.",
                    "EngineerCount"
                );
            }

            return Success(RuleName, Category, $"Kurumda {document.EngineerCount} mühendis bulunmaktadır (Kural 6.5 sağlandı).", "EngineerCount");
        }
    }
}
