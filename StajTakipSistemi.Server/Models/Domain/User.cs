using System;
using System.Security.Cryptography;
using System.Text;

namespace StajTakipSistemi.Server.Models.Domain
{
    /// <summary>
    /// NYP - KAPSÜLLEME  ve SOYUTLAMA Örneği:
    /// Kullanıcı verileri private alanlarda saklanır ve sadece iş kuralları barındıran metotlarla yönetilir.
    /// Şifreler hash'lenerek saklanır ve doğrudan dışarıdan değiştirilemez.
    /// </summary>
    public abstract class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; protected set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Kapsüllenmiş güvenlik alanları
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

        /// <summary>
        /// Şifreyi Salt ve Hash oluşturarak kapsüllenmiş bir şekilde kaydedilir.
        /// </summary>
        public void SetPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Şifre boş olamaz.");

            byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
            _passwordSalt = Convert.ToBase64String(saltBytes);
            _passwordHash = HashPasswordWithSalt(password, _passwordSalt);
        }

        /// <summary>
        /// Verilen şifrenin doğruluğunu kontrol edilir.
        /// </summary>
        public bool VerifyPassword(string inputPassword)
        {
            if (string.IsNullOrWhiteSpace(inputPassword) || string.IsNullOrEmpty(_passwordHash))
                return false;

            string computed = HashPasswordWithSalt(inputPassword, _passwordSalt);
            return _passwordHash == computed;
        }

        /// <summary>
        /// 5 hatalı denemede hesabı kilitler.
        /// </summary>
        public bool RecordFailedLoginAttempt()
        {
            _failedLoginAttempts++;
            if (_failedLoginAttempts >= 5)
            {
                _isLocked = true;
                _lockoutEndTime = DateTime.UtcNow.AddMinutes(15); // 15 dakika veya yönetici müdahalesi
                return true; // Kilitlendi
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

        /// <summary>
        /// NYP - Soyutlama: Alt sınıfların kendilerine özgü rol yetkilerini tanımlamasını zorunlu kılar.
        /// </summary>
        public abstract string GetUserTypeDescription();
        public abstract bool CanAccessAdminPanel();
    }
}
