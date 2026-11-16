using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            context.Database.EnsureCreated();

            // SQLite Şema Kontrolü ve Eksik Kolonların Otomatik Eklenmesi (Farklı bilgisayarlardaki eski staj_takip.db uyumluluğu için)
            try
            {
                var conn = context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) conn.Open();

                void EnsureColumns(string tableName, Dictionary<string, string> columns)
                {
                    var existingCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"PRAGMA table_info({tableName});";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            existingCols.Add(reader.GetString(1));
                        }
                    }

                    foreach (var col in columns)
                    {
                        if (!existingCols.Contains(col.Key))
                        {
                            using var alterCmd = conn.CreateCommand();
                            alterCmd.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {col.Key} {col.Value};";
                            alterCmd.ExecuteNonQuery();
                        }
                    }
                }

                EnsureColumns("InternshipDocuments", new Dictionary<string, string>
                {
                    { "ExtractedText", "TEXT DEFAULT ''" },
                    { "ValidationReportJson", "TEXT DEFAULT ''" },
                    { "GoogleFormUrl", "TEXT NULL" },
                    { "FaxNumber", "TEXT NULL" },
                    { "EngineerGraduationDepartment", "TEXT NULL" },
                    { "EngineerCount", "INTEGER DEFAULT 1" },
                    { "HasEmployerSignatureAndStamp", "INTEGER DEFAULT 1" },
                    { "HasStudentSignature", "INTEGER DEFAULT 0" },
                    { "HasAdvisorSignature", "INTEGER DEFAULT 0" },
                    { "UpdatedAt", "TEXT NULL" }
                });

                EnsureColumns("Users", new Dictionary<string, string>
                {
                    { "CompletedInternshipDays", "INTEGER DEFAULT 0" },
                    { "DepartmentTitle", "TEXT NULL" },
                    { "IsSuperAdmin", "INTEGER DEFAULT 0" },
                    { "FailedLoginAttempts", "INTEGER DEFAULT 0" },
                    { "IsLocked", "INTEGER DEFAULT 0" },
                    { "LockoutEndTime", "TEXT NULL" }
                });
            }
            catch { }

            // Admin kullanıcısı ekle
            if (!context.AdminUsers.Any())
            {
                var admin = new AdminUser
                {
                    Username = "admin",
                    Email = "admin@kocaeli.edu.tr",
                    FullName = "Prof. Dr. Staj Komisyonu Başkanı",
                    DepartmentTitle = "Yazılım Mühendisliği Staj Komisyonu",
                    IsSuperAdmin = true
                };
                admin.SetPassword("Admin123!");
                context.AdminUsers.Add(admin);
            }

            // Öğrenci kullanıcısı ekle
            if (!context.StudentUsers.Any())
            {
                var student = new StudentUser
                {
                    Username = "ogrenci",
                    Email = "ogrenci@ogr.kocaeli.edu.tr",
                    FullName = "Göktuğ Büyüksarı",
                    StudentNumber = "250229061",
                    TcNo = "10000000146",
                    Nationality = "TC",
                    Department = "Yazılım Mühendisliği",
                    PhoneNumber = "+90 535 100 44 40",
                    ResidentialAddress = "Atatürk Mah. Cumhuriyet Cad. No: 12 Daire: 4 İzmit / Kocaeli",
                    HealthInsuranceStatus = "Var",
                    CompletedInternshipDays = 0
                };
                student.SetPassword("Ogrenci123!");
                context.StudentUsers.Add(student);
            }

            // Admin Test Veri Seti (Doküman Bölüm 4.3)
            if (!context.StudentDatasetRecords.Any())
            {
                context.StudentDatasetRecords.AddRange(
                    new StudentDatasetRecord
                    {
                        FullName = "Göktuğ Büyüksarı",
                        TcNo = "10000000146",
                        Nationality = "TC",
                        StudentNumber = "250229061",
                        BirthDate = "15.05.2003",
                        ResidentialAddress = "Atatürk Mah. Cumhuriyet Cad. No: 12 Daire: 4 İzmit / Kocaeli",
                        Department = "Yazılım Mühendisliği",
                        SubmissionYear = "2025-2026",
                        CompletedInternshipDays = 0,
                        Email = "ogrenci@ogr.kocaeli.edu.tr",
                        PhoneNumber = "+90 535 100 44 40",
                        HealthInsuranceStatus = "Var"
                    },
                    new StudentDatasetRecord
                    {
                        FullName = "Ayşe Kaya",
                        TcNo = "28324716298",
                        Nationality = "TC",
                        StudentNumber = "210202002",
                        BirthDate = "22.08.2002",
                        ResidentialAddress = "İnönü Mah. İstiklal Cad. No: 45 Daire: 2 Gebze / Kocaeli",
                        Department = "Yazılım Mühendisliği",
                        SubmissionYear = "2025-2026",
                        CompletedInternshipDays = 20,
                        Email = "ayse.kaya@ogr.kocaeli.edu.tr",
                        PhoneNumber = "+90 533 222 33 44",
                        HealthInsuranceStatus = "Var"
                    },
                    new StudentDatasetRecord
                    {
                        FullName = "Ali Al-Mansoor",
                        TcNo = "99123456780",
                        Nationality = "Yabancı",
                        StudentNumber = "210202099",
                        BirthDate = "10.01.2001",
                        ResidentialAddress = "Fatih Mah. Bağdat Cad. No: 8 Daire: 6 Kadıköy / İstanbul",
                        Department = "Bilgisayar Mühendisliği",
                        SubmissionYear = "2025-2026",
                        CompletedInternshipDays = 0,
                        Email = "ali.mansoor@ogr.kocaeli.edu.tr",
                        PhoneNumber = "+90 535 459 47 56",
                        HealthInsuranceStatus = "Yok"
                    }
                );
            }

            context.SaveChanges();
        }
    }
}
