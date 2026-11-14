using System;

namespace StajTakipSistemi.Server.Models.Domain
{
    /// <summary>
    /// NYP - KALITIM => User sınıfından türetilen Yönetici sınıfı.
    /// </summary>
    public class AdminUser : User
    {
        public string DepartmentTitle { get; set; } = "Staj Komisyonu";
        public bool IsSuperAdmin { get; set; } = true;

        public AdminUser()
        {
            Role = "ROLE_ADMIN";
        }

        // NYP - ÇOK BİÇİMLİLİK=> Taban sınıftaki soyut metot ezilmiştir.
        public override string GetUserTypeDescription()
        {
            return $"Yönetici Hesabı: {FullName} ({DepartmentTitle})";
        }

        public override bool CanAccessAdminPanel()
        {
            return true;
        }
    }
}
