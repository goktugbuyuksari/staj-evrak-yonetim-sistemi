using System;
using System.Collections.Generic;

namespace StajTakipSistemi.Server.Services
{
    public interface IHolidayService
    {
        bool IsOfficialHoliday(DateTime date, out string holidayName);
        int CalculateWorkingDays(DateTime startDate, DateTime endDate, HashSet<DayOfWeek> allowedDays, out List<string> excludedHolidayDetails);
    }

    /// <summary>
    /// Türkiye Cumhuriyeti Resmi Tatil Takvimi Servisi.
    /// Sabit ve dini resmi tatilleri yönetir, staj gün hesabından bu günleri düşer (Doküman 6.3).
    /// </summary>
    public class HolidayService : IHolidayService
    {
        // Sabit tarihli resmi tatiller (Ay, Gün)
        private static readonly Dictionary<(int Month, int Day), string> FixedHolidays = new()
        {
            { (1, 1), "Yılbaşı" },
            { (4, 23), "Ulusal Egemenlik ve Çocuk Bayramı" },
            { (5, 1), "Emek ve Dayanışma Günü" },
            { (5, 19), "Atatürk'ü Anma, Gençlik ve Spor Bayramı" },
            { (7, 15), "Demokrasi ve Milli Birlik Günü" },
            { (8, 30), "Zafer Bayramı" },
            { (10, 29), "Cumhuriyet Bayramı" }
        };

        // 2025 ve 2026 yılları Dini Bayram ve Arife günleri
        private static readonly Dictionary<DateTime, string> VariableHolidays = new()
        {
            // 2025 Ramazan Bayramı (29 Mart arife, 30-31 Mart - 1 Nisan)
            { new DateTime(2025, 3, 29), "Ramazan Bayramı Arifesi" },
            { new DateTime(2025, 3, 30), "Ramazan Bayramı 1. Gün" },
            { new DateTime(2025, 3, 31), "Ramazan Bayramı 2. Gün" },
            { new DateTime(2025, 4, 1), "Ramazan Bayramı 3. Gün" },

            // 2025 Kurban Bayramı (5 Haziran arife, 6-9 Haziran)
            { new DateTime(2025, 6, 5), "Kurban Bayramı Arifesi" },
            { new DateTime(2025, 6, 6), "Kurban Bayramı 1. Gün" },
            { new DateTime(2025, 6, 7), "Kurban Bayramı 2. Gün" },
            { new DateTime(2025, 6, 8), "Kurban Bayramı 3. Gün" },
            { new DateTime(2025, 6, 9), "Kurban Bayramı 4. Gün" },

            // 2026 Ramazan Bayramı (19 Mart arife, 20-22 Mart)
            { new DateTime(2026, 3, 19), "Ramazan Bayramı Arifesi" },
            { new DateTime(2026, 3, 20), "Ramazan Bayramı 1. Gün" },
            { new DateTime(2026, 3, 21), "Ramazan Bayramı 2. Gün" },
            { new DateTime(2026, 3, 22), "Ramazan Bayramı 3. Gün" },

            // 2026 Kurban Bayramı (26 Mayıs arife, 27-30 Mayıs)
            { new DateTime(2026, 5, 26), "Kurban Bayramı Arifesi" },
            { new DateTime(2026, 5, 27), "Kurban Bayramı 1. Gün" },
            { new DateTime(2026, 5, 28), "Kurban Bayramı 2. Gün" },
            { new DateTime(2026, 5, 29), "Kurban Bayramı 3. Gün" },
            { new DateTime(2026, 5, 30), "Kurban Bayramı 4. Gün" }
        };

        public bool IsOfficialHoliday(DateTime date, out string holidayName)
        {
            DateTime d = date.Date;

            // Değişken bayram kontrolü
            if (VariableHolidays.TryGetValue(d, out var varName))
            {
                holidayName = varName;
                return true;
            }

            // Sabit tatil kontrolü
            if (FixedHolidays.TryGetValue((d.Month, d.Day), out var fixedName))
            {
                holidayName = fixedName;
                return true;
            }

            holidayName = string.Empty;
            return false;
        }

        public int CalculateWorkingDays(DateTime startDate, DateTime endDate, HashSet<DayOfWeek> allowedDays, out List<string> excludedHolidayDetails)
        {
            excludedHolidayDetails = new List<string>();
            if (endDate < startDate)
                return 0;

            int count = 0;
            DateTime current = startDate.Date;
            DateTime end = endDate.Date;

            while (current <= end)
            {
                // Seçilen haftalık gün kontrolü
                if (allowedDays.Contains(current.DayOfWeek))
                {
                    // Resmi tatil kontrolü
                    if (IsOfficialHoliday(current, out string holName))
                    {
                        excludedHolidayDetails.Add($"{current:dd.MM.yyyy} ({holName})");
                    }
                    else
                    {
                        count++;
                    }
                }
                current = current.AddDays(1);
            }

            return count;
        }
    }
}
