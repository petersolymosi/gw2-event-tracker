using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Gw2EventTracker.Models;
using Gw2Sharp.WebApi.V2.Models;

namespace Gw2EventTracker.Services {

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

        public event EventHandler? ProgressUpdated;

        /// <summary>True when the last API fetch failed; module should retry sooner.</summary>
        public bool NeedsUrgentRefresh => _fetchFailed;

        public AccountProgressService(Gw2ApiManager apiManager, TrackableRewardsCatalog catalog) {
            _apiManager = apiManager;
            _catalog = catalog;
        }

        public bool HasApiAccess => _hasApiAccess;

        public IReadOnlyCollection<string> CompletedWorldBosses => _completedWorldBosses;

        public IReadOnlyCollection<string> CompletedMapChests => _completedMapChests;

        public async Task RefreshAsync(bool force = false) {
            var utcNow = DateTime.UtcNow;

            if (!force && !_fetchFailed && (utcNow - _lastRefreshUtc).TotalMinutes < 5) {
                return;
            }

            if (!_apiManager.HasPermissions(new[] { TokenPermission.Account, TokenPermission.Progression })) {
                _hasApiAccess = false;
                return;
            }

            _hasApiAccess = true;

            try {
                var client = _apiManager.Gw2ApiClient.V2;

                var worldBosses = await client.Account.WorldBosses.GetAsync().ConfigureAwait(false);
                var mapChests = await client.Account.MapChests.GetAsync().ConfigureAwait(false);

                _completedWorldBosses = new HashSet<string>(worldBosses, StringComparer.OrdinalIgnoreCase);
                _completedMapChests = new HashSet<string>(mapChests, StringComparer.OrdinalIgnoreCase);
                _lastRefreshUtc = utcNow;
                _fetchFailed = false;

                ProgressUpdated?.Invoke(this, EventArgs.Empty);
            } catch (Exception ex) {
                _fetchFailed = true;
                Logger.Warn(ex, "Failed to refresh account progress from GW2 API.");
            }
        }

        /// <summary>
        /// Called when the schedule engine detects UTC midnight. Clears cached completion
        /// immediately so the UI updates, then always fetches fresh API data.
        /// </summary>
        public void HandleDailyReset() {
            var utcMidnight = DateTime.UtcNow.Date;
            if (_lastUtcMidnight != utcMidnight) {
                ApplyDailyReset(utcMidnight);
            }

            _ = RefreshAsync(force: true);
        }

        private void ApplyDailyReset(DateTime utcMidnight) {
            _completedWorldBosses.Clear();
            _completedMapChests.Clear();
            _lastUtcMidnight = utcMidnight;
            ProgressUpdated?.Invoke(this, EventArgs.Empty);
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
    }

}
