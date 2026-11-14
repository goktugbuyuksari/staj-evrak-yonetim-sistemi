using System;

namespace StajTakipSistemi.Server.Models.Domain
{
    /// <summary>
    /// NYP - KALITIM => User taban sınıfından türetilmiştir.
    /// Öğrenciye ait özel kimlik ve staj bilgileri kapsüllenir.
    /// </summary>
    public class StudentUser : User
    {
        public string StudentNumber { get; set; } = string.Empty;
        public string TcNo { get; set; } = string.Empty;
        public string Nationality { get; set; } = "TC"; // TC veya Yabancı
        public string Department { get; set; } = string.Empty;
        public int CompletedInternshipDays { get; set; } = 0;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ResidentialAddress { get; set; } = string.Empty;
        public string HealthInsuranceStatus { get; set; } = "Var";

        public StudentUser()
        {
            Role = "ROLE_STUDENT";
        }

        // NYP - ÇOK BİÇİMLİLİK=> Taban sınıftaki soyut metot ezilmiştir.
        public override string GetUserTypeDescription()
        {
            return $"Öğrenci Hesabı: {FullName} (No: {StudentNumber}, Bölüm: {Department})";
        }

        public override bool CanAccessAdminPanel()
        {
            return false;
        }
    }
}
