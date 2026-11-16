using System;
using System.Text.RegularExpressions;
using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Services.OOP_RuleEngine
{
    // OOP - STRATEGY PATTERN: İkametgah adres bileşenlerini denetleyen kural sınıfı (Doküman 6.1)
    public class AddressFormatValidator : BaseValidator
    {
        public override string RuleName => "İkametgâh Adresi Format Kontrolü (Kısıt 6.1)";
        public override string Category => "Format";

        // İŞ KURALI: Adreste Mahalle, Cadde/Sokak, No, İlçe/İl bileşenlerinin varlığı sorgulanır
        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            string? addr = document.Address;
            if (string.IsNullOrWhiteSpace(addr))
            {
                return Error(RuleName, Category, "İkametgâh adresi boş bırakılamaz.", "Mahalle, Cadde/Sokak, No, İlçe/İl içerecek şekilde doldurunuz.", "Address");
            }

            bool hasMah = Regex.IsMatch(addr, @"(mah(\.|)|mahalle)", RegexOptions.IgnoreCase);
            bool hasCadSok = Regex.IsMatch(addr, @"(cad(\.|)|cadde|sok(\.|)|sokak|bulvar)", RegexOptions.IgnoreCase);
            bool hasNo = Regex.IsMatch(addr, @"(no|kapı|d(:|\.|\s)|daire|\d+)", RegexOptions.IgnoreCase);

            if (!hasMah || !hasCadSok || !hasNo)
            {
                return Warning(
                    RuleName, 
                    Category, 
                    "İkametgâh adresi eksik bileşenler içerebilir. (Mahalle, Cadde/Sokak, Bina No, Daire No, İlçe/İl belirtilmelidir.)",
                    "Örn: Atatürk Mah. Cumhuriyet Cad. No: 12 Daire: 4 İzmit / Kocaeli",
                    "Address"
                );
            }

            return Success(RuleName, Category, "İkametgâh adresi format kuralına uygundur.", "Address");
        }
    }

    // OOP - STRATEGY PATTERN: Telefon (+90 standardı) ve e-posta kurallarını denetleyen sınıf (Doküman 6.1)
    public class ContactInfoValidator : BaseValidator
    {
        public override string RuleName => "İletişim Bilgileri Format Kontrolü (Kısıt 6.1)";
        public override string Category => "Format";

        // İŞ KURALI: Telefon ve e-posta zorunlu; faks isteğe bağlıdır (nullable)
        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            if (string.IsNullOrWhiteSpace(document.PhoneNumber))
            {
                return Error(RuleName, Category, "Telefon numarası boş bırakılamaz.", "Telefon giriniz.", "PhoneNumber");
            }

            if (!MatchRegex(document.PhoneNumber, @"^(\+90|0)?\s*[1-9]\d{2}\s*\d{3}\s*\d{2}\s*\d{2}$"))
            {
                return Warning(
                    RuleName, 
                    Category, 
                    "Telefon numarası Türkiye standartlarında (+90 5XX XXX XX XX) formatında olmalıdır.",
                    "Örn: +90 532 111 22 33",
                    "PhoneNumber"
                );
            }

            if (string.IsNullOrWhiteSpace(document.Email))
            {
                return Error(RuleName, Category, "E-posta adresi boş bırakılamaz.", "E-posta giriniz.", "Email");
            }

            if (!MatchRegex(document.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                return Error(RuleName, Category, "Geçersiz e-posta adresi formatı.", "Örn: ogrenci@ogr.kocaeli.edu.tr", "Email");
            }

            return Success(RuleName, Category, "İletişim bilgileri kurala uygundur.", "PhoneNumber");
        }
    }
}
