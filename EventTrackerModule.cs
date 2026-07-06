using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Gw2EventTracker.Models;
using Gw2EventTracker.Services;
using Gw2EventTracker.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace Gw2EventTracker {

    [Export(typeof(Module))]
    public class EventTrackerModule : Module {

        private static readonly Logger Logger = Logger.GetLogger<EventTrackerModule>();

        internal static EventTrackerModule Instance { get; private set; } = null!;

        private ModuleSettings _moduleSettings = null!;
        private EventProfileStore _profileStore = null!;
        private EventScheduleEngine _scheduleEngine = null!;
        private TrackableRewardsCatalog _rewardsCatalog = null!;
        private AccountProgressService _progressService = null!;
        private AlertService _alertService = null!;
        private AchievementInfoCache _achievementCache = null!;
        private SettingCollection _watchSettings = null!;

        private EventTrackerView? _trackerView;
        private DailyProgressView? _dailyProgressView;
        private NextUpOverlayWidget? _overlayWidget;
        private WindowTab? _tab;
        private WindowTab? _progressTab;
        private double _scheduleElapsed;
        private double _progressElapsed;
        private double _accessRetryElapsed;
        private int? _lastMapId;
        private DateTime _lastMapProgressRefreshUtc = DateTime.MinValue;
        private bool _usedRemoteSchedule;

        private const double ProgressRefreshSeconds = 60;
        private const double UrgentProgressRefreshSeconds = 20;
        private const double MapProgressRefreshSeconds = 30;
        private const double AccessRetrySeconds = 15;

        private Texture2D _textureWatch = null!;
        private Texture2D _textureWatchActive = null!;

        internal ContentsManager ContentsManager => ModuleParameters.ContentsManager;
        internal Gw2ApiManager Gw2ApiManager => ModuleParameters.Gw2ApiManager;
        internal AchievementInfoCache AchievementCache => _achievementCache;
        internal bool UsedRemoteSchedule => _usedRemoteSchedule;
        internal float NotificationDuration => _moduleSettings.NotificationDurationSeconds;

        public bool NotificationsEnabled {
            get => _moduleSettings.NotificationsEnabled;
            set => _moduleSettings.NotificationsEnabled = value;
        }

        public bool ChimeEnabled {
            get => _moduleSettings.ChimeEnabled;
            set => _moduleSettings.ChimeEnabled = value;
        }

        public Point NotificationPosition {
            get => _moduleSettings.NotificationPosition;
            set => _moduleSettings.NotificationPosition = value;
        }

        [ImportingConstructor]
        public EventTrackerModule([Import("ModuleParameters")] ModuleParameters moduleParameters)
            : base(moduleParameters) {
            Instance = this;
        }

        protected override void DefineSettings(SettingCollection settings) {
            _moduleSettings = new ModuleSettings(settings);
            _profileStore = new EventProfileStore(_moduleSettings);
            _watchSettings = settings.AddSubCollection("Watching");
        }

        protected override void Initialize() {
            Gw2ApiManager.SubtokenUpdated += OnSubtokenUpdated;
        }

        protected override async Task LoadAsync() {
            var rewardsFile = JsonConvert.DeserializeObject<TrackableRewardsFile>(
                ModuleData.ReadEmbedded("trackable-rewards.json")) ?? new TrackableRewardsFile();

            var loadResult = await EventScheduleLoader
                .LoadSectionsAsync(_moduleSettings.UseRemoteSchedule)
                .ConfigureAwait(false);
            _usedRemoteSchedule = loadResult.UsedRemote;
            Logger.Info("Schedule source: {Source}.", loadResult.UsedRemote ? "remote" : "embedded");

            _textureWatch = ContentsManager.GetTexture("textures/605021.png");
            _textureWatchActive = ContentsManager.GetTexture("textures/605019.png");

            _rewardsCatalog = new TrackableRewardsCatalog(rewardsFile);
            _scheduleEngine = new EventScheduleEngine(loadResult.Sections);
            _scheduleEngine.DailyReset += OnDailyReset;
            _progressService = new AccountProgressService(Gw2ApiManager, _rewardsCatalog);
            _achievementCache = new AchievementInfoCache(Gw2ApiManager);
            _alertService = new AlertService(_scheduleEngine, _progressService, _moduleSettings);

            _ = _achievementCache.PrefetchAsync(EventRewardsFormatter.CollectAchievementIds(loadResult.Sections));

            _progressService.ProgressUpdated += (_, __) => RefreshProgressUi();

            await _progressService.RefreshAsync(force: true).ConfigureAwait(false);
        }

        protected override void OnModuleLoaded(EventArgs e) {
            var contentRegion = GameService.Overlay.BlishHudWindow.ContentRegion;

            _trackerView = new EventTrackerView(
                contentRegion,
                _scheduleEngine,
                _progressService,
                _moduleSettings,
                _profileStore,
                _watchSettings,
                _achievementCache,
                _textureWatch,
                _textureWatchActive);

            _dailyProgressView = new DailyProgressView(contentRegion, _progressService, _scheduleEngine);

            _overlayWidget = new NextUpOverlayWidget(_scheduleEngine, _moduleSettings) {
                Parent = GameService.Graphics.SpriteScreen
            };

            _tab = GameService.Overlay.BlishHudWindow.AddTab(
                "Event Tracker",
                ContentsManager.GetTexture("textures/1466345.png"),
                _trackerView);

            _progressTab = GameService.Overlay.BlishHudWindow.AddTab(
                "Daily Progress",
                ContentsManager.GetTexture("textures/605021.png"),
                _dailyProgressView);

            _progressService.ApplyCompletionStates(_scheduleEngine.Events);
            _trackerView.RefreshList();
            _dailyProgressView.RefreshList();
            _overlayWidget.Refresh();

            var activeProfile = _moduleSettings.ActiveProfileName;
            if (string.IsNullOrWhiteSpace(activeProfile) || _profileStore.Find(activeProfile) == null) {
                ApplyEventProfile(EventProfileStore.DefaultProfileName);
            } else {
                ApplyEventProfile(activeProfile);
            }

            base.OnModuleLoaded(e);
            Logger.Info("GW2 Event Tracker loaded with {Count} events.", _scheduleEngine.Events.Count);

            _ = _progressService.RefreshAsync(force: true);
        }

        public override IView GetSettingsView() {
            return new SettingsView(_moduleSettings, _profileStore, _usedRemoteSchedule);
        }

        internal void SnoozeEvent(string eventKey) {
            _moduleSettings.SnoozeUntilReset(eventKey);
            ScreenNotification.ShowNotification("Snoozed until daily reset.", duration: 2);
        }

        internal void ApplyShowIncompleteOnly(bool enabled) {
            _trackerView?.SetIncompleteFilter(enabled);
        }

        internal void ApplyEventProfile(string profileName) {
            var profile = _profileStore.Find(profileName);
            if (profile == null) {
                return;
            }

            EventProfileStore.ApplyToSettings(profile, _moduleSettings, _watchSettings, _scheduleEngine.Events);
            _trackerView?.ApplyProfile(profile);
            _overlayWidget?.Refresh();
            ScreenNotification.ShowNotification($"Applied profile: {profile.Name}", duration: 2);
        }

        internal bool SaveCustomProfile(string name, string description) {
            if (_trackerView == null || string.IsNullOrWhiteSpace(name)) {
                return false;
            }

            if (_profileStore.IsBuiltIn(name)) {
                ScreenNotification.ShowNotification(
                    "That name is reserved for a built-in profile.",
                    ScreenNotification.NotificationType.Red,
                    duration: 3);
                return false;
            }

            var profile = _profileStore.CaptureFromCurrent(
                name,
                description,
                _moduleSettings,
                _trackerView.GetFilterMode(),
                _trackerView.GetSortMode(),
                _scheduleEngine.Events);

            if (!_profileStore.SaveCustom(profile)) {
                return false;
            }

            _trackerView.RefreshProfileDropdown();
            ScreenNotification.ShowNotification($"Saved profile: {profile.Name}", duration: 2);
            return true;
        }

        internal bool DeleteCustomProfile(string name) {
            if (string.IsNullOrWhiteSpace(name) || !_profileStore.DeleteCustom(name)) {
                ScreenNotification.ShowNotification(
                    "Cannot delete built-in or unknown profiles.",
                    ScreenNotification.NotificationType.Red,
                    duration: 3);
                return false;
            }

            _trackerView?.RefreshProfileDropdown();
            ScreenNotification.ShowNotification($"Deleted profile: {name}", duration: 2);
            return true;
        }

        internal IReadOnlyList<EventProfile> GetAllProfiles() => _profileStore.GetAllProfiles();

        internal bool IsBuiltInProfile(string name) => _profileStore.IsBuiltIn(name);

        internal void ApplyCategoryWatchDefault(string category, bool watched) {
            _trackerView?.ApplyCategoryWatchDefault(category, watched);
        }

        internal void RefreshTrackerList() {
            _trackerView?.RefreshList();
        }

        internal async Task ForceRefreshProgressAsync() {
            await _progressService.RefreshAsync(force: true).ConfigureAwait(false);
            RefreshProgressUi();
        }

        internal void ShowSetNotificationPositions() {
            var tempSizeSetting = new SettingEntry<Point> {
                Value = new Point(EventNotification.NotificationWidth, 512)
            };

            var mover = new NotificationMover(new ScreenRegion(
                "Notifications",
                _moduleSettings.NotificationPositionSetting,
                tempSizeSetting));
            mover.Parent = GameService.Graphics.SpriteScreen;
            mover.Size = GameService.Graphics.SpriteScreen.ContentRegion.Size;
        }

        internal void ShowSetOverlayPosition() {
            var widgetHeight = _overlayWidget?.Height ?? NextUpOverlayWidget.CalculateHeight(3);
            var tempSizeSetting = new SettingEntry<Point> {
                Value = new Point(NextUpOverlayWidget.WidgetWidth, widgetHeight)
            };

            var mover = new OverlayPositionMover(new ScreenRegion(
                "Next Up Overlay",
                _moduleSettings.OverlayPositionSetting,
                tempSizeSetting));
            mover.Parent = GameService.Graphics.SpriteScreen;
            mover.Size = GameService.Graphics.SpriteScreen.ContentRegion.Size;
        }

        protected override void Update(GameTime gameTime) {
            var elapsed = gameTime.ElapsedGameTime.TotalSeconds;
            var utcNow = DateTime.UtcNow;

            _scheduleElapsed += elapsed;
            if (_scheduleElapsed >= 5) {
                if (_progressService.EnsureUtcDay(utcNow)) {
                    _moduleSettings.ClearSnoozes();
                    _ = _progressService.RefreshAsync(force: true);
                }

                _scheduleEngine.Refresh(utcNow);
                _alertService.OnScheduleRefreshed();
                _trackerView?.RefreshList();
                _dailyProgressView?.RefreshList();
                _overlayWidget?.Refresh();
                _scheduleElapsed = 0;
            }

            _progressElapsed += elapsed;
            var progressInterval = _progressService.NeedsUrgentRefresh
                ? UrgentProgressRefreshSeconds
                : ProgressRefreshSeconds;

            if (_progressElapsed >= progressInterval) {
                _ = _progressService.RefreshAsync(force: true);
                _progressElapsed = 0;
            }

            if (!_progressService.HasApiAccess) {
                _accessRetryElapsed += elapsed;
                if (_accessRetryElapsed >= AccessRetrySeconds) {
                    _ = _progressService.RefreshAsync(force: true);
                    _accessRetryElapsed = 0;
                }
            } else {
                _accessRetryElapsed = 0;
            }

            TryRefreshProgressOnMapChange();

            _alertService.Update(elapsed);
        }

        private void TryRefreshProgressOnMapChange() {
            if (!GameService.Gw2Mumble.IsAvailable) {
                return;
            }

            var mapId = GameService.Gw2Mumble.CurrentMap.Id;
            if (mapId <= 0) {
                return;
            }

            if (_lastMapId == mapId) {
                return;
            }

            var isFirstMap = !_lastMapId.HasValue;
            _lastMapId = mapId;

            var utcNow = DateTime.UtcNow;
            if (!isFirstMap &&
                (utcNow - _lastMapProgressRefreshUtc).TotalSeconds < MapProgressRefreshSeconds) {
                return;
            }

            _lastMapProgressRefreshUtc = utcNow;
            _ = _progressService.RefreshAsync(force: true);
        }

        protected override void Unload() {
            Gw2ApiManager.SubtokenUpdated -= OnSubtokenUpdated;

            if (_tab != null) {
                GameService.Overlay.BlishHudWindow.RemoveTab(_tab);
            }

            if (_progressTab != null) {
                GameService.Overlay.BlishHudWindow.RemoveTab(_progressTab);
            }

            _overlayWidget?.Dispose();

            Instance = null!;
        }

        private void OnSubtokenUpdated(object? sender, EventArgs e) {
            Logger.Info("GW2 API subtoken updated — refreshing daily progress.");
            _progressService.ResetAccessLogging();
            _ = _progressService.RefreshAsync(force: true);
        }

        private void OnDailyReset(object? sender, EventArgs e) {
            Logger.Info("UTC daily reset — clearing snoozes and refreshing progress.");
            _moduleSettings.NormalizeSnoozesForUtcDay();
            _moduleSettings.ClearSnoozes();
            _progressService.HandleDailyReset();
            RefreshProgressUi();
        }

        private void RefreshProgressUi() {
            GameService.Overlay.QueueMainThreadUpdate(_ => {
                _progressService.ApplyCompletionStates(_scheduleEngine.Events);
                _trackerView?.RefreshList();
                _dailyProgressView?.RefreshList();
                _overlayWidget?.Refresh();
            });
        }
    }

}
