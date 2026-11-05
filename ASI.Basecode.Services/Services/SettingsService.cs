using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Services
{
    public class SettingsService : ISettingsService
    {
        private const string KeyMaxActive = "max_active_items";
        private BorrowingSettings _cached;
        private const string KeyDefaultHours = "default_duration_hours";
        private const string KeyMaxDays = "max_duration_days";
        private const string KeyAdvanceDays = "advance_booking_days";
        private const string KeyGrace = "overdue_grace_minutes";
        private const string KeyDueSoon = "due_soon_hours";
        private const string KeyOpen = "opening_time";
        private const string KeyClose = "closing_time";

        private readonly ISettingsRepository _repo;

        public SettingsService(ISettingsRepository repo)
        {
            _repo = repo;
        }

        // Saved values, or the defaults for anything not saved yet
        public BorrowingSettings Get() => _cached ??= Load();
        public BorrowingSettings Load()
        {
            var rows = _repo.GetAll().ToDictionary(r => r.SettingKey, r => r.SettingValue);
            var s = new BorrowingSettings();

            s.MaxActiveItems = ReadInt(rows, KeyMaxActive, s.MaxActiveItems);
            s.DefaultDurationHours = ReadInt(rows, KeyDefaultHours, s.DefaultDurationHours);
            s.MaxDurationDays = ReadInt(rows, KeyMaxDays, s.MaxDurationDays);
            s.AdvanceBookingDays = ReadInt(rows, KeyAdvanceDays, s.AdvanceBookingDays);
            s.OverdueGraceMinutes = ReadInt(rows, KeyGrace, s.OverdueGraceMinutes);
            s.DueSoonHours = ReadInt(rows, KeyDueSoon, s.DueSoonHours);
            s.OpeningTime = ReadTime(rows, KeyOpen, s.OpeningTime);
            s.ClosingTime = ReadTime(rows, KeyClose, s.ClosingTime);
            return s;
        }

        public ServiceResult Save(BorrowingSettings s, string userCode)
        {
            if (s == null) return ServiceResult.Fail("No settings were submitted.");

            if (!InRange(s.MaxActiveItems, 1, 10))
                return ServiceResult.Fail("Max active items must be between 1 and 10.");
            if (!InRange(s.DefaultDurationHours, 1, 72))
                return ServiceResult.Fail("Default reservation duration must be between 1 and 72 hours.");
            if (!InRange(s.MaxDurationDays, 1, 30))
                return ServiceResult.Fail("Max reservation duration must be between 1 and 30 days.");
            if (!InRange(s.AdvanceBookingDays, 1, 60))
                return ServiceResult.Fail("Advance booking limit must be between 1 and 60 days.");
            if (!InRange(s.OverdueGraceMinutes, 0, 240))
                return ServiceResult.Fail("Overdue grace period must be between 0 and 240 minutes.");
            if (!InRange(s.DueSoonHours, 1, 48))
                return ServiceResult.Fail("Due-soon window must be between 1 and 48 hours.");

            if (!TryTime(s.OpeningTime, out var open) || !TryTime(s.ClosingTime, out var close))
                return ServiceResult.Fail("Please enter valid opening and closing times.");
            if (open >= close)
                return ServiceResult.Fail("Opening time must be earlier than closing time.");

            if (s.DefaultDurationHours > s.MaxDurationDays * 24)
                return ServiceResult.Fail("The default duration can't be longer than the maximum duration.");

            var values = new Dictionary<string, string>
            {
                [KeyMaxActive] = s.MaxActiveItems.ToString(CultureInfo.InvariantCulture),
                [KeyDefaultHours] = s.DefaultDurationHours.ToString(CultureInfo.InvariantCulture),
                [KeyMaxDays] = s.MaxDurationDays.ToString(CultureInfo.InvariantCulture),
                [KeyAdvanceDays] = s.AdvanceBookingDays.ToString(CultureInfo.InvariantCulture),
                [KeyGrace] = s.OverdueGraceMinutes.ToString(CultureInfo.InvariantCulture),
                [KeyDueSoon] = s.DueSoonHours.ToString(CultureInfo.InvariantCulture),
                [KeyOpen] = open.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
                [KeyClose] = close.ToString(@"hh\:mm", CultureInfo.InvariantCulture)
            };

            try
            {
                _repo.SaveAll(values, userCode);
                _cached = null;
            }
            catch (Exception)
            {
                return ServiceResult.Fail("Could not save the settings. Please try again.");
            }

            return ServiceResult.Ok("Settings saved.");
        }

        // ---------- helpers ----------
        private static bool InRange(int value, int min, int max) => value >= min && value <= max;

        private static bool TryTime(string text, out TimeSpan time) =>
            TimeSpan.TryParseExact((text ?? "").Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out time);

        private static int ReadInt(Dictionary<string, string> rows, string key, int fallback) =>
            rows.TryGetValue(key, out var v) &&
            int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
                ? n : fallback;

        private static string ReadTime(Dictionary<string, string> rows, string key, string fallback) =>
            rows.TryGetValue(key, out var v) && TryTime(v, out _) ? v : fallback;
    }
}