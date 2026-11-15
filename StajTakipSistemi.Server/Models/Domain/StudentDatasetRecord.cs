using System;

namespace StajTakipSistemi.Server.Models.Domain
{
    /// <summary>
    /// Admin tarafından yüklenen test veri setindeki öğrenci bilgilerini tutar (Doküman Bölüm 4.3).
    /// Evrak yüklendiğinde, evraktaki bilgiler bu tablodaki bilgilerle çapraz kontrol edilir.
    /// </summary>
    public class StudentDatasetRecord
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string TcNo { get; set; } = string.Empty;
        public string Nationality { get; set; } = "TC"; // TC veya Yabancı
        public string StudentNumber { get; set; } = string.Empty;
        public string BirthDate { get; set; } = string.Empty;
        public string ResidentialAddress { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string SubmissionYear { get; set; } = "2025-2026";
        public int CompletedInternshipDays { get; set; } = 0;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string HealthInsuranceStatus { get; set; } = "Var"; // Var / Yok
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
