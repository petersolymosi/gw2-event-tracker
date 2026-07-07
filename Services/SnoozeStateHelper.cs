using System;
using System.Collections.Generic;
using System.Linq;

namespace Ghost.Gw2EventTracker.Services {

    internal static class SnoozeStateHelper {

        public static (string Keys, string UtcDate) Normalize(string keys, string utcDate, DateTime utcToday) {
            var today = FormatUtcDate(utcToday);

            if (string.IsNullOrWhiteSpace(keys)) {
                return (string.Empty, string.Empty);
            }

            if (string.IsNullOrWhiteSpace(utcDate)) {
                return (keys, today);
            }

            if (!utcDate.Equals(today, StringComparison.Ordinal)) {
                return (string.Empty, string.Empty);
            }

            return (keys, utcDate);
        }

        public static bool IsActiveForUtcDay(string keys, string utcDate, DateTime utcToday, string eventKey) {
            var (normalizedKeys, normalizedDate) = Normalize(keys, utcDate, utcToday);

            if (string.IsNullOrWhiteSpace(normalizedKeys) || string.IsNullOrWhiteSpace(normalizedDate)) {
                return false;
            }

            return ParseKeys(normalizedKeys).Contains(eventKey);
        }

        public static string FormatUtcDate(DateTime utcDate) => utcDate.ToString("yyyy-MM-dd");

        private static HashSet<string> ParseKeys(string keys) =>
            keys.Split(',')
                .Select(key => key.Trim())
                .Where(key => key.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

}
