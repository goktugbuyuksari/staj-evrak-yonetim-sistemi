using System;

namespace StajTakipSistemi.Server.Models.Domain
{
    // OOP - KALITIM (Inheritance): User taban sınıfından türetilen Yönetici sınıfı
    public class AdminUser : User
    {
        // Yöneticiye has özel alanlar
        public string DepartmentTitle { get; set; } = "Staj Komisyonu";
        public bool IsSuperAdmin { get; set; } = true;

        public AdminUser()
        {
            Role = "ROLE_ADMIN";
        }

        // OOP - ÇOK BİÇİMLİLİK (Polymorphism): Taban sınıftaki soyut metot yöneticiye göre ezilmiştir (override)
        public override string GetUserTypeDescription()
        {
            return $"Yönetici Hesabı: {FullName} ({DepartmentTitle})";
        }

        // OOP - ÇOK BİÇİMLİLİK: Yönetici paneline erişim izni verilir
        public override bool CanAccessAdminPanel()
        {
            return true;
        }
    }
}
