using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StajTakipSistemi.Server.Data;
using StajTakipSistemi.Server.Models.Domain;
using StajTakipSistemi.Server.Models.DTOs;
using StajTakipSistemi.Server.Services.OOP_RuleEngine;
using StajTakipSistemi.Server.Services;

namespace StajTakipSistemi.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IDocumentValidationEngine _validationEngine;
        private readonly IDocumentTextExtractor _textExtractor;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;
        private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public DocumentsController(
            AppDbContext context, 
            IDocumentValidationEngine validationEngine,
            IDocumentTextExtractor textExtractor,
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _context = context;
            _validationEngine = validationEngine;
            _textExtractor = textExtractor;
            _env = env;
        }

        /// <summary>
        /// Doküman Bölüm 4.4: Evrak Yükleme
        /// Kabul edilen dosya formatları: Word (.doc/.docx), PDF, JPEG, PNG.
        /// Diğer formatlar reddedilir ve kullanıcıya uyarı verilir.
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument([FromForm] IFormFile? file, [FromForm] string? documentData)
        {
            string? userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Geçersiz kullanıcı oturumu." });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Unauthorized(new { message = "Kullanıcı bulunamadı." });

            // Dosya kontrolü ve fiziksel kaydetme (Bölüm 4.4 & Hoca Soru 1)
            string fileName = "Ek16_Belgesi.pdf";
            string fileExt = ".pdf";
            long fileSize = 1024 * 50;
            string filePath = string.Empty;
            string extractedText = string.Empty;

            if (file != null && file.Length > 0)
            {
                fileExt = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(fileExt))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Geçersiz dosya formatı ({fileExt})! Sistem yalnızca Word (.doc, .docx), PDF, JPEG ve PNG formatındaki Ek-16 belgelerini kabul etmektedir."
                    });
                }
                fileName = file.FileName;
                fileSize = file.Length;

                try
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                    string physicalPath = Path.Combine(uploadsFolder, uniqueFileName);
                    filePath = $"/uploads/{uniqueFileName}";

                    using (var stream = new FileStream(physicalPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Belge Metin Çıkarıcı / Ön-OCR Akışı (Hoca Soru 1)
                    using var readStream = file.OpenReadStream();
                    extractedText = _textExtractor.ExtractText(readStream, fileExt);
                }
                catch { }
            }

            if (string.IsNullOrEmpty(filePath))
            {
                filePath = $"/uploads/{Guid.NewGuid()}_{fileName}";
            }

            // JSON verisini çözümle
            DocumentUploadDto? dto = null;
            if (!string.IsNullOrWhiteSpace(documentData))
            {
                try
                {
                    dto = JsonSerializer.Deserialize<DocumentUploadDto>(documentData, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (Exception ex)
                {
                    return BadRequest(new { success = false, message = $"Form verisi çözümlenemedi: {ex.Message}" });
                }
            }

            dto ??= new DocumentUploadDto();

            // Öğrenci ise DB verisiyle tamamla
            var studentUser = user as StudentUser;
            var doc = new InternshipDocument
            {
                StudentUserId = user.Id,
                StudentUsername = user.Username,
                FileName = fileName,
                FileExtension = fileExt,
                FileSizeBytes = fileSize,
                FilePath = filePath,
                ExtractedText = !string.IsNullOrWhiteSpace(extractedText) ? extractedText : ($"T.C. KOCAELİ ÜNİVERSİTESİ EK-16 STAJ FORMU\nÖğrenci: {dto.StudentFullName ?? user.FullName}\nTC: {dto.StudentTcNo ?? studentUser?.TcNo}\nNo: {dto.StudentNumber ?? studentUser?.StudentNumber}\nBölüm: {dto.Department ?? studentUser?.Department}\nKurum: {dto.CompanyName}\nTarih: {dto.StartDate:dd.MM.yyyy} - {dto.EndDate:dd.MM.yyyy} ({dto.DeclaredDaysCount} gün)"),
                StudentFullName = !string.IsNullOrWhiteSpace(dto.StudentFullName) ? dto.StudentFullName : user.FullName,
                StudentTcNo = !string.IsNullOrWhiteSpace(dto.StudentTcNo) ? dto.StudentTcNo : (studentUser?.TcNo ?? ""),
                Nationality = !string.IsNullOrWhiteSpace(dto.Nationality) ? dto.Nationality : (studentUser?.Nationality ?? "TC"),
                StudentNumber = !string.IsNullOrWhiteSpace(dto.StudentNumber) ? dto.StudentNumber : (studentUser?.StudentNumber ?? user.Username),
                Department = !string.IsNullOrWhiteSpace(dto.Department) ? dto.Department : (studentUser?.Department ?? "Yazılım Mühendisliği"),
                Address = !string.IsNullOrWhiteSpace(dto.Address) ? dto.Address : (studentUser?.ResidentialAddress ?? ""),
                PhoneNumber = !string.IsNullOrWhiteSpace(dto.PhoneNumber) ? dto.PhoneNumber : (studentUser?.PhoneNumber ?? ""),
                Email = !string.IsNullOrWhiteSpace(dto.Email) ? dto.Email : user.Email,
                FaxNumber = dto.FaxNumber,
                AcademicYear = dto.AcademicYear,
                SubmissionDate = dto.SubmissionDate != default ? dto.SubmissionDate : DateTime.Today,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                SelectedDaysOfWeek = dto.SelectedDaysOfWeek,
                DeclaredDaysCount = dto.DeclaredDaysCount,
                CompanyName = dto.CompanyName,
                CompanyAddress = dto.CompanyAddress,
                EngineerCount = dto.EngineerCount,
                EngineerGraduationDepartment = dto.EngineerGraduationDepartment,
                HasStudentSignature = dto.HasStudentSignature,
                HasAdvisorSignature = dto.HasAdvisorSignature,
                HasEmployerSignatureAndStamp = dto.HasEmployerSignatureAndStamp,
                GoogleFormUrl = dto.GoogleFormUrl,
                Status = DocumentStatuses.Inceleniyor,
                CreatedAt = DateTime.UtcNow
            };

            // Öğrencinin Admin tarafından yüklenmiş veri tabanındaki orijinal kaydını bul
            var dbRecord = await _context.StudentDatasetRecords
                .FirstOrDefaultAsync(r => r.StudentNumber == doc.StudentNumber || r.TcNo == doc.StudentTcNo);

            try
            {
                // Kural motorunu çalıştır
                var report = _validationEngine.ValidateDocument(doc, dbRecord);
                doc.ValidationReportJson = JsonSerializer.Serialize(report, _jsonOptions);
                doc.Status = report.FinalStatus;
                doc.IsApproved = report.IsOverallValid;

                _context.InternshipDocuments.Add(doc);
                _context.SystemLogs.Add(new SystemLog
                {
                    ActionType = "DOCUMENT_UPLOAD",
                    Username = user.Username,
                    Details = $"Ek-16 belgesi yüklendi ve doğrulandı. Sonuç Statüsü: {doc.Status}. Hata: {report.ErrorCount}, Uyarı: {report.WarningCount}"
                });

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = report.IsOverallValid
                        ? "Belgeniz kurallara tam uyumlu olarak doğrulandı ve 'Tamamlandı' statüsüne alındı."
                        : "Belgenizde eksik veya hatalı alanlar tespit edildi. Lütfen rapordaki uyarıları inceleyiniz.",
                    document = doc,
                    report = report
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Evrak kaydedilirken veritabanı hatası oluştu: {ex.Message}. (Veritabanınız güncel değilse lütfen 'staj_takip.db' dosyasını silip sunucuyu yeniden başlatınız.)"
                });
            }
        }

        [HttpGet("my-documents")]
        public async Task<IActionResult> GetMyDocuments()
        {
            string? userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            var docs = await _context.InternshipDocuments
                .Where(d => d.StudentUserId == userId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return Ok(docs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocumentById(int id)
        {
            var doc = await _context.InternshipDocuments.FindAsync(id);
            if (doc == null)
                return NotFound(new { message = "Belge bulunamadı." });

            string? userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "ROLE_ADMIN" && doc.StudentUserId.ToString() != userIdStr)
            {
                return StatusCode(403, new { message = "Bu belgeye erişim yetkiniz yoktur." });
            }

            ValidationReport? report = null;
            if (!string.IsNullOrEmpty(doc.ValidationReportJson))
            {
                try
                {
                    report = JsonSerializer.Deserialize<ValidationReport>(doc.ValidationReportJson, _jsonOptions);
                }
                catch { }
            }

            return Ok(new { document = doc, report });
        }

        /// <summary>
        /// Doküman Bölüm 5: Tamamlandı statüsündeki belgeler bir daha güncellenemez (öğrenci için kilitli)!
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(int id, [FromBody] DocumentUploadDto dto)
        {
            var doc = await _context.InternshipDocuments.FindAsync(id);
            if (doc == null)
                return NotFound(new { message = "Belge bulunamadı." });

            string? userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            bool isAdmin = userRole == "ROLE_ADMIN";

            if (!isAdmin && doc.StudentUserId.ToString() != userIdStr)
            {
                return StatusCode(403, new { message = "Bu belgeyi güncelleme yetkiniz yoktur." });
            }

            // Değişmezlik Kuralı: Öğrenci Tamamlandı statüsündeki belgeyi güncelleyemez!
            if (!doc.CanStudentModify() && !isAdmin)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Bu belge 'Tamamlandı' statüsüne geçmiştir ve kilitlenmiştir. Tamamlanan belgeler öğrenci tarafından güncellenemez ve silinemez. Değişiklik için Staj Komisyonu (Admin) ile iletişime geçiniz."
                });
            }

            // Alanları güncelle
            doc.StudentFullName = dto.StudentFullName;
            doc.StudentTcNo = dto.StudentTcNo;
            doc.Nationality = dto.Nationality;
            doc.StudentNumber = dto.StudentNumber;
            doc.Department = dto.Department;
            doc.Address = dto.Address;
            doc.PhoneNumber = dto.PhoneNumber;
            doc.Email = dto.Email;
            doc.FaxNumber = dto.FaxNumber;
            doc.AcademicYear = dto.AcademicYear;
            doc.SubmissionDate = dto.SubmissionDate;
            doc.StartDate = dto.StartDate;
            doc.EndDate = dto.EndDate;
            doc.SelectedDaysOfWeek = dto.SelectedDaysOfWeek;
            doc.DeclaredDaysCount = dto.DeclaredDaysCount;
            doc.CompanyName = dto.CompanyName;
            doc.CompanyAddress = dto.CompanyAddress;
            doc.EngineerCount = dto.EngineerCount;
            doc.EngineerGraduationDepartment = dto.EngineerGraduationDepartment;
            doc.HasStudentSignature = dto.HasStudentSignature;
            doc.HasAdvisorSignature = dto.HasAdvisorSignature;
            doc.HasEmployerSignatureAndStamp = dto.HasEmployerSignatureAndStamp;
            doc.GoogleFormUrl = dto.GoogleFormUrl;

            // Tekrar kural motorunu çalıştır
            var dbRecord = await _context.StudentDatasetRecords
                .FirstOrDefaultAsync(r => r.StudentNumber == doc.StudentNumber || r.TcNo == doc.StudentTcNo);

            var report = _validationEngine.ValidateDocument(doc, dbRecord);
            doc.ValidationReportJson = JsonSerializer.Serialize(report, _jsonOptions);
            doc.Status = report.FinalStatus;
            doc.IsApproved = report.IsOverallValid;
            doc.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Belge başarıyla güncellendi ve yeniden doğrulandı.",
                document = doc,
                report = report
            });
        }

        /// <summary>
        /// Doküman Bölüm 5: Tamamlandı statüsündeki belgeler bir daha silinemez (öğrenci için kilitli)!
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var doc = await _context.InternshipDocuments.FindAsync(id);
            if (doc == null)
                return NotFound(new { message = "Belge bulunamadı." });

            string? userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            bool isAdmin = userRole == "ROLE_ADMIN";

            if (!isAdmin && doc.StudentUserId.ToString() != userIdStr)
            {
                return StatusCode(403, new { message = "Bu belgeyi silme yetkiniz yoktur." });
            }

            if (!doc.CanStudentDelete() && !isAdmin)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Bu belge 'Tamamlandı' statüsündedir. Tamamlanan belgeler öğrenci tarafından silinemez!"
                });
            }

            _context.InternshipDocuments.Remove(doc);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Belge başarıyla silindi." });
        }

        /// <summary>
        /// Doküman Bölüm 4.6 & Bölüm 3:
        /// Kullanıcı sisteme staj evrağı teslim Google Form'un URL'sini girer.
        /// Sistem URL'nin geçerli olup olmadığını kontrol eder. Geçersizse ekranda pop-up uyarı çıkar.
        /// </summary>
        [HttpPost("check-google-form")]
        public IActionResult CheckGoogleForm([FromBody] CheckGoogleFormRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Url))
            {
                return Ok(new
                {
                    isValid = false,
                    isPopupAlert = true,
                    title = "Geçersiz Form URL'si",
                    message = "Google Form URL alanı boş bırakılamaz! Lütfen staj evrağı teslim Google Form linkini giriniz."
                });
            }

            string url = req.Url.Trim();
            bool isGoogleForm = url.StartsWith("https://docs.google.com/forms/", StringComparison.OrdinalIgnoreCase) ||
                               url.StartsWith("https://forms.gle/", StringComparison.OrdinalIgnoreCase);

            if (!isGoogleForm)
            {
                return Ok(new
                {
                    isValid = false,
                    isPopupAlert = true,
                    title = "UYARI: Geçersiz Google Form URL'si!",
                    message = $"Girilen adres ('{url}') geçerli bir Google Form bağlantısı değildir!\nURL adresi 'https://docs.google.com/forms/...' veya 'https://forms.gle/...' ile başlamalıdır."
                });
            }

            return Ok(new
            {
                isValid = true,
                isPopupAlert = false,
                title = "Başarılı",
                message = "Google Form URL'si doğrulandı. Belge 'Tamamlandı' statüsüne geçtiğinde veriler bu forma otomatik kaydedilecektir."
            });
        }
    }
}
