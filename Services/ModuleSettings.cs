using System;
using System.Collections.Generic;
using System.Linq;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;

namespace Gw2EventTracker.Services {

    public sealed class ModuleSettings {

        private readonly SettingEntry<bool> _notificationsEnabled;
        private readonly SettingEntry<bool> _chimeEnabled;
        private readonly SettingEntry<bool> _suppressCompletedAlerts;
        private readonly SettingEntry<bool> _showIncompleteOnly;
        private readonly SettingEntry<bool> _eventStartedAlerts;
        private readonly SettingEntry<string> _alertLeadTimes;
        private readonly SettingEntry<Point> _notificationPosition;
        private readonly SettingEntry<bool> _overlayEnabled;
        private readonly SettingEntry<Point> _overlayPosition;
        private readonly SettingEntry<float> _overlayOpacity;
        private readonly SettingEntry<int> _overlayMaxEvents;
        private readonly SettingEntry<bool> _useRemoteSchedule;
        private readonly SettingEntry<string> _snoozedEventKeys;
        private readonly SettingEntry<string> _snoozeUtcDate;
        private readonly SettingEntry<bool> _mapHighlightEnabled;
        private readonly SettingEntry<string> _activeProfileName;
        private readonly SettingEntry<string> _customProfilesJson;
        private readonly SettingCollection _categoryWatchSettings;

        public ModuleSettings(SettingCollection settings) {
            var managed = settings.AddSubCollection("Managed Settings");

            _notificationsEnabled = managed.DefineSetting("notificationsEnabled", true);
            _chimeEnabled = managed.DefineSetting("chimeEnabled", true);
            _notificationPosition = managed.DefineSetting("notificationPosition", new Point(180, 60));
            _suppressCompletedAlerts = managed.DefineSetting("suppressCompletedAlerts", true);
            _showIncompleteOnly = managed.DefineSetting("showIncompleteOnly", false);
            _eventStartedAlerts = managed.DefineSetting("eventStartedAlerts", true);
            _alertLeadTimes = managed.DefineSetting("alertLeadTimes", "15,10,5");
            _overlayEnabled = managed.DefineSetting("overlayEnabled", true);
            _overlayPosition = managed.DefineSetting("overlayPosition", new Point(20, 220));
            _overlayOpacity = managed.DefineSetting("overlayOpacity", 0.85f);
            _overlayMaxEvents = managed.DefineSetting("overlayMaxEvents", 5);
            _useRemoteSchedule = managed.DefineSetting("useRemoteSchedule", true);
            _snoozedEventKeys = managed.DefineSetting("snoozedEventKeys", string.Empty);
            _snoozeUtcDate = managed.DefineSetting("snoozeUtcDate", string.Empty);
            _mapHighlightEnabled = managed.DefineSetting("mapHighlightEnabled", true);
            _activeProfileName = managed.DefineSetting("activeProfileName", string.Empty);
            _customProfilesJson = managed.DefineSetting("customProfilesJson", string.Empty);

            _categoryWatchSettings = settings.AddSubCollection("CategoryDefaults");

            NormalizeSnoozesForUtcDay();
        }

        /// <summary>
        /// Migrates legacy snoozes (keys without a date) and clears keys from a previous UTC day.
        /// </summary>
        public void NormalizeSnoozesForUtcDay() {
            var (keys, date) = SnoozeStateHelper.Normalize(
                _snoozedEventKeys.Value,
                _snoozeUtcDate.Value,
                DateTime.UtcNow.Date);

            _snoozedEventKeys.Value = keys;
            _snoozeUtcDate.Value = date;
        }

        public bool NotificationsEnabled {
            get => _notificationsEnabled.Value;
            set => _notificationsEnabled.Value = value;
        }

        public bool ChimeEnabled {
            get => _chimeEnabled.Value;
            set => _chimeEnabled.Value = value;
        }

        public bool SuppressCompletedAlerts {
            get => _suppressCompletedAlerts.Value;
            set => _suppressCompletedAlerts.Value = value;
        }

        public bool ShowIncompleteOnly {
            get => _showIncompleteOnly.Value;
            set => _showIncompleteOnly.Value = value;
        }

        public bool EventStartedAlerts {
            get => _eventStartedAlerts.Value;
            set => _eventStartedAlerts.Value = value;
        }

        public bool UseRemoteSchedule {
            get => _useRemoteSchedule.Value;
            set => _useRemoteSchedule.Value = value;
        }

        public bool MapHighlightEnabled {
            get => _mapHighlightEnabled.Value;
            set => _mapHighlightEnabled.Value = value;
        }

        public string ActiveProfileName {
            get => _activeProfileName.Value;
            set => _activeProfileName.Value = value ?? string.Empty;
        }

        public string CustomProfilesJson {
            get => _customProfilesJson.Value;
            set => _customProfilesJson.Value = value ?? string.Empty;
        }

        public Point NotificationPosition {
            get => _notificationPosition.Value;
            set => _notificationPosition.Value = value;
        }

        internal SettingEntry<Point> NotificationPositionSetting => _notificationPosition;

        public bool OverlayEnabled {
            get => _overlayEnabled.Value;
            set => _overlayEnabled.Value = value;
        }

        public Point OverlayPosition {
            get => _overlayPosition.Value;
            set => _overlayPosition.Value = value;
        }

        internal SettingEntry<Point> OverlayPositionSetting => _overlayPosition;

        public float OverlayOpacity {
            get => _overlayOpacity.Value;
            set => _overlayOpacity.Value = value;
        }

        public int OverlayMaxEvents {
            get => Math.Max(1, Math.Min(8, _overlayMaxEvents.Value));
            set => _overlayMaxEvents.Value = Math.Max(1, Math.Min(8, value));
        }

        public IReadOnlyList<int> AlertLeadTimes => ParseLeadTimes(_alertLeadTimes.Value);

        public void SetAlertLeadTimes(string value) => _alertLeadTimes.Value = value;

        public string AlertLeadTimesRaw => _alertLeadTimes.Value;

        public bool GetCategoryWatchDefault(string category) {
            return _categoryWatchSettings.DefineSetting(
                NormalizeCategoryKey(category),
                GetBuiltInCategoryWatchDefault(category)).Value;
        }

        public void SetCategoryWatchDefault(string category, bool watched) {
            _categoryWatchSettings.DefineSetting(NormalizeCategoryKey(category), watched).Value = watched;
        }

        public bool IsSnoozed(string eventKey) {
            return SnoozeStateHelper.IsActiveForUtcDay(
                _snoozedEventKeys.Value,
                _snoozeUtcDate.Value,
                DateTime.UtcNow.Date,
                eventKey);
        }

        public void SnoozeUntilReset(string eventKey) {
            var keys = ParseSnoozedKeys();
            keys.Add(eventKey);
            _snoozedEventKeys.Value = string.Join(",", keys);
            _snoozeUtcDate.Value = SnoozeStateHelper.FormatUtcDate(DateTime.UtcNow.Date);
        }

        public void ClearSnoozes() {
            _snoozedEventKeys.Value = string.Empty;
            _snoozeUtcDate.Value = string.Empty;
        }

        public static bool GetBuiltInCategoryWatchDefault(string category) {
            return category switch {
                "World Bosses" => true,
                "Meta Event" => true,
                "Day-Night Cycle" => true,
                _ => false
            };
        }

        private static string NormalizeCategoryKey(string category) =>
            $"watch:{category.Replace(' ', '_')}";

        private HashSet<string> ParseSnoozedKeys() {
            if (string.IsNullOrWhiteSpace(_snoozedEventKeys.Value)) {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            return _snoozedEventKeys.Value
                .Split(',')
                .Select(key => key.Trim())
                .Where(key => key.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static List<int> ParseLeadTimes(string raw) =>
            raw.Split(',')
                .Select(v => int.TryParse(v.Trim(), out var minutes) ? minutes : (int?)null)
                .Where(v => v.HasValue && v.Value > 0)
                .Select(v => v!.Value)
                .Distinct()
                .OrderByDescending(v => v)
                .ToList();

        public double AlertCheckIntervalSeconds => 30;
        public float NotificationDurationSeconds => 10f;
    }

}
