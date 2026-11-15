using System;
using System.Linq;
using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Services.OOP_RuleEngine
{
    /// <summary>
    /// NYP - KALITIM & SOYUTLAMA: Kimlik doğrulama için temel soyut sınıf.
    /// </summary>
    public abstract class IdentityValidator : BaseValidator
    {
        public override string Category => "Kimlik Doğrulama";
    }

    /// <summary>
    /// NYP - ÇOK BİÇİMLİLİK (Polymorphism):
    /// Doküman Bölüm 6.2: T.C. uyruklular için 11 hane, sadece rakam, ilk hane 0 olamaz ve resmi TC Kimlik Algoritması denetlenir.
    /// </summary>
    public class TcIdentityValidator : IdentityValidator
    {
        public override string RuleName => "TC Kimlik Algoritması Kontrolü";

        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            if (document.Nationality?.ToUpperInvariant() == "YABANCI")
            {
                // Yabancı öğrenci için bu kural atlanır
                return Success(RuleName, Category, "Öğrenci Yabancı uyruklu olduğu için TC algoritması muaf.", "StudentTcNo");
            }

            string tc = document.StudentTcNo?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(tc))
                return Error(RuleName, Category, "T.C. Kimlik Numarası boş bırakılamaz.", "11 haneli TC kimlik numaranızı giriniz.", "StudentTcNo");

            if (tc.Length != 11 || !tc.All(char.IsDigit))
                return Error(RuleName, Category, "T.C. Kimlik Numarası 11 haneli ve yalnızca rakamlardan oluşmalıdır.", "11 haneli sayı giriniz.", "StudentTcNo");

            if (tc[0] == '0')
                return Error(RuleName, Category, "T.C. Kimlik Numarasının ilk hanesi '0' olamaz.", "İlk hanesi 0 olmayan geçerli kimlik giriniz.", "StudentTcNo");

            // Resmi TC Kimlik Algoritması Kontrolü:
            // 1, 3, 5, 7, 9. haneler toplamı = tekler
            // 2, 4, 6, 8. haneler toplamı = ciftler
            // (tekler * 7 - ciftler) % 10 == 10. hane
            // ilk 10 hanenin toplamı % 10 == 11. hane
            int[] d = tc.Select(c => c - '0').ToArray();
            int oddSum = d[0] + d[2] + d[4] + d[6] + d[8];
            int evenSum = d[1] + d[3] + d[5] + d[7];

            int digit10 = ((oddSum * 7) - evenSum) % 10;
            if (digit10 < 0) digit10 += 10;

            int sumFirst10 = d.Take(10).Sum();
            int digit11 = sumFirst10 % 10;

            if (d[9] != digit10 || d[10] != digit11)
            {
                return Error(RuleName, Category, "T.C. Kimlik Numarası algoritma doğrulamasından geçemedi (Hatalı kimlik no).", "Nüfus cüzdanınızdaki 11 haneli geçerli TC Kimlik Numaranızı yazınız.", "StudentTcNo");
            }

            return Success(RuleName, Category, "T.C. Kimlik No format ve algoritma doğrulaması başarılı.", "StudentTcNo");
        }
    }

    /// <summary>
    /// NYP - ÇOK BİÇİMLİLİK (Polymorphism):
    /// Doküman Bölüm 6.2: Yabancı uyruklu öğrenciler için 11 hane ve numara 99 ile başlamalıdır.
    /// </summary>
    public class ForeignIdentityValidator : IdentityValidator
    {
        public override string RuleName => "Yabancı Kimlik Numarası Kontrolü";

        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            if (document.Nationality?.ToUpperInvariant() != "YABANCI")
            {
                // TC vatandaşı için bu kural atlanır
                return Success(RuleName, Category, "Öğrenci T.C. uyruklu olduğu için yabancı kimlik kontrolü muaf.", "StudentTcNo");
            }

            string id = document.StudentTcNo?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(id))
                return Error(RuleName, Category, "Yabancı Kimlik Numarası boş bırakılamaz.", "99 ile başlayan 11 haneli yabancı kimlik no giriniz.", "StudentTcNo");

            if (id.Length != 11 || !id.All(char.IsDigit))
                return Error(RuleName, Category, "Yabancı Kimlik Numarası 11 haneli ve yalnızca rakam olmalıdır.", "11 haneli sayı giriniz.", "StudentTcNo");

            if (!id.StartsWith("99"))
                return Error(RuleName, Category, "Yabancı Kimlik Numarası mutlaka '99' ile başlamalıdır.", "Numaranın 99 ile başladığından emin olunuz.", "StudentTcNo");

            return Success(RuleName, Category, "Yabancı Kimlik Numarası (99...) doğrulaması başarılı.", "StudentTcNo");
        }
    }
}
