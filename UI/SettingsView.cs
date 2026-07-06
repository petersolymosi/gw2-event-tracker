using System;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Gw2EventTracker.Services;
using Microsoft.Xna.Framework;

namespace Gw2EventTracker.UI {

    public sealed class SettingsView : View {

        private const int Left = 32;
        private const int ContentWidth = 400;
        private const int Gap = 18;

        private readonly ModuleSettings _settings;
        private readonly EventProfileStore _profileStore;
        private readonly bool _usedRemoteSchedule;

        public SettingsView(ModuleSettings settings, EventProfileStore profileStore, bool usedRemoteSchedule) {
            _settings = settings;
            _profileStore = profileStore;
            _usedRemoteSchedule = usedRemoteSchedule;
        }

        protected override void Build(Container buildPanel) {
            buildPanel.Width = 480;

            var content = new Panel {
                Width = 480,
                Parent = buildPanel
            };

            var y = 24;

            var mapHighlight = new Checkbox {
                Text = "Highlight events on your current map (MumbleLink)",
                Checked = _settings.MapHighlightEnabled,
                Width = ContentWidth,
                Location = new Point(Left, y),
                Parent = content
            };
            mapHighlight.CheckedChanged += (_, e) => {
                _settings.MapHighlightEnabled = e.Checked;
                EventTrackerModule.Instance.RefreshTrackerList();
            };
            y = mapHighlight.Bottom + Gap;

            var setNotificationPosition = new StandardButton {
                Text = "Set Notification Position",
                Width = ContentWidth,
                Location = new Point(Left, y),
                Parent = content
            };
            setNotificationPosition.Click += (_, __) => EventTrackerModule.Instance.ShowSetNotificationPositions();
            y = setNotificationPosition.Bottom + Gap;

            var setOverlayPosition = new StandardButton {
                Text = "Set Next Up Overlay Position",
                Width = ContentWidth,
                Location = new Point(Left, y),
                Parent = content
            };
            setOverlayPosition.Click += (_, __) => EventTrackerModule.Instance.ShowSetOverlayPosition();
            y = setOverlayPosition.Bottom + Gap;

            var overlayEnabled = new Checkbox {
                Text = "Show Next Up overlay on screen",
                Checked = _settings.OverlayEnabled,
                Width = ContentWidth,
                Location = new Point(Left, y),
                Parent = content
            };
            overlayEnabled.CheckedChanged += (_, e) => _settings.OverlayEnabled = e.Checked;
            y = overlayEnabled.Bottom + Gap;

            var overlayEventsLabel = new Label {
                Text = "Overlay event count (1-8):",
                Width = ContentWidth,
                Location = new Point(Left, y),
                Parent = content
            };
            y = overlayEventsLabel.Bottom + 8;

            var overlayEvents = new TextBox {
                Text = _settings.OverlayMaxEvents.ToString(),
                Width = 80,
                Location = new Point(Left, y),
                Parent = content
            };
            overlayEvents.TextChanged += (_, __) => {
                if (int.TryParse(overlayEvents.Text.Trim(), out var count)) {
                    _settings.OverlayMaxEvents = count;
                }
            };
            y = overlayEvents.Bottom + Gap;

            var incompleteOnly = new Checkbox {
                Text = "Show incomplete daily rewards only",
                Checked = _settings.ShowIncompleteOnly,
                Width = ContentWidth,
                Location = new Point(Left, y),
                Parent = content
            };
            incompleteOnly.CheckedChanged += (_, e) => {
                _settings.ShowIncompleteOnly = e.Checked;
                EventTrackerModule.Instance.ApplyShowIncompleteOnly(e.Checked);
            };
            y = incompleteOnly.Bottom + Gap;

            var startedAlerts = new Checkbox {
                Text = "Alert when a watched event becomes active",
                Checked = _settings.EventStartedAlerts,
                Width = ContentWidth,
                Location = new Point(Left, y),
                Parent = content
            };
            startedAlerts.CheckedChanged += (_, e) => _settings.EventStartedAlerts = e.Checked;
            y = startedAlerts.Bottom + Gap;

            var suppress = new Checkbox {
                Text = "Suppress alerts for completed daily rewards",
                Checked = _settings.SuppressCompletedAlerts,
                Width = ContentWidth,
                Location = new Point(Left, y),
                Parent = content
            };
            suppress.CheckedChanged += (_, e) => _settings.SuppressCompletedAlerts = e.Checked;
            y = suppress.Bottom + Gap;

            _ = new Label {
                Text = "Daily reward tracking needs a GW2 API key (account + progression) in Blish settings, plus both permissions enabled for this module under Settings → Modules.",
                Width = ContentWidth,
                Height = 48,
                WrapText = true,
                Location = new Point(Left, y),
                Parent = content
            };
            y += 56;

            var remoteSchedule = new Checkbox {
                Text = "Fetch latest schedule from giovazz89 on reload",
                Checked = _settings.UseRemoteSchedule,
                Width = ContentWidth,
                Location = new Point(Left, y),
                Parent = content
            };
            remoteSchedule.CheckedChanged += (_, e) => _settings.UseRemoteSchedule = e.Checked;
            y = remoteSchedule.Bottom + 8;

            _ = new Label {
                Text = $"Current session schedule: {(_usedRemoteSchedule ? "remote GitHub copy" : "embedded fallback")}. Reload the module to apply remote toggle changes.",
                Width = ContentWidth,
                Height = 36,
                WrapText = true,
                Location = new Point(Left, y),
                Parent = content
            };
            y += 44;

            _ = new Label {
                Text = "Auto-watch categories (applies to all events in category):",
                Width = ContentWidth,
                Height = 24,
                Location = new Point(Left, y),
                Parent = content
            };
            y += 28;

            foreach (var category in EventCategoryMapper.MenuCategories) {
                var categoryCheckbox = new Checkbox {
                    Text = category,
                    Checked = _settings.GetCategoryWatchDefault(category),
                    Width = ContentWidth,
                    Location = new Point(Left, y),
                    Parent = content
                };
                categoryCheckbox.CheckedChanged += (_, e) => {
                    _settings.SetCategoryWatchDefault(category, e.Checked);
                    EventTrackerModule.Instance.ApplyCategoryWatchDefault(category, e.Checked);
                };
                y = categoryCheckbox.Bottom + 8;
            }

            y += 8;

            var leadTimesLabel = new Label {
                Text = "Alert lead times (minutes, comma-separated):",
                Width = ContentWidth,
                Location = new Point(Left, y),
                Parent = content
            };
            y = leadTimesLabel.Bottom + 8;

            var leadTimes = new TextBox {
                Text = _settings.AlertLeadTimesRaw,
                Width = 200,
                Location = new Point(Left, y),
                Parent = content
            };
            leadTimes.TextChanged += (_, __) => _settings.SetAlertLeadTimes(leadTimes.Text);
            y = leadTimes.Bottom + Gap;

            _ = new Label {
                Text = "Default: 15,10,5 — alerts fire when an event is within those minute windows. Right-click a notification or event card to snooze until UTC midnight reset.",
                Width = ContentWidth,
                Height = 52,
                WrapText = true,
                Location = new Point(Left, y),
                Parent = content
            };
            y += 60;

            y = CreateProfileSection(content, y);

            content.Height = y + 24;
        }

        private int CreateProfileSection(Panel content, int y) {
            _ = new Label {
                Text = "Event profiles",
                Width = ContentWidth,
                Location = new Point(Left, y),
                Parent = content
            };
            y += 24;

            var profileDropdown = new Dropdown {
                Width = ContentWidth,
                Location = new Point(Left, y),
                Parent = content
            };
            PopulateProfileDropdown(profileDropdown);
            y = profileDropdown.Bottom + 8;

            var applyProfile = new StandardButton {
                Text = "Apply Profile",
                Width = ContentWidth,
                Location = new Point(Left, y),
                Parent = content
            };
            applyProfile.Click += (_, __) => {
                if (profileDropdown.SelectedItem is string profileName) {
                    EventTrackerModule.Instance.ApplyEventProfile(profileName);
                }
            };
            y = applyProfile.Bottom + Gap;

            _ = new Label {
                Text = "Save current tracker settings as a custom profile:",
                Width = ContentWidth,
                WrapText = true,
                Height = 24,
                Location = new Point(Left, y),
                Parent = content
            };
            y += 28;

            var profileNameBox = new TextBox {
                PlaceholderText = "Profile name",
                Width = ContentWidth,
                Location = new Point(Left, y),
                Parent = content
            };
            y = profileNameBox.Bottom + 8;

            var profileDescriptionBox = new TextBox {
                PlaceholderText = "Description (optional)",
                Width = ContentWidth,
                Location = new Point(Left, y),
                Parent = content
            };
            y = profileDescriptionBox.Bottom + 8;

            var saveProfile = new StandardButton {
                Text = "Save Current Settings as Profile",
                Width = ContentWidth,
                Location = new Point(Left, y),
                Parent = content
            };
            saveProfile.Click += (_, __) => {
                if (EventTrackerModule.Instance.SaveCustomProfile(
                        profileNameBox.Text?.Trim() ?? string.Empty,
                        profileDescriptionBox.Text?.Trim() ?? string.Empty)) {
                    PopulateProfileDropdown(profileDropdown);
                    profileDropdown.SelectedItem = profileNameBox.Text.Trim();
                }
            };
            y = saveProfile.Bottom + 8;

            var deleteProfile = new StandardButton {
                Text = "Delete Selected Custom Profile",
                Width = ContentWidth,
                Location = new Point(Left, y),
                Parent = content
            };
            deleteProfile.Click += (_, __) => {
                if (!(profileDropdown.SelectedItem is string profileName)) {
                    return;
                }

                if (EventTrackerModule.Instance.DeleteCustomProfile(profileName)) {
                    PopulateProfileDropdown(profileDropdown);
                }
            };
            y = deleteProfile.Bottom + 8;

            _ = new Label {
                Text = "Custom profiles capture filter, sort, alert, overlay, and watch settings. Built-in profiles (including Track Everything) cannot be deleted.",
                Width = ContentWidth,
                Height = 48,
                WrapText = true,
                Location = new Point(Left, y),
                Parent = content
            };
            y += 56;

            return y;
        }

        private void PopulateProfileDropdown(Dropdown profileDropdown) {
            var selected = profileDropdown.SelectedItem as string;
            profileDropdown.Items.Clear();
            foreach (var profile in _profileStore.GetAllProfiles()) {
                profileDropdown.Items.Add(profile.Name);
            }

            if (!string.IsNullOrWhiteSpace(selected) && profileDropdown.Items.Contains(selected)) {
                profileDropdown.SelectedItem = selected;
            } else if (!string.IsNullOrWhiteSpace(_settings.ActiveProfileName) &&
                       profileDropdown.Items.Contains(_settings.ActiveProfileName)) {
                profileDropdown.SelectedItem = _settings.ActiveProfileName;
            }
        }
    }

}
