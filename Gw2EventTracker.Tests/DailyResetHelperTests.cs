using System;
using Gw2EventTracker.Services;
using Xunit;

namespace Gw2EventTracker.Tests {

    public sealed class DailyResetHelperTests {

        [Fact]
        public void NextResetUtc_IsMidnightAfterCurrentDay() {
            var now = new DateTime(2026, 7, 6, 14, 30, 0, DateTimeKind.Utc);

            var reset = DailyResetHelper.NextResetUtc(now);

            Assert.Equal(new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc), reset);
        }

        [Fact]
        public void TimeUntilResetUtc_CountsDownToMidnight() {
            var now = new DateTime(2026, 7, 6, 23, 45, 30, DateTimeKind.Utc);

            var remaining = DailyResetHelper.TimeUntilResetUtc(now);

            Assert.Equal(TimeSpan.FromMinutes(14) + TimeSpan.FromSeconds(30), remaining);
        }

        [Fact]
        public void FormatCountdown_UsesHoursWhenOverOneHour() {
            var now = new DateTime(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc);

            var text = DailyResetHelper.FormatCountdown(now);

            Assert.Equal("14h 0m", text);
        }

        [Fact]
        public void FormatCountdown_UsesMinutesWhenUnderOneHour() {
            var now = new DateTime(2026, 7, 6, 23, 50, 15, DateTimeKind.Utc);

            var text = DailyResetHelper.FormatCountdown(now);

            Assert.Equal("9m 45s", text);
        }

        [Fact]
        public void HasUtcDayChanged_ReturnsFalseForSameUtcDay() {
            var day = new DateTime(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);

            Assert.False(DailyResetHelper.HasUtcDayChanged(day, day.AddHours(6)));
        }

        [Fact]
        public void HasUtcDayChanged_ReturnsTrueAcrossUtcMidnight() {
            var before = new DateTime(2026, 7, 6, 23, 59, 0, DateTimeKind.Utc);
            var after = new DateTime(2026, 7, 7, 0, 1, 0, DateTimeKind.Utc);

            Assert.True(DailyResetHelper.HasUtcDayChanged(before, after));
        }
    }

}
