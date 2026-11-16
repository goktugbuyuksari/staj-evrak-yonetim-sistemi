using System;
using System.Collections.Generic;

namespace StajTakipSistemi.Server.Services
{
    // SERVİS ARAYÜZÜ: Resmi tatil ve iş günü hesaplama sözleşmesi
    public interface IHolidayService
    {
        bool IsOfficialHoliday(DateTime date, out string holidayName);
        int CalculateWorkingDays(DateTime startDate, DateTime endDate, HashSet<DayOfWeek> allowedDays, out List<string> excludedHolidayDetails);
    }

    // SERVİS: Türkiye Cumhuriyeti sabit ve dini resmi tatil takvimi ve net iş günü hesaplayıcı
    public class HolidayService : IHolidayService
    {
        // SABİT RESMİ TATİLLER: Her yıl aynı gün kutlanan milli bayramlar (Ay, Gün)
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

        // DEĞİŞKEN RESMİ TATİLLER: 2025 ve 2026 yılları Dini Bayram ve Arife günleri
        private static readonly Dictionary<DateTime, string> VariableHolidays = new()
        {
            // 2025 Ramazan Bayramı
            { new DateTime(2025, 3, 29), "Ramazan Bayramı Arifesi" },
            { new DateTime(2025, 3, 30), "Ramazan Bayramı 1. Gün" },
            { new DateTime(2025, 3, 31), "Ramazan Bayramı 2. Gün" },
            { new DateTime(2025, 4, 1), "Ramazan Bayramı 3. Gün" },

            // 2025 Kurban Bayramı
            { new DateTime(2025, 6, 5), "Kurban Bayramı Arifesi" },
            { new DateTime(2025, 6, 6), "Kurban Bayramı 1. Gün" },
            { new DateTime(2025, 6, 7), "Kurban Bayramı 2. Gün" },
            { new DateTime(2025, 6, 8), "Kurban Bayramı 3. Gün" },
            { new DateTime(2025, 6, 9), "Kurban Bayramı 4. Gün" },

            // 2026 Ramazan Bayramı
            { new DateTime(2026, 3, 19), "Ramazan Bayramı Arifesi" },
            { new DateTime(2026, 3, 20), "Ramazan Bayramı 1. Gün" },
            { new DateTime(2026, 3, 21), "Ramazan Bayramı 2. Gün" },
            { new DateTime(2026, 3, 22), "Ramazan Bayramı 3. Gün" },

            // 2026 Kurban Bayramı
            { new DateTime(2026, 5, 26), "Kurban Bayramı Arifesi" },
            { new DateTime(2026, 5, 27), "Kurban Bayramı 1. Gün" },
            { new DateTime(2026, 5, 28), "Kurban Bayramı 2. Gün" },
            { new DateTime(2026, 5, 29), "Kurban Bayramı 3. Gün" },
            { new DateTime(2026, 5, 30), "Kurban Bayramı 4. Gün" }
        };

        // KONTROL: Verilen tarihin resmi tatile denk gelip gelmediğini kontrol eder
        public bool IsOfficialHoliday(DateTime date, out string holidayName)
        {
            DateTime dateOnly = date.Date;

            if (VariableHolidays.TryGetValue(dateOnly, out var varName))
            {
                holidayName = varName;
                return true;
            }

            var key = (dateOnly.Month, dateOnly.Day);
            if (FixedHolidays.TryGetValue(key, out var fixedName))
            {
                holidayName = fixedName;
                return true;
            }

            holidayName = string.Empty;
            return false;
        }

        // İŞ KURALI (Doküman 6.3): Başlangıç ve bitiş arasındaki resmi tatilleri düşerek net iş gününü hesaplar
        public int CalculateWorkingDays(DateTime startDate, DateTime endDate, HashSet<DayOfWeek> allowedDays, out List<string> excludedHolidayDetails)
        {
            excludedHolidayDetails = new List<string>();
            if (startDate > endDate) return 0;

            int workingDays = 0;
            DateTime current = startDate.Date;
            DateTime end = endDate.Date;

            while (current <= end)
            {
                if (allowedDays.Contains(current.DayOfWeek))
                {
                    if (IsOfficialHoliday(current, out string holidayName))
                    {
                        excludedHolidayDetails.Add($"{current:dd.MM.yyyy} ({holidayName})");
                    }
                    else
                    {
                        workingDays++;
                    }
                }
                current = current.AddDays(1);
            }

            return workingDays;
        }
    }
}
