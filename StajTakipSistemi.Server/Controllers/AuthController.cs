using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StajTakipSistemi.Server.Data;
using StajTakipSistemi.Server.Models.Domain;
using StajTakipSistemi.Server.Models.DTOs;
using StajTakipSistemi.Server.Services;

namespace StajTakipSistemi.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly AppDbContext _context;

        public AuthController(IAuthService authService, AppDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _authService.LoginAsync(request.Username, request.Password, ip);

            if (!result.Success)
            {
                if (result.IsLocked)
                {
                    return StatusCode(423, result); // 423 Locked
                }
                return Unauthorized(result);
            }

            return Ok(result);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                string token = authHeader.Substring("Bearer ".Length).Trim();
                await _authService.InvalidateTokenAsync(token);
            }

            string username = User.Identity?.Name ?? "Bilinmeyen";
            _context.SystemLogs.Add(new SystemLog
            {
                ActionType = "LOGOUT",
                Username = username,
                Details = "Kullanıcı güvenli çıkış yaptı (Token geçersiz kılındı)."
            });
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Başarıyla çıkış yapıldı. Token geçersiz kılındı." });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            string? userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new { message = "Geçersiz oturum." });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı." });
            }

            if (user is StudentUser student)
            {
                return Ok(new
                {
                    id = student.Id,
                    username = student.Username,
                    fullName = student.FullName,
                    email = student.Email,
                    role = student.Role,
                    studentNumber = student.StudentNumber,
                    tcNo = student.TcNo,
                    nationality = student.Nationality,
                    department = student.Department,
                    phoneNumber = student.PhoneNumber,
                    residentialAddress = student.ResidentialAddress,
                    completedInternshipDays = student.CompletedInternshipDays,
                    healthInsuranceStatus = student.HealthInsuranceStatus,
                    userTypeDescription = student.GetUserTypeDescription()
                });
            }

            if (user is AdminUser admin)
            {
                return Ok(new
                {
                    id = admin.Id,
                    username = admin.Username,
                    fullName = admin.FullName,
                    email = admin.Email,
                    role = admin.Role,
                    departmentTitle = admin.DepartmentTitle,
                    isSuperAdmin = admin.IsSuperAdmin,
                    userTypeDescription = admin.GetUserTypeDescription()
                });
            }

            return Ok(new
            {
                id = user.Id,
                username = user.Username,
                fullName = user.FullName,
                email = user.Email,
                role = user.Role,
                userTypeDescription = user.GetUserTypeDescription()
            });
        }
    }
}
