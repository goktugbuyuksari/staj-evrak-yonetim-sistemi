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

    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string usernameOrEmail, string password, string ipAddress);
        Task InvalidateTokenAsync(string token);
        bool IsTokenInvalidated(string token);
        Task<User?> GetUserByIdAsync(int id);
        Task<bool> UnlockUserAsync(int userId);
    }

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

        public async Task<AuthResult> LoginAsync(string usernameOrEmail, string password, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
            {
                return new AuthResult { Success = false, Message = "Kullanıcı adı/e-posta ve şifre zorunludur." };
            }

            string identifier = usernameOrEmail.Trim().ToLowerInvariant();
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == identifier || u.Email.ToLower() == identifier);

            if (user == null)
            {
                // Bilinmeyen kullanıcı başarısız giriş isteği
                _context.FailedLoginLogs.Add(new FailedLoginLog
                {
                    UsernameAttempted = usernameOrEmail,
                    IpAddress = ipAddress,
                    Reason = "Kullanıcı bulunamadı",
                    TriggeredLockout = false
                });
                await _context.SaveChangesAsync();

                return new AuthResult { Success = false, Message = "Kullanıcı adı veya şifre hatalı." };
            }

            // Hesap kilit kontrolü (5 hatalı deneme)
            if (user.IsLocked)
            {
                if (user.LockoutEndTime.HasValue && user.LockoutEndTime.Value > DateTime.UtcNow)
                {
                    var remaining = (user.LockoutEndTime.Value - DateTime.UtcNow).Minutes;
                    return new AuthResult
                    {
                        Success = false,
                        IsLocked = true,
                        Message = $"Çok sayıda hatalı deneme nedeniyle hesabınız kilitlenmiştir! Lütfen {remaining} dakika sonra tekrar deneyiniz veya yönetici ile iletişime geçiniz."
                    };
                }
                else if (user.LockoutEndTime.HasValue && user.LockoutEndTime.Value <= DateTime.UtcNow)
                {
                    // Kilit süresi bittiyse kilidi aç
                    user.Unlock();
                    await _context.SaveChangesAsync();
                }
            }

            // Şifre doğrulama
            if (!user.VerifyPassword(password))
            {
                bool lockedNow = user.RecordFailedLoginAttempt();
                int remaining = Math.Max(0, 5 - user.FailedLoginAttempts);

                _context.FailedLoginLogs.Add(new FailedLoginLog
                {
                    UsernameAttempted = user.Username,
                    IpAddress = ipAddress,
                    Reason = $"Hatalı şifre. Deneme {user.FailedLoginAttempts}/5",
                    TriggeredLockout = lockedNow
                });
                await _context.SaveChangesAsync();

                if (lockedNow)
                {
                    return new AuthResult
                    {
                        Success = false,
                        IsLocked = true,
                        RemainingAttempts = 0,
                        Message = "5 kez üst üste hatalı giriş yaptığınız için hesabınız güvenlik gerekçesiyle kilitlenmiştir!"
                    };
                }

                return new AuthResult
                {
                    Success = false,
                    IsLocked = false,
                    RemainingAttempts = remaining,
                    Message = $"Hatalı şifre! Kalan deneme hakkınız: {remaining}"
                };
            }

            // Başarılı giriş: Hatalı sayaçları sıfırla
            user.ResetFailedAttempts();

            _context.SystemLogs.Add(new SystemLog
            {
                ActionType = "LOGIN",
                Username = user.Username,
                Details = $"Başarılı oturum açıldı. Rol: {user.Role}, IP: {ipAddress}"
            });

            await _context.SaveChangesAsync();

            // JWT Token üret
            string token = GenerateJwtToken(user);

            return new AuthResult
            {
                Success = true,
                Message = "Giriş başarılı.",
                Token = token,
                Role = user.Role,
                Username = user.Username,
                FullName = user.FullName,
                UserId = user.Id
            };
        }

        public Task InvalidateTokenAsync(string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                BlacklistedTokens.TryAdd(token, DateTime.UtcNow.AddHours(8));
            }
            return Task.CompletedTask;
        }

        public bool IsTokenInvalidated(string token)
        {
            if (string.IsNullOrEmpty(token)) return true;
            return BlacklistedTokens.ContainsKey(token);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<bool> UnlockUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.Unlock();
            await _context.SaveChangesAsync();
            return true;
        }

        private string GenerateJwtToken(User user)
        {
            var keyStr = _config["Jwt:Key"] ?? "StajTakipSistemi_SuperSecret_SecurityKey_2026_ProLab3!";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FullName", user.FullName)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "StajTakipApi",
                audience: _config["Jwt:Audience"] ?? "StajTakipClient",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
