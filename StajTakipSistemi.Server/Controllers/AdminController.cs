using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    [Authorize(Roles = "ROLE_ADMIN")] // Yetkilendirme Katmanı (Doküman 4.2)
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IDatasetImportService _importService;
        private readonly IAuthService _authService;

        public AdminController(AppDbContext context, IDatasetImportService importService, IAuthService authService)
        {
            _context = context;
            _importService = importService;
            _authService = authService;
        }

        /// <summary>
        /// Doküman Bölüm 4.2: Tüm başvuruları görüntüleme, listeleme + filtreleme işlemleri
        /// </summary>
        [HttpGet("documents")]
        public async Task<IActionResult> GetAllDocuments([FromQuery] string? status, [FromQuery] string? search)
        {
            var query = _context.InternshipDocuments.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(d => d.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.ToLower();
                query = query.Where(d => d.StudentFullName.ToLower().Contains(s) ||
                                         d.StudentNumber.ToLower().Contains(s) ||
                                         d.StudentTcNo.Contains(s) ||
                                         d.CompanyName.ToLower().Contains(s));
            }

            var list = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
            return Ok(list);
        }

        /// <summary>
        /// Doküman Bölüm 4.2 & 4.7: Tamamlanan başvurular ayrı sekmede listelenir
        /// </summary>
        [HttpGet("documents/completed")]
        public async Task<IActionResult> GetCompletedDocuments()
        {
            var list = await _context.InternshipDocuments
                .Where(d => d.Status == DocumentStatuses.Tamamlandi)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return Ok(list);
        }

        /// <summary>
        /// Doküman Bölüm 5: Tamamlandı statüsündeki evrakları sadece Admin güncelleyebilir ve silebilir
        /// </summary>
        [HttpPut("documents/{id}/status")]
        public async Task<IActionResult> UpdateDocumentStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var doc = await _context.InternshipDocuments.FindAsync(id);
            if (doc == null)
                return NotFound(new { message = "Belge bulunamadı." });

            doc.UpdateStatus(dto.Status, isAdmin: true);
            await _context.SaveChangesAsync();

            _context.SystemLogs.Add(new SystemLog
            {
                ActionType = "STATUS_OVERRIDE",
                Username = User.Identity?.Name ?? "Admin",
                Details = $"Belge #{id} statüsü Admin tarafından '{dto.Status}' olarak güncellendi."
            });
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Belge statüsü '{dto.Status}' olarak güncellendi.", document = doc });
        }

        [HttpDelete("documents/{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var doc = await _context.InternshipDocuments.FindAsync(id);
            if (doc == null)
                return NotFound(new { message = "Belge bulunamadı." });

            _context.InternshipDocuments.Remove(doc);
            _context.SystemLogs.Add(new SystemLog
            {
                ActionType = "DOCUMENT_DELETE",
                Username = User.Identity?.Name ?? "Admin",
                Details = $"Belge #{id} ({doc.StudentFullName} - {doc.FileName}) Admin tarafından silindi."
            });
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Belge admin yetkisiyle başarıyla silindi." });
        }

        /// <summary>
        /// Doküman Bölüm 4.3: Test veri setini yönetme ve listeleme
        /// </summary>
        [HttpGet("dataset")]
        public async Task<IActionResult> GetDatasetRecords([FromQuery] string? search)
        {
            var query = _context.StudentDatasetRecords.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.ToLower();
                query = query.Where(r => r.FullName.ToLower().Contains(s) ||
                                         r.StudentNumber.ToLower().Contains(s) ||
                                         r.TcNo.Contains(s));
            }

            var records = await query.OrderBy(r => r.StudentNumber).ToListAsync();
            return Ok(records);
        }

        /// <summary>
        /// Doküman Bölüm 4.3: Admin, test amaçlı bir veri seti yükler (CSV formatında)
        /// </summary>
        [HttpPost("dataset/upload")]
        public async Task<IActionResult> UploadDataset(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "Lütfen bir CSV dosyası seçiniz." });

            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".csv" && ext != ".txt")
            {
                return BadRequest(new { success = false, message = "Yalnızca .csv dosyaları desteklenmektedir." });
            }

            using var stream = file.OpenReadStream();
            var result = await _importService.ImportCsvAsync(stream);

            _context.SystemLogs.Add(new SystemLog
            {
                ActionType = "DATASET_IMPORT",
                Username = User.Identity?.Name ?? "Admin",
                Details = $"Öğrenci test veri seti yüklendi. Eklenen: {result.AddedRecords}, Güncellenen: {result.UpdatedRecords}"
            });
            await _context.SaveChangesAsync();

            return Ok(result);
        }

        [HttpGet("dataset/sample")]
        public IActionResult DownloadSampleCsv()
        {
            string csvContent = _importService.GenerateSampleCsv();
            byte[] bytes = Encoding.UTF8.GetBytes(csvContent);
            return File(bytes, "text/csv", "test_ogrenci_veriseti.csv");
        }

        /// <summary>
        /// Güvenlik ve Giriş Denetim Logları (5 hatalı deneme ve kilitlenmeler)
        /// </summary>
        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs()
        {
            var failedLogins = await _context.FailedLoginLogs
                .OrderByDescending(l => l.AttemptTime)
                .Take(50)
                .ToListAsync();

            var systemLogs = await _context.SystemLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(50)
                .ToListAsync();

            var lockedUsers = await _context.Users
                .Where(u => u.IsLocked)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.FullName,
                    u.Email,
                    u.FailedLoginAttempts,
                    u.LockoutEndTime
                })
                .ToListAsync();

            return Ok(new
            {
                failedLogins,
                systemLogs,
                lockedUsers
            });
        }

        [HttpPost("users/{id}/unlock")]
        public async Task<IActionResult> UnlockUser(int id)
        {
            bool ok = await _authService.UnlockUserAsync(id);
            if (!ok) return NotFound(new { message = "Kullanıcı bulunamadı." });

            _context.SystemLogs.Add(new SystemLog
            {
                ActionType = "USER_UNLOCK",
                Username = User.Identity?.Name ?? "Admin",
                Details = $"Kullanıcı #{id} kilidi Admin tarafından kaldırıldı."
            });
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Kullanıcı hesabı kilidi başarıyla açıldı." });
        }
    }
}
