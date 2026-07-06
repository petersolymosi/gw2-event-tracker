using System;
using Gw2EventTracker.Services;
using Xunit;

namespace Gw2EventTracker.Tests {

    public sealed class SnoozeStateHelperTests {

        private static readonly DateTime Today = new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Normalize_LegacyKeysWithoutDate_AssignsToday() {
            var (keys, date) = SnoozeStateHelper.Normalize("1:0,2:1", string.Empty, Today);

            Assert.Equal("1:0,2:1", keys);
            Assert.Equal("2026-07-06", date);
        }

        [Fact]
        public void Normalize_StaleDate_ClearsSnoozes() {
            var (keys, date) = SnoozeStateHelper.Normalize("1:0", "2026-07-05", Today);

            Assert.Equal(string.Empty, keys);
            Assert.Equal(string.Empty, date);
        }

        [Fact]
        public void Normalize_EmptyKeys_ClearsOrphanedDate() {
            var (keys, date) = SnoozeStateHelper.Normalize(string.Empty, "2026-07-06", Today);

            Assert.Equal(string.Empty, keys);
            Assert.Equal(string.Empty, date);
        }

        [Fact]
        public void Normalize_SameDayKeys_KeepsState() {
            var (keys, date) = SnoozeStateHelper.Normalize("3:2", "2026-07-06", Today);

            Assert.Equal("3:2", keys);
            Assert.Equal("2026-07-06", date);
        }

        [Fact]
        public void IsActiveForUtcDay_ReturnsTrueForSnoozedKeyToday() {
            var active = SnoozeStateHelper.IsActiveForUtcDay("1:0", "2026-07-06", Today, "1:0");

            Assert.True(active);
        }

        [Fact]
        public void IsActiveForUtcDay_ReturnsFalseForYesterdaySnooze() {
            var active = SnoozeStateHelper.IsActiveForUtcDay("1:0", "2026-07-05", Today, "1:0");

            Assert.False(active);
        }

        [Fact]
        public void IsActiveForUtcDay_LegacySnoozeWithoutDate_IsActiveToday() {
            var active = SnoozeStateHelper.IsActiveForUtcDay("1:0", string.Empty, Today, "1:0");

            Assert.True(active);
        }

        [Fact]
        public void IsActiveForUtcDay_IsCaseInsensitiveForKeys() {
            var active = SnoozeStateHelper.IsActiveForUtcDay("1:0", "2026-07-06", Today, "1:0");

            Assert.True(active);
        }
    }

}
