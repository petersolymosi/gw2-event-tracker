using System;
using System.Collections.Generic;
using System.Linq;
using Blish_HUD.Settings;
using Ghost.Gw2EventTracker.Models;
using Newtonsoft.Json;

namespace Ghost.Gw2EventTracker.Services {

    public sealed class EventProfile {

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FilterMode { get; set; } = "All Events";
        public string SortMode { get; set; } = "Next Up";
        public bool? NotificationsEnabled { get; set; }
        public bool? OverlayEnabled { get; set; }
        public bool? ShowIncompleteOnly { get; set; }
        public bool? MapHighlightEnabled { get; set; }
        public bool WatchAllEvents { get; set; }
        public bool IsBuiltIn { get; set; }
        public Dictionary<string, bool>? CategoryWatchDefaults { get; set; }
    }

    public sealed class EventProfileStore {

        public const string DefaultProfileName = "Track Everything";

        private static readonly EventProfile[] BuiltInProfiles = {
            new EventProfile {
                Name = DefaultProfileName,
                IsBuiltIn = true,
                Description = "Watch every event with notifications and overlay enabled.",
                FilterMode = "All Events",
                SortMode = "Next Up",
                NotificationsEnabled = true,
                OverlayEnabled = true,
                ShowIncompleteOnly = false,
                MapHighlightEnabled = true,
                WatchAllEvents = true
            },
            new EventProfile {
                Name = "Meta Daily Runner",
                IsBuiltIn = true,
                Description = "Focus on incomplete meta dailies, sorted by next start.",
                FilterMode = "Incomplete Rewards",
                SortMode = "Next Up",
                NotificationsEnabled = true,
                OverlayEnabled = true,
                ShowIncompleteOnly = true,
                MapHighlightEnabled = true,
                CategoryWatchDefaults = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) {
                    ["Meta Event"] = true,
                    ["World Bosses"] = false,
                    ["Day-Night Cycle"] = false
                }
            },
            new EventProfile {
                Name = "World Boss Train",
                IsBuiltIn = true,
                Description = "World bosses only, alerts and overlay on.",
                FilterMode = "World Bosses",
                SortMode = "Next Up",
                NotificationsEnabled = true,
                OverlayEnabled = true,
                ShowIncompleteOnly = false,
                MapHighlightEnabled = true,
                CategoryWatchDefaults = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) {
                    ["World Bosses"] = true,
                    ["Meta Event"] = false
                }
            },
            new EventProfile {
                Name = "Map Focus",
                IsBuiltIn = true,
                Description = "Events on your current map, with map highlighting.",
                FilterMode = "On Current Map",
                SortMode = "Next Up",
                NotificationsEnabled = true,
                OverlayEnabled = true,
                ShowIncompleteOnly = false,
                MapHighlightEnabled = true
            },
            new EventProfile {
                Name = "Quiet Tracker",
                IsBuiltIn = true,
                Description = "All events visible, notifications and overlay off.",
                FilterMode = "All Events",
                SortMode = "Alphabetical",
                NotificationsEnabled = false,
                OverlayEnabled = false,
                ShowIncompleteOnly = false,
                MapHighlightEnabled = false,
                CategoryWatchDefaults = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) {
                    ["World Bosses"] = true,
                    ["Meta Event"] = true,
                    ["Day-Night Cycle"] = true
                }
            }
        };

        private readonly ModuleSettings _settings;
        private List<EventProfile> _customProfiles = new List<EventProfile>();

        public EventProfileStore(ModuleSettings settings) {
            _settings = settings;
            LoadCustomProfiles();
        }

        public IReadOnlyList<EventProfile> GetAllProfiles() =>
            BuiltInProfiles.Concat(_customProfiles).ToList();

        public EventProfile? Find(string name) =>
            GetAllProfiles().FirstOrDefault(profile =>
                profile.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        public bool IsBuiltIn(string name) =>
            BuiltInProfiles.Any(profile => profile.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        public bool SaveCustom(EventProfile profile) {
            if (string.IsNullOrWhiteSpace(profile.Name) || IsBuiltIn(profile.Name)) {
                return false;
            }

            profile.IsBuiltIn = false;
            _customProfiles.RemoveAll(existing =>
                existing.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase));
            _customProfiles.Add(profile);
            PersistCustomProfiles();
            return true;
        }

        public bool DeleteCustom(string name) {
            if (IsBuiltIn(name)) {
                return false;
            }

            var removed = _customProfiles.RemoveAll(profile =>
                profile.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (removed == 0) {
                return false;
            }

            PersistCustomProfiles();
            if (_settings.ActiveProfileName.Equals(name, StringComparison.OrdinalIgnoreCase)) {
                _settings.ActiveProfileName = string.Empty;
            }

            return true;
        }

        public EventProfile CaptureFromCurrent(
            string name,
            string description,
            ModuleSettings settings,
            string filterMode,
            string sortMode,
            IEnumerable<TrackedEvent> events) {

            var eventList = events.ToList();
            return new EventProfile {
                Name = name.Trim(),
                Description = description?.Trim() ?? string.Empty,
                FilterMode = filterMode,
                SortMode = sortMode,
                NotificationsEnabled = settings.NotificationsEnabled,
                OverlayEnabled = settings.OverlayEnabled,
                ShowIncompleteOnly = settings.ShowIncompleteOnly,
                MapHighlightEnabled = settings.MapHighlightEnabled,
                WatchAllEvents = eventList.Count > 0 && eventList.All(tracked => tracked.IsWatched),
                CategoryWatchDefaults = EventCategoryMapper.MenuCategories
                    .ToDictionary(category => category, settings.GetCategoryWatchDefault)
            };
        }

        public static void ApplyToSettings(
            EventProfile profile,
            ModuleSettings settings,
            SettingCollection watchSettings,
            IEnumerable<TrackedEvent> events) {

            if (profile.NotificationsEnabled.HasValue) {
                settings.NotificationsEnabled = profile.NotificationsEnabled.Value;
            }

            if (profile.OverlayEnabled.HasValue) {
                settings.OverlayEnabled = profile.OverlayEnabled.Value;
            }

            if (profile.ShowIncompleteOnly.HasValue) {
                settings.ShowIncompleteOnly = profile.ShowIncompleteOnly.Value;
            }

            if (profile.MapHighlightEnabled.HasValue) {
                settings.MapHighlightEnabled = profile.MapHighlightEnabled.Value;
            }

            settings.ActiveProfileName = profile.Name;

            if (profile.WatchAllEvents) {
                foreach (var category in EventCategoryMapper.MenuCategories) {
                    settings.SetCategoryWatchDefault(category, true);
                }

                foreach (var tracked in events) {
                    tracked.IsWatched = true;
                    watchSettings.DefineSetting($"watch:{tracked.Key}", true).Value = true;
                }

                return;
            }

            if (profile.CategoryWatchDefaults == null) {
                return;
            }

            foreach (var pair in profile.CategoryWatchDefaults) {
                settings.SetCategoryWatchDefault(pair.Key, pair.Value);

                foreach (var tracked in events.Where(e =>
                             e.Category.Equals(pair.Key, StringComparison.OrdinalIgnoreCase))) {
                    tracked.IsWatched = pair.Value;
                    watchSettings.DefineSetting($"watch:{tracked.Key}", pair.Value).Value = pair.Value;
                }
            }
        }

        private void LoadCustomProfiles() {
            if (string.IsNullOrWhiteSpace(_settings.CustomProfilesJson)) {
                _customProfiles = new List<EventProfile>();
                return;
            }

            try {
                _customProfiles = JsonConvert.DeserializeObject<List<EventProfile>>(_settings.CustomProfilesJson)
                    ?? new List<EventProfile>();
            } catch (JsonException) {
                _customProfiles = new List<EventProfile>();
            }
        }

        private void PersistCustomProfiles() {
            _settings.CustomProfilesJson = JsonConvert.SerializeObject(_customProfiles);
        }
    }

}
