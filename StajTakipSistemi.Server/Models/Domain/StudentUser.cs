using System;

namespace StajTakipSistemi.Server.Models.Domain
{
    // OOP - KALITIM (Inheritance): User taban sınıfından türetilen Öğrenci sınıfı
    public class StudentUser : User
    {
        // Öğrenciye has özel alanlar
        public string StudentNumber { get; set; } = string.Empty;
        public string TcNo { get; set; } = string.Empty;
        public string Nationality { get; set; } = "TC";
        public string Department { get; set; } = string.Empty;
        public int CompletedInternshipDays { get; set; } = 0;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ResidentialAddress { get; set; } = string.Empty;
        public string HealthInsuranceStatus { get; set; } = "Var";

        public StudentUser()
        {
            Role = "ROLE_STUDENT";
        }

        // OOP - ÇOK BİÇİMLİLİK (Polymorphism): Taban sınıftaki soyut metot öğrenciye göre ezilmiştir (override)
        public override string GetUserTypeDescription()
        {
            return $"Öğrenci Hesabı: {FullName} (No: {StudentNumber}, Bölüm: {Department})";
        }

        // OOP - ÇOK BİÇİMLİLİK: Öğrencinin yönetici paneline erişimi engellenir
        public override bool CanAccessAdminPanel()
        {
            return false;
        }
    }
}
