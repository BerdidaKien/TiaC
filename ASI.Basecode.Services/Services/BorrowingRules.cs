using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Services
{
    // The time rules a student's request must follow. The student form shows the same rules,
    // but this is the real check because a form can be bypassed.
    public static class BorrowingRules
    {
        // Returns an error message, or null when the request follows the rules
        public static string CheckSchedule(BorrowingSettings s, DateTime pickup, DateTime ret, DateTime now)
        {
            if (pickup < now.AddMinutes(-5))
                return "The pickup time can't be in the past.";

            if (ret <= pickup)
                return "The return time must be after the pickup time.";

            if ((ret - pickup).TotalDays > s.MaxDurationDays)
                return $"You can borrow equipment for up to {s.MaxDurationDays} day(s).";

            if (pickup > now.AddDays(s.AdvanceBookingDays))
                return $"You can only reserve up to {s.AdvanceBookingDays} day(s) ahead.";

            var open = ParseTime(s.OpeningTime, new TimeSpan(8, 0, 0));
            var close = ParseTime(s.ClosingTime, new TimeSpan(17, 0, 0));

            if (pickup.TimeOfDay < open || pickup.TimeOfDay > close ||
                ret.TimeOfDay < open || ret.TimeOfDay > close)
            {
                return $"Pickup and return times must be between {Clock(open)} and {Clock(close)}.";
            }

            return null;
        }

        private static TimeSpan ParseTime(string text, TimeSpan fallback) =>
            TimeSpan.TryParseExact(text ?? "", @"hh\:mm", CultureInfo.InvariantCulture, out var t) ? t : fallback;

        private static string Clock(TimeSpan t) =>
            DateTime.Today.Add(t).ToString("h:mm tt", CultureInfo.InvariantCulture);
    }
}