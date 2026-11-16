using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StajTakipSistemi.Server.Data;
using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Services
{
    // SERVİS DTO: CSV veri seti yükleme sonuç ve istatistik modeli
    public class ImportResult
    {
        public bool Success { get; set; }
        public int TotalRecords { get; set; }
        public int AddedRecords { get; set; }
        public int UpdatedRecords { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    // SERVİS ARAYÜZÜ: Öğrenci ana veri setini içe aktarma sözleşmesi
    public interface IDatasetImportService
    {
        Task<ImportResult> ImportCsvAsync(Stream csvStream);
        string GenerateSampleCsv();
    }

    // SERVİS: Admin tarafından yüklenen CSV dosyasını okuyup veritabanına aktaran servis
    public class DatasetImportService : IDatasetImportService
    {
        private readonly AppDbContext _context;

        public DatasetImportService(AppDbContext context)
        {
            _context = context;
        }

        // İŞ KURALI: CSV satırlarını ayrıştırır, TC/Öğrenci No ile eşleştirip günceller veya ekler
        public async Task<ImportResult> ImportCsvAsync(Stream csvStream)
        {
            var result = new ImportResult();
            using var reader = new StreamReader(csvStream, Encoding.UTF8);

            string? headerLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                result.Errors.Add("CSV dosyası boş veya başlık satırı eksik.");
                return result;
            }

            int lineIndex = 1;
            while (!reader.EndOfStream)
            {
                string? line = await reader.ReadLineAsync();
                lineIndex++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = ParseCsvLine(line);
                if (parts.Length < 9)
                {
                    result.Errors.Add($"Satır {lineIndex}: Eksik sütun sayısı (en az 9 sütun bekleniyor).");
                    continue;
                }

                try
                {
                    string fullName = parts[0].Trim();
                    string tcNo = parts[1].Trim();
                    string nationality = parts.Length > 2 ? parts[2].Trim() : "TC";
                    string studentNumber = parts.Length > 3 ? parts[3].Trim() : "";
                    string birthDate = parts.Length > 4 ? parts[4].Trim() : "";
                    string address = parts.Length > 5 ? parts[5].Trim() : "";
                    string department = parts.Length > 6 ? parts[6].Trim() : "Yazılım Mühendisliği";
                    string submissionYear = parts.Length > 7 ? parts[7].Trim() : "2025-2026";
                    int completedDays = parts.Length > 8 && int.TryParse(parts[8].Trim(), out int cd) ? cd : 0;
                    string email = parts.Length > 9 ? parts[9].Trim() : "";
                    string phone = parts.Length > 10 ? parts[10].Trim() : "";
                    string health = parts.Length > 11 ? parts[11].Trim() : "Var";

                    var existing = await _context.StudentDatasetRecords
                        .FirstOrDefaultAsync(s => s.StudentNumber == studentNumber || s.TcNo == tcNo);

                    if (existing != null)
                    {
                        // Mevcut kaydı güncelle
                        existing.FullName = fullName;
                        existing.Nationality = nationality;
                        existing.BirthDate = birthDate;
                        existing.ResidentialAddress = address;
                        existing.Department = department;
                        existing.SubmissionYear = submissionYear;
                        existing.CompletedInternshipDays = completedDays;
                        existing.Email = email;
                        existing.PhoneNumber = phone;
                        existing.HealthInsuranceStatus = health;
                        result.UpdatedRecords++;
                    }
                    else
                    {
                        // Yeni öğrenci kaydı ekle
                        var newRecord = new StudentDatasetRecord
                        {
                            FullName = fullName,
                            TcNo = tcNo,
                            Nationality = nationality,
                            StudentNumber = studentNumber,
                            BirthDate = birthDate,
                            ResidentialAddress = address,
                            Department = department,
                            SubmissionYear = submissionYear,
                            CompletedInternshipDays = completedDays,
                            Email = email,
                            PhoneNumber = phone,
                            HealthInsuranceStatus = health,
                            UploadedAt = DateTime.UtcNow
                        };
                        _context.StudentDatasetRecords.Add(newRecord);
                        result.AddedRecords++;
                    }
                    result.TotalRecords++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Satır {lineIndex}: Hata - {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;
            return result;
        }

        // YARDIMCI: Virgül ve noktalı virgül ile ayrılmış CSV satırını parse eder
        private static string[] ParseCsvLine(string line)
        {
            char separator = line.Contains(';') ? ';' : ',';
            return line.Split(separator);
        }

        public string GenerateSampleCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Ad Soyad;TC Kimlik No;Uyruk;Ogrenci No;Dogum Tarihi;Ikametgah Adresi;Bolum;Ogretim Yili;Tamamlanan Gun;Eposta;Telefon;Saglik Sigortasi");
            sb.AppendLine("Göktuğ Büyüksarı;10000000146;TC;210202001;15.04.2003;Atatürk Mah. Cumhuriyet Cad. No: 12 Daire: 4 İzmit / Kocaeli;Yazılım Mühendisliği;2025-2026;20;ogrenci@ogr.kocaeli.edu.tr;+90 532 111 22 33;Var");
            sb.AppendLine("Ayşe Kaya;99123456780;Yabancı;210202099;20.08.2002;Fatih Mah. İnönü Sok. No: 5 İzmit / Kocaeli;Yazılım Mühendisliği;2025-2026;0;ayse@ogr.kocaeli.edu.tr;+90 533 444 55 66;Yok");
            return sb.ToString();
        }
    }
}
