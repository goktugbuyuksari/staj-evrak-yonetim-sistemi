using System;

namespace StajTakipSistemi.Server.Models.Domain
{
    public static class DocumentStatuses
    {
        public const string Yuklendi = "Yüklendi";
        public const string Inceleniyor = "İnceleniyor";
        public const string HataliEksik = "Hatalı / Eksik";
        public const string Onaylandi = "Onaylandı";
        public const string Tamamlandi = "Tamamlandı";
    }

    /// <summary>
    /// NYP - KAPSÜLLEME (Encapsulation): Ek-16 Staj Başvuru Belgesi Varlığı.
    /// Belge durum geçişleri ve değişmezlik (immutability) kuralları burada kapsüllenir.
    /// </summary>
    public class InternshipDocument
    {
        public int Id { get; set; }
        public int StudentUserId { get; set; }
        public string StudentUsername { get; set; } = string.Empty;

        // Dosya bilgileri
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }

        // Öğrenci Belge Alanları
        public string StudentFullName { get; set; } = string.Empty;
        public string StudentTcNo { get; set; } = string.Empty;
        public string Nationality { get; set; } = "TC"; // TC veya Yabancı
        public string StudentNumber { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FaxNumber { get; set; } // Boş bırakılabilir (nullable)

        // Tarih ve Staj Planı Alanları
        public string AcademicYear { get; set; } = "2025-2026 Bahar";
        public DateTime SubmissionDate { get; set; } = DateTime.Today;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string SelectedDaysOfWeek { get; set; } = "Pazartesi,Salı,Çarşamba,Perşembe,Cuma";
        public int DeclaredDaysCount { get; set; } = 20;

        // İşveren ve Mühendis Alanları
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyAddress { get; set; } = string.Empty;
        public int EngineerCount { get; set; } = 1;
        public string? EngineerGraduationDepartment { get; set; }

        // İmza ve Onay Alanları (Beklenen: Öğrenci ve Danışman BOŞ, İşveren DOLU)
        public bool HasStudentSignature { get; set; } = false;
        public bool HasAdvisorSignature { get; set; } = false;
        public bool HasEmployerSignatureAndStamp { get; set; } = true;

        // Google Form URL
        public string? GoogleFormUrl { get; set; }

        // Belge Statüsü (Kapsüllenmiş yönetim)
        public string Status { get; set; } = DocumentStatuses.Yuklendi;
        public bool IsApproved { get; set; } = false;
        public string ValidationReportJson { get; set; } = string.Empty;
        public string? ExtractedText { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Doküman Bölüm 5: Tamamlandı statüsündeki belgeler bir daha öğrenci tarafından güncellenemez veya silinemez!
        /// </summary>
        public bool CanStudentModify()
        {
            return Status != DocumentStatuses.Tamamlandi;
        }

        public bool CanStudentDelete()
        {
            return Status != DocumentStatuses.Tamamlandi;
        }

        /// <summary>
        /// Belge statüsünü güvenli bir şekilde günceller.
        /// </summary>
        public void UpdateStatus(string newStatus, bool isAdmin = false)
        {
            if (Status == DocumentStatuses.Tamamlandi && !isAdmin)
            {
                throw new InvalidOperationException("Tamamlandı statüsündeki evrak yalnızca yönetici tarafından değiştirilebilir.");
            }

            Status = newStatus;
            UpdatedAt = DateTime.UtcNow;

            if (newStatus == DocumentStatuses.Onaylandi || newStatus == DocumentStatuses.Tamamlandi)
            {
                IsApproved = true;
            }
            else
            {
                IsApproved = false;
            }
        }
    }
}
