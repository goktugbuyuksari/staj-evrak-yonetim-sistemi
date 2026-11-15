using System;
using System.Text.RegularExpressions;
using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Services.OOP_RuleEngine
{
    /// <summary>
    /// Doküman Bölüm 6.1:
    /// Adres: Mahalle, Cadde/Sokak, Bina No, Daire No, İlçe/İl sırasıyla ve tam olmalıdır.
    /// </summary>
    public class AddressFormatValidator : BaseValidator
    {
        public override string RuleName => "Adres Format ve Sıralama Kontrolü";
        public override string Category => "Format Doğrulama";

        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            string address = document.Address?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(address))
                return Error(RuleName, Category, "İkametgâh adresi boş bırakılamaz.", "Mahalle, Cadde/Sokak, Bina No, Daire No, İlçe/İl sırasıyla yazınız.", "Address");

            // Kural: Mahalle, Cadde veya Sokak, No/Bina, Daire, İlçe/İl anahtar unsurları içermeli
            bool hasMahalle = Regex.IsMatch(address, @"(mah\.|mahalle|mahallesi)", RegexOptions.IgnoreCase);
            bool hasCaddeOrSokak = Regex.IsMatch(address, @"(cad\.|cadde|caddesi|sok\.|sokak|sokağı)", RegexOptions.IgnoreCase);
            bool hasBinaNo = Regex.IsMatch(address, @"(no|bina|kapı|numara)\s*[:.]?\s*\d+", RegexOptions.IgnoreCase) || Regex.IsMatch(address, @"\bno:\s*\d+", RegexOptions.IgnoreCase);
            bool hasDaireNo = Regex.IsMatch(address, @"(d\.|daire|iç kapı)\s*[:.]?\s*\d+", RegexOptions.IgnoreCase) || Regex.IsMatch(address, @"\bdaire:\s*\d+", RegexOptions.IgnoreCase);
            bool hasIlceIl = Regex.IsMatch(address, @"(/|-|,)\s*([a-zA-ZçğıöşüÇĞİÖŞÜ\s]+)$") || address.Contains("/");

            if (!hasMahalle)
                return Error(RuleName, Category, "Adres içerisinde 'Mahalle' (Mah.) bilgisi eksik.", "Örnek: Atatürk Mah. şeklinde mahalle bilgisini ekleyiniz.", "Address");

            if (!hasCaddeOrSokak)
                return Error(RuleName, Category, "Adres içerisinde 'Cadde' (Cad.) veya 'Sokak' (Sok.) bilgisi eksik.", "Örnek: Hürriyet Cad. veya Gül Sok. şeklinde belirtiniz.", "Address");

            if (!hasBinaNo)
                return Error(RuleName, Category, "Adres içerisinde 'Bina No' (No: X) eksik.", "Örnek: No: 15 şeklinde dış kapı/bina numarasını yazınız.", "Address");

            if (!hasDaireNo)
                return Error(RuleName, Category, "Adres içerisinde 'Daire No' (Daire: X) eksik.", "Örnek: Daire: 4 şeklinde daire numaranızı yazınız.", "Address");

            if (!hasIlceIl)
                return Warning(RuleName, Category, "Adres sonunda İlçe/İl ayrımı net değil.", "Örnek: İzmit / Kocaeli şeklinde ilçe ve il bilgisini belirtiniz.", "Address");

            return Success(RuleName, Category, "Adres formatı (Mahalle, Cadde/Sokak, Bina No, Daire No, İlçe/İl) tam ve eksiksiz.", "Address");
        }
    }

    /// <summary>
    /// Doküman Bölüm 6.1:
    /// Telefon: +90 xxx xxx xx xx formatında olmalıdır.
    /// E-posta: Geçerli e-posta formatında olmalıdır.
    /// E-posta + Telefon: İkisinin de bulunması zorunludur.
    /// Faks: Boş bırakılabilir (nullable).
    /// </summary>
    public class ContactInfoValidator : BaseValidator
    {
        public override string RuleName => "İletişim Bilgileri (Telefon, E-posta, Faks) Kontrolü";
        public override string Category => "Format Doğrulama";

        private static readonly Regex PhoneRegex = new Regex(@"^\+90\s[0-9]{3}\s[0-9]{3}\s[0-9]{2}\s[0-9]{2}$", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);

        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            string phone = document.PhoneNumber?.Trim() ?? string.Empty;
            string email = document.Email?.Trim() ?? string.Empty;

            // İkisinin de bulunması zorunludur
            if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(email))
            {
                return Error(RuleName, Category, "Hem Telefon hem de E-posta bilgisi eksik. İkisinin de bulunması zorunludur.", "Telefon (+90 xxx xxx xx xx) ve e-posta adresinizi giriniz.", "PhoneNumber");
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                return Error(RuleName, Category, "Telefon numarası eksik. Telefon girilmesi zorunludur.", "+90 xxx xxx xx xx formatında telefon giriniz.", "PhoneNumber");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                return Error(RuleName, Category, "E-posta adresi eksik. E-posta girilmesi zorunludur.", "ad@alanadi.uzanti formatında e-posta giriniz.", "Email");
            }

            // Telefon formatı: +90 xxx xxx xx xx
            if (!PhoneRegex.IsMatch(phone))
            {
                return Error(RuleName, Category, $"Telefon numarası geçersiz: '{phone}'. Beklenen format: +90 xxx xxx xx xx", "Örnek: +90 532 123 45 67 şeklinde yazınız.", "PhoneNumber");
            }

            // E-posta formatı
            if (!EmailRegex.IsMatch(email))
            {
                return Error(RuleName, Category, $"E-posta adresi geçersiz formattadır: '{email}'.", "Örnek: ogrenci@ogr.kocaeli.edu.tr şeklinde geçerli e-posta yazınız.", "Email");
            }

            // Faks kontrolü: Boş bırakılabilir (nullable)
            string faxMsg = string.IsNullOrWhiteSpace(document.FaxNumber)
                ? "Faks boş bırakılmıştır (İsteğe bağlı kural sağlandı)."
                : $"Faks girilmiştir: {document.FaxNumber}";

            return Success(RuleName, Category, $"Telefon (+90 formatı) ve E-posta doğrulaması başarılı. {faxMsg}", "PhoneNumber");
        }
    }
}
