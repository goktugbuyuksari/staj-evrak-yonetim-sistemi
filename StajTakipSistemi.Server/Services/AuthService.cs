using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StajTakipSistemi.Server.Data;
using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Services
{
    // SERVİS DTO: Oturum açma sonucunu ve üretilen JWT token'ı taşıyan sonuç modeli
    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public string? Role { get; set; }
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public int? UserId { get; set; }
        public bool IsLocked { get; set; }
        public int RemainingAttempts { get; set; } = 5;
    }

    // SERVİS ARAYÜZÜ: Kimlik doğrulama, token yönetimi ve kilit açma sözleşmesi
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string usernameOrEmail, string password, string ipAddress);
        Task InvalidateTokenAsync(string token);
        bool IsTokenInvalidated(string token);
        Task<User?> GetUserByIdAsync(int id);
        Task<bool> UnlockUserAsync(int userId);
    }

    // SERVİS: Kullanıcı girişi, PBKDF2/SHA-256 şifre denetimi ve JWT üretim servisi
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private static readonly ConcurrentDictionary<string, DateTime> BlacklistedTokens = new();

        public AuthService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // İŞ KURALI: Kullanıcı girişi, 5 hatalı deneme kilit kontrolü ve oturum başlatma
        public async Task<AuthResult> LoginAsync(string usernameOrEmail, string password, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
            {
                return new AuthResult { Success = false, Message = "Kullanıcı adı ve şifre gereklidir." };
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);

            if (user == null)
            {
                await LogFailedAttemptAsync(usernameOrEmail, ipAddress, "Kullanıcı bulunamadı.", false);
                return new AuthResult { Success = false, Message = "Geçersiz kullanıcı adı veya şifre." };
            }

            // GÜVENLİK KURALI: Hesap kilitli mi kontrolü (Doküman 4.1)
            if (user.IsLocked)
            {
                if (user.LockoutEndTime.HasValue && user.LockoutEndTime.Value > DateTime.UtcNow)
                {
                    await LogFailedAttemptAsync(usernameOrEmail, ipAddress, "Hesap kilitli iken giriş denemesi.", true);
                    return new AuthResult
                    {
                        Success = false,
                        IsLocked = true,
                        Message = "Çok fazla hatalı giriş denemesi yapıldığı için hesabınız kilitlendi! Lütfen yönetici ile iletişime geçiniz."
                    };
                }
                else
                {
                    user.ResetFailedAttempts();
                    await _context.SaveChangesAsync();
                }
            }

            // ŞİFRE DOĞRULAMA: Kapsüllenmiş VerifyPassword metodu çağrılır
            if (!user.VerifyPassword(password))
            {
                bool justLocked = user.RecordFailedLoginAttempt();
                await LogFailedAttemptAsync(usernameOrEmail, ipAddress, "Hatalı şifre girildi.", justLocked);
                await _context.SaveChangesAsync();

                if (justLocked)
                {
                    return new AuthResult
                    {
                        Success = false,
                        IsLocked = true,
                        RemainingAttempts = 0,
                        Message = "5 ardışık hatalı şifre girişi yapıldı! Güvenlik kuralları gereği hesabınız 15 dakika kilitlendi."
                    };
                }

                int remaining = Math.Max(0, 5 - user.FailedLoginAttempts);
                return new AuthResult
                {
                    Success = false,
                    RemainingAttempts = remaining,
                    Message = $"Geçersiz şifre! Kalan deneme hakkınız: {remaining}"
                };
            }

            // Başarılı giriş: Hatalı deneme sayacını sıfırla ve JWT token üret
            user.ResetFailedAttempts();
            await _context.SaveChangesAsync();

            string token = GenerateJwtToken(user);

            return new AuthResult
            {
                Success = true,
                Token = token,
                Role = user.Role,
                Username = user.Username,
                FullName = user.FullName,
                UserId = user.Id,
                Message = "Giriş başarılı."
            };
        }

        // OTURUM GÜVENLİĞİ: Çıkış yapan kullanıcının token'ını karalisteye (blacklist) ekler
        public Task InvalidateTokenAsync(string token)
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                BlacklistedTokens.TryAdd(token, DateTime.UtcNow.AddHours(8));
            }
            return Task.CompletedTask;
        }

        public bool IsTokenInvalidated(string token)
        {
            return BlacklistedTokens.ContainsKey(token);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        // YÖNETİCİ YETKİSİ: Kilitlenmiş hesabın kilidini açar
        public async Task<bool> UnlockUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.Unlock();
            _context.SystemLogs.Add(new SystemLog
            {
                ActionType = "USER_UNLOCK",
                Username = "ADMIN",
                Details = $"{user.Username} kullanıcısının hesabı yönetici tarafından açıldı."
            });

            await _context.SaveChangesAsync();
            return true;
        }

        // GÜVENLİK LOGU: Başarısız denemeleri IP ve zaman damgasıyla kaydeder
        private async Task LogFailedAttemptAsync(string username, string ip, string reason, bool triggeredLockout)
        {
            _context.FailedLoginLogs.Add(new FailedLoginLog
            {
                UsernameAttempted = username,
                IpAddress = ip,
                AttemptTime = DateTime.UtcNow,
                Reason = reason,
                TriggeredLockout = triggeredLockout
            });
            await _context.SaveChangesAsync();
        }

        // TOKEN ÜRETİMİ: Kullanıcı ID ve rol claim'leri içeren JWT oluşturur
        private string GenerateJwtToken(User user)
        {
            var keyStr = _config["Jwt:Key"] ?? "KocaeliUniversitesiStajTakipGizliAnahtar2026!";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FullName", user.FullName)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "StajTakipSistemi",
                audience: _config["Jwt:Audience"] ?? "StajTakipClient",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
