using System;
using System.Collections.Generic;
using System.Linq;
using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Services.OOP_RuleEngine
{
    // OOP - STRATEGY PATTERN & İŞ MANTIĞI: Resmi tatiller ve haftalık çalışma günlerine göre staj süresini denetler (Doküman 6.3)
    public class InternshipWorkDaysValidator : BaseValidator
    {
        private readonly IHolidayService _holidayService;

        public override string RuleName => "Staj Gün Sayısı ve Resmi Tatil Kontrolü (Kısıt 6.3)";
        public override string Category => "Tarih ve Gün Hesabı";

        public InternshipWorkDaysValidator(IHolidayService holidayService)
        {
            _holidayService = holidayService;
        }

        // İŞ KURALI: Seçilen haftalık günlerden resmi tatilleri çıkararak beyan edilen gün sayısını doğrular
        public override RuleResult Evaluate(InternshipDocument document, StudentDatasetRecord? dbRecord)
        {
            if (document.StartDate == default || document.EndDate == default)
            {
                return Error(RuleName, Category, "Staj başlangıç ve bitiş tarihleri belirtilmelidir.", field: "StartDate");
            }

            if (document.EndDate < document.StartDate)
            {
                return Error(RuleName, Category, "Staj bitiş tarihi başlangıç tarihinden önce olamaz!", field: "EndDate");
            }

            // Seçilen haftalık çalışma günlerini çözümle
            var allowedDays = ParseAllowedDays(document.SelectedDaysOfWeek);

            // Resmi tatilleri düşerek net iş günü sayısını hesapla
            int calculatedWorkingDays = _holidayService.CalculateWorkingDays(
                document.StartDate, 
                document.EndDate, 
                allowedDays, 
                out List<string> excludedHolidays
            );

            // Belgede beyan edilen gün sayısı ile hesaplanan net iş günü uyuşmazlık kontrolü
            if (document.DeclaredDaysCount != calculatedWorkingDays)
            {
                string holidayNote = excludedHolidays.Count > 0 
                    ? $" (Düşülen tatiller: {string.Join(", ", excludedHolidays)})" 
                    : "";

                return Error(
                    RuleName,
                    Category,
                    $"Belgede beyan edilen staj gün sayısı ({document.DeclaredDaysCount} gün) ile resmi tatiller ve çalışma günleri hesaba katılarak hesaplanan gün sayısı ({calculatedWorkingDays} gün) uyuşmuyor!{holidayNote}",
                    $"Önerilen beyan edilen gün sayısı: {calculatedWorkingDays}",
                    "DeclaredDaysCount"
                );
            }

            string successDetail = excludedHolidays.Count > 0
                ? $" ({excludedHolidays.Count} resmi tatil günü hesaptan düşüldü: {string.Join(", ", excludedHolidays)})"
                : "";

            return Success(
                RuleName, 
                Category, 
                $"Staj tarihleri ve gün sayısı kurala tam uygundur ({calculatedWorkingDays} net iş günü).{successDetail}",
                "DeclaredDaysCount"
            );
        }

        // YARDIMCI: Seçilen gün isimlerini DayOfWeek enum değerlerine çevirir
        private static HashSet<DayOfWeek> ParseAllowedDays(string? selectedDaysStr)
        {
            var set = new HashSet<DayOfWeek>();
            if (string.IsNullOrWhiteSpace(selectedDaysStr))
            {
                set.Add(DayOfWeek.Monday);
                set.Add(DayOfWeek.Tuesday);
                set.Add(DayOfWeek.Wednesday);
                set.Add(DayOfWeek.Thursday);
                set.Add(DayOfWeek.Friday);
                return set;
            }

            var parts = selectedDaysStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                string clean = p.Trim().ToLowerInvariant();
                if (clean.Contains("pazartesi")) set.Add(DayOfWeek.Monday);
                else if (clean.Contains("salı") || clean.Contains("sali")) set.Add(DayOfWeek.Tuesday);
                else if (clean.Contains("çarşamba") || clean.Contains("carsamba")) set.Add(DayOfWeek.Wednesday);
                else if (clean.Contains("perşembe") || clean.Contains("persembe")) set.Add(DayOfWeek.Thursday);
                else if (clean.Contains("cuma")) set.Add(DayOfWeek.Friday);
                else if (clean.Contains("cumartesi")) set.Add(DayOfWeek.Saturday);
                else if (clean.Contains("pazar")) set.Add(DayOfWeek.Sunday);
            }

            if (set.Count == 0)
            {
                set.Add(DayOfWeek.Monday);
                set.Add(DayOfWeek.Tuesday);
                set.Add(DayOfWeek.Wednesday);
                set.Add(DayOfWeek.Thursday);
                set.Add(DayOfWeek.Friday);
            }

            return set;
        }
    }
}
