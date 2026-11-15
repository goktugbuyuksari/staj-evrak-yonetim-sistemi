using System;
using System.Collections.Generic;
using System.Linq;
using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Services.OOP_RuleEngine
{
    /// <summary>
    /// Doküman Bölüm 6.3:
    /// Staj gün sayısı kontrolü:
    /// 1. Öğrenci staj yapacağı haftalık günleri seçer (örn. Pazartesi, Salı, Çarşamba) ve tarih aralığını belirler.
    /// 2. Sistem bu aralıkta kalan staj günlerini hesaplar. Resmi tatiller hesaptan çıkarılır.
    /// 3. Hesaplanan günler, yüklenen belgedeki staj tarihleriyle/gün sayısıyla karşılaştırılır. Uyuşmazlık raporda gösterilir.
    /// </summary>
    public class InternshipWorkDaysValidator : BaseValidator
    {
        private readonly IHolidayService _holidayService;

        public override string RuleName => "Staj Gün Sayısı ve Resmi Tatil Kontrolü";
        public override string Category => "Mantık ve Tarih Kontrolleri";

        public InternshipWorkDaysValidator(IHolidayService holidayService)
        {
            _holidayService = holidayService;
        }

        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            if (document.StartDate == default || document.EndDate == default)
            {
                return Error(RuleName, Category, "Staj başlangıç ve bitiş tarihleri belirtilmelidir.", "Tarih aralığını seçiniz.", "StartDate");
            }

            if (document.EndDate < document.StartDate)
            {
                return Error(RuleName, Category, "Staj bitiş tarihi, başlangıç tarihinden önce olamaz.", "Geçerli bir tarih aralığı giriniz.", "EndDate");
            }

            // Seçilen haftalık günleri ayrıştır
            var allowedDays = ParseSelectedDays(document.SelectedDaysOfWeek);
            if (allowedDays.Count == 0)
            {
                // Varsayılan hafta içi (Pzt-Cuma)
                allowedDays = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
            }

            // Hesapla (Resmi tatiller hesaptan çıkarılır)
            int calculatedDays = _holidayService.CalculateWorkingDays(document.StartDate, document.EndDate, allowedDays, out var excludedHolidays);

            string holidayNote = excludedHolidays.Count > 0
                ? $" (Tarih aralığına denk gelen ve hesaptan düşülen resmi tatiller: {string.Join(", ", excludedHolidays)})"
                : "";

            int declaredDays = document.DeclaredDaysCount;

            // Uyuşmazlık kontrolü
            if (calculatedDays != declaredDays)
            {
                int diff = Math.Abs(calculatedDays - declaredDays);
                string direction = calculatedDays > declaredDays ? "fazla" : "eksik";

                return Error(RuleName, Category,
                    $"Staj gün sayısı uyuşmazlığı tespit edildi! Belgede beyan edilen: {declaredDays} gün, Tarih aralığı ve tatiller düşülerek hesaplanan net gün: {calculatedDays} gün. ({diff} gün {direction}).{holidayNote}",
                    $"Beyan edilen staj gün sayısını {calculatedDays} olarak güncelleyiniz veya tarih aralığınızı düzenleyiniz.",
                    "DeclaredDaysCount");
            }

            return Success(RuleName, Category,
                $"Staj gün sayısı ({declaredDays} gün) ile resmi tatiller düşülerek hesaplanan net iş günü ({calculatedDays} gün) birebir uyumlu.{holidayNote}",
                "DeclaredDaysCount");
        }

        private static HashSet<DayOfWeek> ParseSelectedDays(string? selectedDaysStr)
        {
            var result = new HashSet<DayOfWeek>();
            if (string.IsNullOrWhiteSpace(selectedDaysStr))
                return result;

            var parts = selectedDaysStr.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                string p = part.Trim().ToLowerInvariant();
                if (p.Contains("pazartesi") || p == "mon" || p == "monday") result.Add(DayOfWeek.Monday);
                else if (p.Contains("salı") || p.Contains("sali") || p == "tue" || p == "tuesday") result.Add(DayOfWeek.Tuesday);
                else if (p.Contains("çarşamba") || p.Contains("carsamba") || p == "wed" || p == "wednesday") result.Add(DayOfWeek.Wednesday);
                else if (p.Contains("perşembe") || p.Contains("persembe") || p == "thu" || p == "thursday") result.Add(DayOfWeek.Thursday);
                else if (p.Contains("cuma") || p == "fri" || p == "friday") result.Add(DayOfWeek.Friday);
                else if (p.Contains("cumartesi") || p == "sat" || p == "saturday") result.Add(DayOfWeek.Saturday);
                else if (p.Contains("pazar") || p == "sun" || p == "sunday") result.Add(DayOfWeek.Sunday);
            }
            return result;
        }
    }
}
