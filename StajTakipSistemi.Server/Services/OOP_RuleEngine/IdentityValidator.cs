using System;
using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Services.OOP_RuleEngine
{
    // OOP - KALITIM & SOYUTLAMA: Kimlik doğrulayıcı sınıflar için ara soyut sınıf
    public abstract class IdentityValidator : BaseValidator
    {
        public override string Category => "Kimlik";
    }

    // OOP - STRATEGY PATTERN: T.C. Vatandaşları için 11 haneli resmi algoritma denetimi (Doküman 6.1)
    public class TcIdentityValidator : IdentityValidator
    {
        public override string RuleName => "T.C. Kimlik Numarası Algoritma Doğrulaması (Kısıt 6.1)";

        // İŞ KURALI: 11 hane, ilk hane 0 olamaz, 10. ve 11. hane mod 10 matematiksel kurallarına uymalıdır
        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            if (document.Nationality?.ToUpperInvariant() != "TC")
            {
                return Success(RuleName, Category, "Öğrenci T.C. vatandaşı olmadığı için T.C. algoritması atlandı.");
            }

            string? tc = document.StudentTcNo;
            if (string.IsNullOrWhiteSpace(tc))
            {
                return Error(RuleName, Category, "T.C. Kimlik numarası boş bırakılamaz.", "11 haneli T.C. kimlik no giriniz.", "StudentTcNo");
            }

            tc = tc.Trim();
            if (tc.Length != 11 || !long.TryParse(tc, out _))
            {
                return Error(RuleName, Category, "T.C. Kimlik numarası tam 11 haneli rakamlardan oluşmalıdır.", field: "StudentTcNo");
            }

            if (tc[0] == '0')
            {
                return Error(RuleName, Category, "T.C. Kimlik numarası '0' ile başlayamaz.", field: "StudentTcNo");
            }

            int[] digits = new int[11];
            for (int i = 0; i < 11; i++) digits[i] = tc[i] - '0';

            int oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
            int evenSum = digits[1] + digits[3] + digits[5] + digits[7];

            int calculatedD10 = ((oddSum * 7) - evenSum) % 10;
            if (calculatedD10 < 0) calculatedD10 += 10;

            if (digits[9] != calculatedD10)
            {
                return Error(RuleName, Category, "T.C. Kimlik numarasının 10. hanesi resmi doğrulama algoritmasına uymuyor!", field: "StudentTcNo");
            }

            int totalFirst10 = 0;
            for (int i = 0; i < 10; i++) totalFirst10 += digits[i];
            int calculatedD11 = totalFirst10 % 10;

            if (digits[10] != calculatedD11)
            {
                return Error(RuleName, Category, "T.C. Kimlik numarasının 11. hanesi resmi doğrulama algoritmasına uymuyor!", field: "StudentTcNo");
            }

            return Success(RuleName, Category, "T.C. Kimlik numarası resmi algoritmaya tam uygundur.", "StudentTcNo");
        }
    }

    // OOP - STRATEGY PATTERN: Yabancı uyruklu öğrenciler için kimlik denetimi
    public class ForeignIdentityValidator : IdentityValidator
    {
        public override string RuleName => "Yabancı Uyruklu Kimlik Doğrulaması (Kısıt 6.1)";

        // İŞ KURALI: Yabancı kimlik numarası 99 ile başlamalı ve 11 haneli olmalıdır
        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            if (document.Nationality?.ToUpperInvariant() == "TC")
            {
                return Success(RuleName, Category, "Öğrenci T.C. vatandaşı olduğu için yabancı kimlik kontrolü atlandı.");
            }

            string? idNo = document.StudentTcNo;
            if (string.IsNullOrWhiteSpace(idNo))
            {
                return Error(RuleName, Category, "Yabancı kimlik numarası boş bırakılamaz.", field: "StudentTcNo");
            }

            idNo = idNo.Trim();
            if (idNo.Length != 11 || !idNo.StartsWith("99"))
            {
                return Warning(
                    RuleName, 
                    Category, 
                    "Yabancı uyruklu öğrencilerin kimlik numarası genellikle '99' ile başlayan 11 haneli numaradır.", 
                    "Örn: 99XXXXXXXXX", 
                    "StudentTcNo"
                );
            }

            return Success(RuleName, Category, "Yabancı kimlik numarası kurala uygundur.", "StudentTcNo");
        }
    }
}
