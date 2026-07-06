using System;

namespace Gw2EventTracker.Services {

    public static class DailyResetHelper {

        /// <summary>GW2 daily reset occurs at 00:00 UTC.</summary>
        public static DateTime UtcDay(DateTime utcNow) => utcNow.Date;

        public static bool HasUtcDayChanged(DateTime lastUtcMidnight, DateTime utcNow) =>
            UtcDay(lastUtcMidnight) != UtcDay(utcNow);

        /// <summary>GW2 daily reset occurs at 00:00 UTC.</summary>
        public static DateTime NextResetUtc(DateTime utcNow) => utcNow.Date.AddDays(1);

        public static TimeSpan TimeUntilResetUtc(DateTime utcNow) {
            var remaining = NextResetUtc(utcNow) - utcNow;
            return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }

        public static string FormatCountdown(DateTime utcNow) {
            var remaining = TimeUntilResetUtc(utcNow);
            if (remaining.TotalHours >= 1) {
                return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
            }

            return $"{remaining.Minutes}m {remaining.Seconds}s";
        }
    }

}
