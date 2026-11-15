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
    public class ImportResult
    {
        public bool Success { get; set; }
        public int TotalRecords { get; set; }
        public int AddedRecords { get; set; }
        public int UpdatedRecords { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public interface IDatasetImportService
    {
        Task<ImportResult> ImportCsvAsync(Stream csvStream);
        string GenerateSampleCsv();
    }

    /// <summary>
    /// Admin Test Veri Seti İçe Aktarma Servisi (Doküman Bölüm 4.3).
    /// CSV formatındaki öğrenci verilerini sisteme yükler ve öğrenci kullanıcı hesaplarını senkronize eder.
    /// </summary>
    public class DatasetImportService : IDatasetImportService
    {
        private readonly AppDbContext _context;

        public DatasetImportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ImportResult> ImportCsvAsync(Stream csvStream)
        {
            var result = new ImportResult();
            using var reader = new StreamReader(csvStream, Encoding.UTF8);

            string? headerLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                result.Success = false;
                result.Errors.Add("CSV dosyası boş veya başlık satırı eksik.");
                return result;
            }

            char delimiter = headerLine.Contains(';') ? ';' : ',';

            int lineNumber = 1;
            while (!reader.EndOfStream)
            {
                lineNumber++;
                string? line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cols = line.Split(delimiter).Select(c => c.Trim().Trim('"')).ToArray();
                if (cols.Length < 11)
                {
                    result.Errors.Add($"Satır {lineNumber}: Eksik sütun sayısı ({cols.Length}/12).");
                    continue;
                }

                try
                {
                    // Sütun Eşlemesi:
                    // 0: Öğrenci ad soyad
                    // 1: TC kimlik no
                    // 2: Uyruk
                    // 3: Öğrenci no
                    // 4: Doğum tarihi
                    // 5: İkametgâh adresi (açık adres)
                    // 6: Öğrencinin okuduğu bölüm
                    // 7: Ek-16 teslim yılı
                    // 8: Daha önce yapılan staj gün sayısı
                    // 9: E-posta
                    // 10: Telefon no
                    // 11: Sağlık yardımı statüsü (opsiyonel)

                    string fullName = cols[0];
                    string tcNo = cols[1];
                    string nationality = cols.Length > 2 ? cols[2] : "TC";
                    string studentNo = cols.Length > 3 ? cols[3] : "";
                    string birthDate = cols.Length > 4 ? cols[4] : "";
                    string address = cols.Length > 5 ? cols[5] : "";
                    string department = cols.Length > 6 ? cols[6] : "Yazılım Mühendisliği";
                    string submissionYear = cols.Length > 7 ? cols[7] : "2025-2026";
                    int.TryParse(cols.Length > 8 ? cols[8] : "0", out int previousDays);
                    string email = cols.Length > 9 ? cols[9] : "";
                    string phone = cols.Length > 10 ? cols[10] : "";
                    string healthStatus = cols.Length > 11 ? cols[11] : "Var";

                    var existingRecord = await _context.StudentDatasetRecords
                        .FirstOrDefaultAsync(s => s.StudentNumber == studentNo || s.TcNo == tcNo);

                    if (existingRecord != null)
                    {
                        existingRecord.FullName = fullName;
                        existingRecord.TcNo = tcNo;
                        existingRecord.Nationality = nationality;
                        existingRecord.BirthDate = birthDate;
                        existingRecord.ResidentialAddress = address;
                        existingRecord.Department = department;
                        existingRecord.SubmissionYear = submissionYear;
                        existingRecord.CompletedInternshipDays = previousDays;
                        existingRecord.Email = email;
                        existingRecord.PhoneNumber = phone;
                        existingRecord.HealthInsuranceStatus = healthStatus;
                        result.UpdatedRecords++;
                    }
                    else
                    {
                        var newRecord = new StudentDatasetRecord
                        {
                            FullName = fullName,
                            TcNo = tcNo,
                            Nationality = nationality,
                            StudentNumber = studentNo,
                            BirthDate = birthDate,
                            ResidentialAddress = address,
                            Department = department,
                            SubmissionYear = submissionYear,
                            CompletedInternshipDays = previousDays,
                            Email = email,
                            PhoneNumber = phone,
                            HealthInsuranceStatus = healthStatus
                        };
                        _context.StudentDatasetRecords.Add(newRecord);
                        result.AddedRecords++;
                    }

                    // Ayrıca öğrenci için giriş hesabı oluştur veya güncelle
                    var studentUser = await _context.StudentUsers
                        .FirstOrDefaultAsync(u => u.StudentNumber == studentNo || u.Username == studentNo || u.Email == email);

                    if (studentUser == null)
                    {
                        var newStudent = new StudentUser
                        {
                            Username = !string.IsNullOrEmpty(studentNo) ? studentNo : email.Split('@')[0],
                            Email = email,
                            FullName = fullName,
                            StudentNumber = studentNo,
                            TcNo = tcNo,
                            Nationality = nationality,
                            Department = department,
                            PhoneNumber = phone,
                            ResidentialAddress = address,
                            HealthInsuranceStatus = healthStatus,
                            CompletedInternshipDays = previousDays
                        };
                        newStudent.SetPassword("Ogrenci123!");
                        _context.StudentUsers.Add(newStudent);
                    }
                    else
                    {
                        studentUser.FullName = fullName;
                        studentUser.TcNo = tcNo;
                        studentUser.Nationality = nationality;
                        studentUser.Department = department;
                        studentUser.PhoneNumber = phone;
                        studentUser.ResidentialAddress = address;
                        studentUser.HealthInsuranceStatus = healthStatus;
                        studentUser.CompletedInternshipDays = previousDays;
                    }

                    result.TotalRecords++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Satır {lineNumber} işlenirken hata: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;
            return result;
        }

        public string GenerateSampleCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Öğrenci Ad Soyad;TC Kimlik No;Uyruk;Öğrenci No;Doğum Tarihi;İkametgâh Adresi;Bölüm;Ek-16 Teslim Yılı;Daha Önce Yapılan Staj Gün Sayısı;E-posta;Telefon No;Sağlık Yardımı Statüsü");
            sb.AppendLine("Göktuğ Büyüksarı;10000000146;TC;210202001;15.05.2003;Atatürk Mah. Cumhuriyet Cad. No: 12 Daire: 4 İzmit / Kocaeli;Yazılım Mühendisliği;2025-2026;0;ogrenci@ogr.kocaeli.edu.tr;+90 532 111 22 33;Var");
            sb.AppendLine("Ayşe Kaya;28324716298;TC;210202002;22.08.2002;İnönü Mah. İstiklal Cad. No: 45 Daire: 2 Gebze / Kocaeli;Yazılım Mühendisliği;2025-2026;20;ayse.kaya@ogr.kocaeli.edu.tr;+90 533 222 33 44;Var");
            sb.AppendLine("Ali Al-Mansoor;99123456780;Yabancı;210202099;10.01.2001;Fatih Mah. Bağdat Cad. No: 8 Daire: 6 Kadıköy / İstanbul;Bilgisayar Mühendisliği;2025-2026;0;ali.mansoor@ogr.kocaeli.edu.tr;+90 535 333 44 55;Yok");
            return sb.ToString();
        }
    }
}
