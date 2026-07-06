using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Settings;
using Gw2EventTracker.Models;
using Gw2EventTracker.Services;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gw2EventTracker.UI {

    public sealed class EventTrackerView : Panel {

        private const string SortNextUp = "Next Up";
        private const string SortAlphabetical = "Alphabetical";
        private const string OnCurrentMapFilter = "On Current Map";

        private readonly EventScheduleEngine _scheduleEngine;
        private readonly AccountProgressService _progressService;
        private readonly ModuleSettings _settings;
        private readonly EventProfileStore _profileStore;
        private readonly SettingCollection _watchSettings;
        private readonly AchievementInfoCache _achievementCache;
        private readonly Texture2D _textureWatch;
        private readonly Texture2D _textureWatchActive;

        private readonly Dropdown _sortDropdown;
        private readonly Dropdown _profileDropdown;
        private readonly FlowPanel _eventPanel;
        private readonly TextBox _searchBox;
        private readonly Label _summaryLabel;

        private readonly List<EventCard> _cards = new List<EventCard>();
        private string _filterMode = "All Events";

        public EventTrackerView(
            Rectangle bounds,
            EventScheduleEngine scheduleEngine,
            AccountProgressService progressService,
            ModuleSettings settings,
            EventProfileStore profileStore,
            SettingCollection watchSettings,
            AchievementInfoCache achievementCache,
            Texture2D textureWatch,
            Texture2D textureWatchActive) {
            _scheduleEngine = scheduleEngine;
            _progressService = progressService;
            _settings = settings;
            _profileStore = profileStore;
            _watchSettings = watchSettings;
            _achievementCache = achievementCache;
            _textureWatch = textureWatch;
            _textureWatchActive = textureWatchActive;

            CanScroll = false;
            Size = bounds.Size;

            _sortDropdown = new Dropdown {
                Location = new Point(Right - 150 - Dropdown.Standard.ControlOffset.X, Dropdown.Standard.ControlOffset.Y),
                Width = 150,
                Parent = this
            };

            _profileDropdown = new Dropdown {
                Width = 150,
                Parent = this,
                Top = Dropdown.Standard.ControlOffset.Y,
                Left = Panel.MenuStandard.PanelOffset.X
            };
            RefreshProfileDropdown();
            _profileDropdown.ValueChanged += (_, __) => {
                if (_profileDropdown.SelectedItem is string profileName && !string.IsNullOrWhiteSpace(profileName)) {
                    EventTrackerModule.Instance.ApplyEventProfile(profileName);
                }
            };

            var notificationToggle = new Checkbox {
                Text = "Enable Notifications",
                Checked = _settings.NotificationsEnabled,
                Parent = this
            };
            notificationToggle.Location = new Point(_sortDropdown.Left - notificationToggle.Width - 10, _sortDropdown.Top + 6);
            notificationToggle.CheckedChanged += (_, e) => _settings.NotificationsEnabled = e.Checked;

            var chimeToggle = new Checkbox {
                Text = "Mute Notifications",
                Checked = !_settings.ChimeEnabled,
                Parent = this,
                Top = notificationToggle.Top,
                Right = notificationToggle.Left - 10
            };
            chimeToggle.CheckedChanged += (_, e) => _settings.ChimeEnabled = !e.Checked;

            const int sectionGap = 8;
            const int summaryHeight = 54;
            var controlsBottom = Math.Max(_sortDropdown.Bottom, _profileDropdown.Bottom);
            var summaryTop = controlsBottom + sectionGap;

            _summaryLabel = new Label {
                WrapText = true,
                Width = _sortDropdown.Right - Panel.MenuStandard.PanelOffset.X,
                Height = summaryHeight,
                Parent = this,
                Location = new Point(Panel.MenuStandard.PanelOffset.X, summaryTop)
            };

            _searchBox = new TextBox {
                PlaceholderText = "Event Search",
                Width = Panel.MenuStandard.Size.X,
                Location = new Point(Panel.MenuStandard.PanelOffset.X, _summaryLabel.Bottom + sectionGap),
                Parent = this
            };
            _searchBox.TextChanged += (_, __) => ApplyFilters();

            var topOffset = _searchBox.Bottom + sectionGap;

            var menuSection = new Panel {
                Title = "Event Categories",
                ShowBorder = true,
                Size = Panel.MenuStandard.Size - new Point(0, topOffset + Panel.MenuStandard.ControlOffset.Y),
                Location = new Point(Panel.MenuStandard.PanelOffset.X, topOffset),
                Parent = this
            };

            _eventPanel = new FlowPanel {
                FlowDirection = ControlFlowDirection.LeftToRight,
                ControlPadding = new Vector2(8, 8),
                Location = new Point(menuSection.Right + Panel.MenuStandard.ControlOffset.X, menuSection.Top),
                Size = new Point(_sortDropdown.Right - menuSection.Right - Control.ControlStandard.ControlOffset.X, menuSection.Height),
                CanScroll = true,
                Parent = this
            };

            BuildCategoryMenu(menuSection);
            BuildEventCards();
            BuildSortDropdown();

            if (_settings.ShowIncompleteOnly) {
                _filterMode = "Incomplete Rewards";
            }

            RefreshList();
        }

        private void BuildSortDropdown() {
            _sortDropdown.Items.Add(SortAlphabetical);
            _sortDropdown.Items.Add(SortNextUp);
            _sortDropdown.SelectedItem = SortNextUp;
            _sortDropdown.ValueChanged += (_, __) => SortEventPanel();
        }

        private void BuildCategoryMenu(Panel menuSection) {
            var viewportSize = menuSection.ContentRegion.Size;
            const int menuItemHeight = 36;

            var scrollHost = new Panel {
                Size = viewportSize,
                CanScroll = true,
                Parent = menuSection
            };

            var menu = new Menu {
                Width = Math.Max(100, viewportSize.X - 8),
                MenuItemHeight = menuItemHeight,
                Parent = scrollHost,
                CanSelect = true
            };

            var itemCount = 0;

            var all = menu.AddMenuItem("All Events");
            itemCount++;
            all.Select();
            all.Click += (_, __) => {
                _filterMode = "All Events";
                ApplyFilters();
            };

            var watched = menu.AddMenuItem("Watched Events");
            itemCount++;
            watched.Click += (_, __) => {
                _filterMode = "Watched Events";
                ApplyFilters();
            };

            var incomplete = menu.AddMenuItem("Incomplete Rewards");
            itemCount++;
            incomplete.Click += (_, __) => {
                _filterMode = "Incomplete Rewards";
                ApplyFilters();
            };

            var activeNow = menu.AddMenuItem("Active Now");
            itemCount++;
            activeNow.Click += (_, __) => {
                _filterMode = "Active Now";
                ApplyFilters();
            };

            var onCurrentMap = menu.AddMenuItem(OnCurrentMapFilter);
            itemCount++;
            onCurrentMap.Click += (_, __) => {
                _filterMode = OnCurrentMapFilter;
                ApplyFilters();
            };

            var presentCategories = new HashSet<string>(
                _scheduleEngine.Events.Select(e => e.Category),
                StringComparer.OrdinalIgnoreCase);

            foreach (var category in EventCategoryMapper.MenuCategories) {
                if (!presentCategories.Contains(category)) {
                    continue;
                }

                var item = menu.AddMenuItem(category);
                itemCount++;
                item.Click += (_, __) => {
                    _filterMode = category;
                    ApplyFilters();
                };
            }

            menu.Height = itemCount * menuItemHeight;
        }

        private void BuildEventCards() {
            _eventPanel.ClearChildren();
            _cards.Clear();

            foreach (var tracked in _scheduleEngine.Events) {
                var categoryDefault = _settings.GetCategoryWatchDefault(tracked.Category);
                var watchSetting = _watchSettings.DefineSetting($"watch:{tracked.Key}", categoryDefault);
                tracked.IsWatched = watchSetting.Value;

                var card = new DetailsButton {
                    Parent = _eventPanel,
                    Text = tracked.DisplayLabel,
                    IconDetails = string.Empty,
                    IconSize = DetailsIconSize.Small,
                    ShowVignette = false,
                    HighlightType = DetailsHighlightType.LightHighlight,
                    ShowToggleButton = true
                };

                card.Icon = ResolveCardIcon(tracked);

                Panel? accentPanel = null;
                if (tracked.AccentColor.HasValue) {
                    accentPanel = new Panel {
                        Width = 4,
                        BackgroundColor = tracked.AccentColor.Value,
                        Parent = card
                    };
                }

                var timeLabel = new Label {
                    Size = new Point(56, card.ContentRegion.Height),
                    Text = "--",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Middle,
                    Parent = card
                };

                GlowButton? wikiButton = null;
                if (!string.IsNullOrWhiteSpace(tracked.WikiUrl)) {
                    wikiButton = new GlowButton {
                        Icon = GameService.Content.GetTexture("102530"),
                        ActiveIcon = GameService.Content.GetTexture("glow-wiki"),
                        BasicTooltipText = "Read about this event on the wiki.",
                        Parent = card,
                        GlowColor = Color.White * 0.1f
                    };
                    wikiButton.Click += (_, __) => OpenWiki(tracked.WikiUrl);
                }

                GlowButton? waypointButton = null;
                if (!string.IsNullOrWhiteSpace(tracked.ChatLink)) {
                    waypointButton = new GlowButton {
                        Icon = GameService.Content.GetTexture("waypoint"),
                        ActiveIcon = GameService.Content.GetTexture("glow-waypoint"),
                        BasicTooltipText = $"Nearby waypoint: {tracked.ChatLink}",
                        Parent = card,
                        GlowColor = Color.White * 0.1f
                    };
                    waypointButton.Click += (_, __) => CopyWaypoint(tracked.ChatLink);
                }

                var completionLabel = new Label {
                    Size = new Point(24, card.ContentRegion.Height),
                    Text = string.Empty,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Middle,
                    Visible = false,
                    Parent = card
                };

                var watchButton = new GlowButton {
                    Icon = _textureWatch,
                    ActiveIcon = _textureWatchActive,
                    BasicTooltipText = "Click to toggle tracking for this event.",
                    ToggleGlow = true,
                    Checked = tracked.IsWatched,
                    Parent = card
                };
                watchButton.Click += (_, __) => {
                    tracked.IsWatched = watchButton.Checked;
                    watchSetting.Value = watchButton.Checked;
                };

                card.RightMouseButtonReleased += (_, __) => {
                    EventTrackerModule.Instance.SnoozeEvent(tracked.Key);
                };
                card.BasicTooltipText = BuildCardTooltip(tracked);

                var mapStripe = new Panel {
                    Height = 2,
                    BackgroundColor = Color.Transparent,
                    Visible = false,
                    Parent = card
                };

                var eventCard = new EventCard(
                    tracked.Key,
                    card,
                    timeLabel,
                    completionLabel,
                    watchButton,
                    wikiButton,
                    waypointButton,
                    accentPanel,
                    mapStripe);
                UpdateCompletionLabel(completionLabel, tracked);
                card.Resized += (_, __) => LayoutCardActions(eventCard);
                watchButton.Resized += (_, __) => LayoutCardActions(eventCard);
                if (wikiButton != null) {
                    wikiButton.Resized += (_, __) => LayoutCardActions(eventCard);
                }

                if (waypointButton != null) {
                    waypointButton.Resized += (_, __) => LayoutCardActions(eventCard);
                }

                LayoutCardActions(eventCard);
                _cards.Add(eventCard);
            }

            SortEventPanel();
        }

        public void SetIncompleteFilter(bool enabled) {
            _filterMode = enabled ? "Incomplete Rewards" : "All Events";
            ApplyFilters();
        }

        public void ApplyCategoryWatchDefault(string category, bool watched) {
            foreach (var card in _cards) {
                var tracked = _scheduleEngine.Events.First(e => e.Key == card.Key);
                if (!tracked.Category.Equals(category, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                tracked.IsWatched = watched;
                _watchSettings.DefineSetting($"watch:{tracked.Key}", watched).Value = watched;
                card.WatchButton.Checked = watched;
            }

            ApplyFilters();
        }

        public void ApplyProfile(EventProfile profile) {
            _filterMode = profile.FilterMode;
            _sortDropdown.SelectedItem = profile.SortMode;
            _profileDropdown.SelectedItem = profile.Name;
            RefreshList();
        }

        public void RefreshProfileDropdown() {
            var selected = _profileDropdown.SelectedItem as string;
            _profileDropdown.Items.Clear();
            foreach (var profile in _profileStore.GetAllProfiles()) {
                _profileDropdown.Items.Add(profile.Name);
            }

            if (!string.IsNullOrWhiteSpace(selected) && _profileDropdown.Items.Contains(selected)) {
                _profileDropdown.SelectedItem = selected;
            } else if (!string.IsNullOrWhiteSpace(_settings.ActiveProfileName) &&
                       _profileDropdown.Items.Contains(_settings.ActiveProfileName)) {
                _profileDropdown.SelectedItem = _settings.ActiveProfileName;
            }
        }

        public string GetFilterMode() => _filterMode;

        public string GetSortMode() => (_sortDropdown.SelectedItem as string) ?? SortNextUp;

        public void RefreshList() {
            _progressService.ApplyCompletionStates(_scheduleEngine.Events);
            UpdateSummary();

            foreach (var card in _cards) {
                var tracked = _scheduleEngine.Events.First(e => e.Key == card.Key);
                card.Button.Text = tracked.DisplayLabel;
                card.Button.BasicTooltipText = BuildCardTooltip(tracked);
                card.TimeLabel.Text = FormatStatusTime(tracked);
                card.TimeLabel.TextColor = tracked.IsActive ? new Color(255, 220, 100) : Color.White;
                card.TimeLabel.BasicTooltipText = GetTimeDetails(tracked);
                UpdateCompletionLabel(card.CompletionLabel, tracked);
                LayoutCardActions(card);
                card.WatchButton.Checked = tracked.IsWatched;
                UpdateMapHighlight(card, tracked);
            }

            GameService.Overlay.QueueMainThreadUpdate(_ => {
                foreach (var card in _cards) {
                    LayoutCardActions(card);
                }
            });

            ApplyFilters();
        }

        private void ApplyFilters() {
            var search = _searchBox.Text?.Trim() ?? string.Empty;

            foreach (var card in _cards) {
                var tracked = _scheduleEngine.Events.First(e => e.Key == card.Key);
                var visible = MatchesFilter(tracked) &&
                              (string.IsNullOrWhiteSpace(search) ||
                               tracked.DisplayLabel.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
                card.Button.Visible = visible;
            }

            SortEventPanel();
        }

        private bool MatchesFilter(TrackedEvent tracked) {
            if (_filterMode == OnCurrentMapFilter) {
                return IsOnCurrentMap(tracked);
            }

            return _filterMode switch {
                "Watched Events" => tracked.IsWatched,
                "Incomplete Rewards" => tracked.CompletionState != CompletionState.Completed,
                "Active Now" => tracked.IsActive,
                "All Events" => true,
                _ => tracked.Category == _filterMode
            };
        }

        private void UpdateMapHighlight(EventCard card, TrackedEvent tracked) {
            var mapId = GetCurrentMapId();
            var highlight = _settings.MapHighlightEnabled &&
                            mapId.HasValue &&
                            EventMapMapper.IsOnMap(tracked, mapId.Value);

            card.MapStripe.Visible = highlight;
            card.MapStripe.Width = card.Button.Width;
            if (highlight) {
                card.MapStripe.BackgroundColor = new Color(255, 210, 80);
            }
        }

        private static int? GetCurrentMapId() {
            if (!GameService.Gw2Mumble.IsAvailable) {
                return null;
            }

            var mapId = GameService.Gw2Mumble.CurrentMap.Id;
            return mapId > 0 ? mapId : (int?)null;
        }

        private bool IsOnCurrentMap(TrackedEvent tracked) {
            var mapId = GetCurrentMapId();
            return mapId.HasValue && EventMapMapper.IsOnMap(tracked, mapId.Value);
        }

        private void SortEventPanel() {
            if (_sortDropdown.SelectedItem == SortAlphabetical) {
                _eventPanel.SortChildren<DetailsButton>((a, b) => string.Compare(a.Text, b.Text, StringComparison.OrdinalIgnoreCase));
                _cards.Sort((a, b) => string.Compare(a.Button.Text, b.Button.Text, StringComparison.OrdinalIgnoreCase));
            } else if (_filterMode == "Day-Night Cycle") {
                _eventPanel.SortChildren<DetailsButton>((a, b) => {
                    var trackedA = _scheduleEngine.Events.First(e => e.Key == _cards.First(c => c.Button == a).Key);
                    var trackedB = _scheduleEngine.Events.First(e => e.Key == _cards.First(c => c.Button == b).Key);
                    return EventCategoryMapper.GetDayNightSortOrder(trackedA.SectionName, trackedA.SegmentName)
                        .CompareTo(EventCategoryMapper.GetDayNightSortOrder(trackedB.SectionName, trackedB.SegmentName));
                });
                _cards.Sort((a, b) => {
                    var trackedA = _scheduleEngine.Events.First(e => e.Key == a.Key);
                    var trackedB = _scheduleEngine.Events.First(e => e.Key == b.Key);
                    return EventCategoryMapper.GetDayNightSortOrder(trackedA.SectionName, trackedA.SegmentName)
                        .CompareTo(EventCategoryMapper.GetDayNightSortOrder(trackedB.SectionName, trackedB.SegmentName));
                });
            } else {
                var order = _scheduleEngine.Events
                    .OrderBy(e => e.NextStartUtc)
                    .Select(e => e.Key)
                    .ToList();

                _eventPanel.SortChildren<DetailsButton>((a, b) => {
                    var keyA = _cards.First(c => c.Button == a).Key;
                    var keyB = _cards.First(c => c.Button == b).Key;
                    return order.IndexOf(keyA).CompareTo(order.IndexOf(keyB));
                });
            }

            RepositionCards();
        }

        private void RepositionCards() {
            if (_filterMode == "Day-Night Cycle") {
                RepositionDayNightCards();
                return;
            }

            var pos = 0;
            foreach (var card in _cards.Where(c => c.Button.Visible)) {
                var x = pos % 2;
                var y = pos / 2;
                card.Button.Location = new Point(x * 308, y * 108);
                pos++;
            }

            _eventPanel.Invalidate();
        }

        private void RepositionDayNightCards() {
            var phases = new[] { "Dawn", "Day", "Dusk", "Night" };

            for (var row = 0; row < phases.Length; row++) {
                var phase = phases[row];
                var tyria = FindVisibleCard(phase, isCantha: false);
                var cantha = FindVisibleCard(phase, isCantha: true);

                if (tyria != null) {
                    tyria.Button.Location = new Point(0, row * 108);
                }

                if (cantha != null) {
                    cantha.Button.Location = new Point(308, row * 108);
                }
            }

            _eventPanel.Invalidate();
        }

        private EventCard? FindVisibleCard(string phase, bool isCantha) {
            foreach (var card in _cards) {
                if (!card.Button.Visible) {
                    continue;
                }

                var tracked = _scheduleEngine.Events.First(e => e.Key == card.Key);
                if (!tracked.SegmentName.Equals(phase, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                var cardIsCantha = tracked.SectionName.IndexOf("Cantha", StringComparison.OrdinalIgnoreCase) >= 0;
                if (cardIsCantha == isCantha) {
                    return card;
                }
            }

            return null;
        }

        private void UpdateSummary() {
            var reset = DailyResetHelper.FormatCountdown(DateTime.UtcNow);
            var recommendation = NextEventRecommender.Recommend(
                _scheduleEngine.Events,
                _settings,
                GetCurrentMapId());

            var recommendLine = recommendation == null
                ? "What should I do next? No strong pick right now."
                : $"What should I do next? {recommendation.Event.DisplayLabel} — {recommendation.Reason}";

            if (!_progressService.HasApiAccess) {
                _summaryLabel.Text = $"{recommendLine}{Environment.NewLine}Add a GW2 API key with account permissions to track daily rewards. Reset in {reset}.";
                return;
            }

            var summary = _progressService.GetSummary();
            _summaryLabel.Text =
                $"Today: {summary.Completed}/{summary.Trackable} daily rewards claimed  |  Reset in {reset}{Environment.NewLine}{recommendLine}";
        }

        private string BuildCardTooltip(TrackedEvent tracked) {
            var lines = new List<string> {
                tracked.Category,
                GetTimeDetails(tracked)
            };

            if (tracked.RecommendedLfg.HasValue && tracked.RecommendedLfg.Value > 0) {
                lines.Add($"Recommended group size: ~{tracked.RecommendedLfg.Value * 2} players");
            }

            var rewardsText = EventRewardsFormatter.FormatTooltip(tracked.Rewards, _achievementCache);
            if (!string.IsNullOrWhiteSpace(rewardsText)) {
                lines.Add(rewardsText);
            }

            lines.Add("Right click: snooze alerts until daily reset.");

            return string.Join(Environment.NewLine + Environment.NewLine, lines.Where(line => !string.IsNullOrWhiteSpace(line)));
        }

        private static AsyncTexture2D ResolveCardIcon(TrackedEvent tracked) {
            if (tracked.Category == "Day-Night Cycle" || string.IsNullOrWhiteSpace(tracked.IconUrl)) {
                return new AsyncTexture2D(GameService.Content.GetTexture("102377"));
            }

            return GameService.Content.GetRenderServiceTexture(tracked.IconUrl);
        }

        private static string FormatStatusTime(TrackedEvent tracked) => FormatTime(tracked);

        private static void UpdateCompletionLabel(Label completionLabel, TrackedEvent tracked) {
            switch (tracked.CompletionState) {
                case CompletionState.Completed:
                case CompletionState.Pending:
                    completionLabel.Text = GetStatusSymbol(tracked);
                    completionLabel.BasicTooltipText = GetStatusTooltip(tracked);
                    completionLabel.Visible = true;
                    break;
                default:
                    completionLabel.Text = string.Empty;
                    completionLabel.Visible = false;
                    break;
            }
        }

        private static string GetStatusSymbol(TrackedEvent tracked) =>
            tracked.CompletionState switch {
                CompletionState.Completed => "[x]",
                CompletionState.Pending => "[ ]",
                _ => string.Empty
            };

        private static void LayoutCardActions(EventCard card) {
            if (card.MapStripe.Visible) {
                card.MapStripe.Width = card.Button.Width;
                card.MapStripe.Top = 0;
                card.MapStripe.Left = 0;
            }

            if (card.AccentPanel != null) {
                card.AccentPanel.Height = card.Button.ContentRegion.Height;
                card.AccentPanel.Top = 0;
                card.AccentPanel.Left = 0;
            }

            var actionTop = Math.Max(0, (card.Button.ContentRegion.Height - card.WatchButton.Height) / 2);
            var rightEdge = card.Button.Width - 8;

            card.WatchButton.Top = actionTop;
            card.WatchButton.Left = rightEdge - card.WatchButton.Width;
            rightEdge = card.WatchButton.Left - 4;

            if (card.CompletionLabel.Visible) {
                card.CompletionLabel.Top = actionTop;
                card.CompletionLabel.Left = Math.Max(64, rightEdge - card.CompletionLabel.Width);
                rightEdge = card.CompletionLabel.Left - 4;
            }

            if (card.WaypointButton != null) {
                card.WaypointButton.Top = actionTop;
                card.WaypointButton.Left = Math.Max(64, rightEdge - card.WaypointButton.Width);
                rightEdge = card.WaypointButton.Left - 4;
            }

            if (card.WikiButton != null) {
                card.WikiButton.Top = actionTop;
                card.WikiButton.Left = Math.Max(64, rightEdge - card.WikiButton.Width);
            }
        }

        private static string GetStatusTooltip(TrackedEvent tracked) =>
            tracked.CompletionState switch {
                CompletionState.Completed => "Daily reward claimed",
                CompletionState.Pending => "Daily reward not yet claimed",
                _ => "Not tracked via API"
            };

        private static string FormatTime(TrackedEvent tracked) {
            if (tracked.IsActive) {
                if (tracked.Category == "Day-Night Cycle" && tracked.NextEndUtc != default) {
                    return tracked.NextEndUtc.ToLocalTime().ToShortTimeString();
                }

                return "ACTIVE";
            }

            if (tracked.NextStartUtc == default) {
                return "--";
            }

            return tracked.NextStartUtc.ToLocalTime().ToShortTimeString();
        }

        private static string GetTimeDetails(TrackedEvent tracked) {
            if (tracked.IsActive && tracked.Category == "Day-Night Cycle" && tracked.NextEndUtc != default) {
                var timeRemaining = tracked.NextEndUtc.ToLocalTime() - DateTime.Now;
                if (timeRemaining.TotalMinutes > 0) {
                    return $"Ends in {timeRemaining.Humanize(maxUnit: TimeUnit.Hour, minUnit: TimeUnit.Minute, precision: 2, collectionSeparator: null)}";
                }
            }

            if (tracked.NextStartUtc == default) {
                if (tracked.Category == "Meta Event") {
                    return "Not on a fixed timer — check the map for prerequisite metas.";
                }

                return "No upcoming occurrence";
            }

            var timeUntil = tracked.NextStartUtc.ToLocalTime() - DateTime.Now;
            if (timeUntil.TotalMinutes < 0) {
                return "Active or recently started";
            }

            return $"Starts in {timeUntil.Humanize(maxUnit: TimeUnit.Hour, minUnit: TimeUnit.Minute, precision: 2, collectionSeparator: null)}";
        }

        private static void OpenWiki(string wikiUrl) {
            if (!Uri.TryCreate(wikiUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) {
                return;
            }

            try {
                Process.Start(wikiUrl);
            } catch (Exception) {
                ScreenNotification.ShowNotification("Failed to open wiki page.", ScreenNotification.NotificationType.Red, duration: 2);
            }
        }

        private static async void CopyWaypoint(string chatLink) {
            try {
                await ClipboardUtil.WindowsClipboardService.SetTextAsync(chatLink).ConfigureAwait(false);
                ScreenNotification.ShowNotification("Copied waypoint to clipboard!", duration: 2);
            } catch (Exception) {
                ScreenNotification.ShowNotification("Failed to copy waypoint to clipboard. Try again.", ScreenNotification.NotificationType.Red, duration: 2);
            }
        }

        private sealed class EventCard {
            public EventCard(
                string key,
                DetailsButton button,
                Label timeLabel,
                Label completionLabel,
                GlowButton watchButton,
                GlowButton? wikiButton,
                GlowButton? waypointButton,
                Panel? accentPanel,
                Panel mapStripe) {
                Key = key;
                Button = button;
                TimeLabel = timeLabel;
                CompletionLabel = completionLabel;
                WatchButton = watchButton;
                WikiButton = wikiButton;
                WaypointButton = waypointButton;
                AccentPanel = accentPanel;
                MapStripe = mapStripe;
            }

            public string Key { get; }
            public DetailsButton Button { get; }
            public Label TimeLabel { get; }
            public Label CompletionLabel { get; }
            public GlowButton WatchButton { get; }
            public GlowButton? WikiButton { get; }
            public GlowButton? WaypointButton { get; }
            public Panel? AccentPanel { get; }
            public Panel MapStripe { get; }
        }
    }

}