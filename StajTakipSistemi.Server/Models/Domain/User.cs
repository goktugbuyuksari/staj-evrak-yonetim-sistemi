using System;
using System.Security.Cryptography;
using System.Text;

namespace StajTakipSistemi.Server.Models.Domain
{
    // OOP - SOYUTLAMA (Abstraction): Tüm kullanıcıların ortak özelliklerini barındıran soyut taban sınıf
    public abstract class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; protected set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // OOP - KAPSÜLLEME (Encapsulation): Hassas güvenlik verileri dışarıya kapalı tutulur (private/protected)
        protected string _passwordHash = string.Empty;
        protected string _passwordSalt = string.Empty;
        protected int _failedLoginAttempts = 0;
        protected bool _isLocked = false;
        protected DateTime? _lockoutEndTime;

        public string PasswordHash
        {
            get => _passwordHash;
            set => _passwordHash = value;
        }

        public string PasswordSalt
        {
            get => _passwordSalt;
            set => _passwordSalt = value;
        }

        public int FailedLoginAttempts
        {
            get => _failedLoginAttempts;
            set => _failedLoginAttempts = value;
        }

        public bool IsLocked
        {
            get => _isLocked;
            set => _isLocked = value;
        }

        public DateTime? LockoutEndTime
        {
            get => _lockoutEndTime;
            set => _lockoutEndTime = value;
        }

        // OOP - KAPSÜLLEME: Şifre doğrudan atanmaz; 128-bit rastgele Salt üretilip SHA-256 ile özetlenir
        public void SetPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Şifre boş olamaz.");

            byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
            _passwordSalt = Convert.ToBase64String(saltBytes);
            _passwordHash = HashPasswordWithSalt(password, _passwordSalt);
        }

        // OOP - KAPSÜLLEME: Şifre doğrulama işlemi sınıf içinde güvenle yürütülür
        public bool VerifyPassword(string inputPassword)
        {
            if (string.IsNullOrWhiteSpace(inputPassword) || string.IsNullOrEmpty(_passwordHash))
                return false;

            string computed = HashPasswordWithSalt(inputPassword, _passwordSalt);
            return _passwordHash == computed;
        }

        // OOP - KAPSÜLLEME & İŞ KURALI: 5 hatalı denemede hesap otomatik olarak kilitlenir
        public bool RecordFailedLoginAttempt()
        {
            _failedLoginAttempts++;
            if (_failedLoginAttempts >= 5)
            {
                _isLocked = true;
                _lockoutEndTime = DateTime.UtcNow.AddMinutes(15);
                return true;
            }
            return false;
        }

        public void ResetFailedAttempts()
        {
            _failedLoginAttempts = 0;
            _isLocked = false;
            _lockoutEndTime = null;
        }

        public void Unlock()
        {
            _isLocked = false;
            _failedLoginAttempts = 0;
            _lockoutEndTime = null;
        }

        private static string HashPasswordWithSalt(string password, string salt)
        {
            using var sha = SHA256.Create();
            byte[] combined = Encoding.UTF8.GetBytes(password + salt);
            byte[] hash = sha.ComputeHash(combined);
            return Convert.ToBase64String(hash);
        }

        // OOP - ÇOK BİÇİMLİLİK İÇİN SOYUT METOTLAR: Alt sınıflar bu metotları ezmek (override) zorundadır
        public abstract string GetUserTypeDescription();
        public abstract bool CanAccessAdminPanel();
    }
}
