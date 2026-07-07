using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Ghost.Gw2EventTracker.Models;
using Gw2Sharp.WebApi.V2.Models;

namespace Ghost.Gw2EventTracker.Services {

    public sealed class AccountProgressService {

        private static readonly Logger Logger = Logger.GetLogger<AccountProgressService>();

        private readonly Gw2ApiManager _apiManager;
        private readonly TrackableRewardsCatalog _catalog;

        private HashSet<string> _completedWorldBosses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _completedMapChests = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private DateTime _lastRefreshUtc = DateTime.MinValue;
        private DateTime _lastUtcMidnight = DateTime.MinValue;
        private bool _hasApiAccess;
        private bool _fetchFailed;
        private bool _loggedAccessMessage;
        private ProgressAccessState _accessState = ProgressAccessState.NoApiKey;
        private string _statusMessage = string.Empty;
        private string? _worldBossFetchError;
        private string? _mapChestFetchError;

        public event EventHandler? ProgressUpdated;

        /// <summary>True when the last API fetch failed; module should retry sooner.</summary>
        public bool NeedsUrgentRefresh => _fetchFailed;

        private const double MinRefreshSeconds = 45;

        public AccountProgressService(Gw2ApiManager apiManager, TrackableRewardsCatalog catalog) {
            _apiManager = apiManager;
            _catalog = catalog;
        }

        public bool HasApiAccess => _hasApiAccess;

        public ProgressAccessState AccessState => _accessState;

        public string StatusMessage => _statusMessage;

        public DateTime? LastSuccessfulRefreshUtc =>
            _lastRefreshUtc == DateTime.MinValue ? (DateTime?)null : _lastRefreshUtc;

        public IReadOnlyCollection<string> CompletedWorldBosses => _completedWorldBosses;

        public IReadOnlyCollection<string> CompletedMapChests => _completedMapChests;

        public void ResetAccessLogging() {
            _loggedAccessMessage = false;
        }

        /// <summary>
        /// Clears cached completions when the UTC day changes. Returns true if a reset was applied.
        /// </summary>
        public bool EnsureUtcDay(DateTime utcNow) {
            var utcMidnight = DailyResetHelper.UtcDay(utcNow);
            if (_lastUtcMidnight == utcMidnight) {
                return false;
            }

            ApplyDailyReset(utcMidnight);
            return true;
        }

        public async Task RefreshAsync(bool force = false) {
            var utcNow = DateTime.UtcNow;
            EnsureUtcDay(utcNow);

            if (!force && !_fetchFailed &&
                (utcNow - _lastRefreshUtc).TotalSeconds < MinRefreshSeconds) {
                return;
            }

            var hasAccount = _apiManager.HasPermission(TokenPermission.Account);
            var hasProgression = _apiManager.HasPermission(TokenPermission.Progression);
            var hasSubtoken = _apiManager.HasSubtoken;

            if (!hasAccount || !hasProgression) {
                _hasApiAccess = false;
                _fetchFailed = false;

                if (!hasSubtoken) {
                    SetAccessState(
                        ProgressAccessState.NoApiKey,
                        "Add a GW2 API key in Blish HUD (Settings → API Keys) with account and progression scopes. " +
                        "Then enable both permissions for GW2 Event Tracker under Settings → Modules.");
                } else {
                    SetAccessState(
                        ProgressAccessState.MissingModulePermissions,
                        "Enable account and progression API permissions for GW2 Event Tracker in Blish HUD → Settings → Modules. " +
                        "If you recently updated the module, try disabling and re-enabling it.");
                }

                if (!_loggedAccessMessage) {
                    _loggedAccessMessage = true;
                    Logger.Info("Daily progress unavailable — {Reason}", _statusMessage);
                }

                NotifyProgressChanged();
                return;
            }

            _hasApiAccess = true;
            _loggedAccessMessage = false;
            _worldBossFetchError = null;
            _mapChestFetchError = null;
            var utcDayAtStart = DailyResetHelper.UtcDay(utcNow);

            try {
                var client = _apiManager.Gw2ApiClient.V2;

                var worldBossFailed = false;
                var mapChestFailed = false;
                IReadOnlyList<string> worldBosses = Array.Empty<string>();
                IReadOnlyList<string> mapChests = Array.Empty<string>();

                try {
                    worldBosses = await client.Account.WorldBosses.GetAsync().ConfigureAwait(false);
                } catch (Exception ex) {
                    worldBossFailed = true;
                    _worldBossFetchError = ex.Message;
                    Logger.Warn(ex, "Failed to refresh world boss progress from GW2 API.");
                }

                try {
                    mapChests = await client.Account.MapChests.GetAsync().ConfigureAwait(false);
                } catch (Exception ex) {
                    mapChestFailed = true;
                    _mapChestFetchError = ex.Message;
                    Logger.Warn(ex, "Failed to refresh map chest progress from GW2 API.");
                }

                if (DailyResetHelper.HasUtcDayChanged(utcDayAtStart, DateTime.UtcNow)) {
                    EnsureUtcDay(DateTime.UtcNow);
                    Logger.Info("Discarded stale GW2 API progress fetched across UTC daily reset.");
                    return;
                }

                if (worldBossFailed && mapChestFailed) {
                    throw new InvalidOperationException("Both world boss and map chest progress requests failed.");
                }

                if (!worldBossFailed) {
                    _completedWorldBosses = new HashSet<string>(worldBosses, StringComparer.OrdinalIgnoreCase);
                }

                if (!mapChestFailed) {
                    _completedMapChests = new HashSet<string>(mapChests, StringComparer.OrdinalIgnoreCase);
                }

                _lastRefreshUtc = DateTime.UtcNow;
                _fetchFailed = worldBossFailed || mapChestFailed;

                if (_fetchFailed) {
                    SetAccessState(
                        ProgressAccessState.FetchFailed,
                        BuildFetchFailedMessage());
                } else {
                    SetAccessState(
                        ProgressAccessState.Ready,
                        BuildReadyMessage());
                }

                Logger.Info(
                    "Progress refreshed: {WorldBossCount} world bosses, {MapChestCount} map chests.",
                    _completedWorldBosses.Count,
                    _completedMapChests.Count);

                NotifyProgressChanged();
            } catch (Exception ex) {
                _fetchFailed = true;
                _hasApiAccess = false;
                SetAccessState(
                    ProgressAccessState.FetchFailed,
                    "Failed to refresh daily progress from the GW2 API. Check your API key and module permissions.");
                Logger.Warn(ex, "Failed to refresh account progress from GW2 API.");
                NotifyProgressChanged();
            }
        }

        /// <summary>
        /// Called when the schedule engine detects UTC midnight. Clears cached completion
        /// immediately so the UI updates, then always fetches fresh API data.
        /// </summary>
        public void HandleDailyReset() {
            EnsureUtcDay(DateTime.UtcNow);
            _ = RefreshAsync(force: true);
        }

        private void ApplyDailyReset(DateTime utcMidnight) {
            _completedWorldBosses.Clear();
            _completedMapChests.Clear();
            _lastUtcMidnight = utcMidnight;
            NotifyProgressChanged();
            Logger.Info("Daily reset applied (UTC {Date:yyyy-MM-dd}).", utcMidnight);
        }

        public void ApplyCompletionStates(IEnumerable<TrackedEvent> events) {
            foreach (var tracked in events) {
                tracked.CompletionState = _catalog.GetCompletionState(
                    tracked.SegmentName,
                    _completedWorldBosses,
                    _completedMapChests);
            }
        }

        public bool IsRewardCompleted(TrackedEvent trackedEvent) {
            return _catalog.IsCompleted(
                trackedEvent.SegmentName,
                _completedWorldBosses,
                _completedMapChests);
        }

        public bool MatchesTrackableName(string segmentName, string trackableDisplayName) {
            foreach (var reward in _catalog.UniqueRewards) {
                if (!reward.ScheduleName.Equals(trackableDisplayName, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                return _catalog.MatchesScheduleName(segmentName, reward);
            }

            return false;
        }

        public IReadOnlyList<TrackableRewardStatus> GetTrackableRewardStatuses() {
            return _catalog.UniqueRewards
                .Select(reward => new TrackableRewardStatus {
                    DisplayName = reward.ScheduleName,
                    TrackType = reward.TrackType,
                    ApiId = reward.ApiId,
                    IsCompleted = reward.TrackType.Equals("worldboss", StringComparison.OrdinalIgnoreCase)
                        ? _completedWorldBosses.Contains(reward.ApiId)
                        : _completedMapChests.Contains(reward.ApiId)
                })
                .ToList();
        }

        public RewardProgressSummary GetSummary() {
            var unique = _catalog.UniqueRewards.ToList();
            var completed = unique.Count(r =>
                r.TrackType.Equals("worldboss", StringComparison.OrdinalIgnoreCase)
                    ? _completedWorldBosses.Contains(r.ApiId)
                    : _completedMapChests.Contains(r.ApiId));

            return new RewardProgressSummary {
                Completed = completed,
                Trackable = unique.Count
            };
        }

        private string BuildReadyMessage() =>
            $"Synced {_completedWorldBosses.Count} world bosses and {_completedMapChests.Count} map chests.";

        private string BuildFetchFailedMessage() {
            var parts = new List<string>();

            if (_worldBossFetchError != null) {
                parts.Add($"world bosses: {_worldBossFetchError}");
            }

            if (_mapChestFetchError != null) {
                parts.Add($"map chests: {_mapChestFetchError}");
            }

            return parts.Count == 0
                ? "Could not refresh all daily progress from the GW2 API. Retrying shortly."
                : $"API error — {string.Join("; ", parts)}";
        }

        private void SetAccessState(ProgressAccessState state, string message) {
            _accessState = state;
            _statusMessage = message;
        }

        private void NotifyProgressChanged() {
            ProgressUpdated?.Invoke(this, EventArgs.Empty);
        }
    }

}
