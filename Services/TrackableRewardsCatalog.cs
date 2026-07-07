using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ghost.Gw2EventTracker.Models;
using Newtonsoft.Json;

namespace Ghost.Gw2EventTracker.Services {

    public sealed class TrackableRewardsCatalog {

        private readonly Dictionary<string, TrackableRewardDefinition> _byScheduleName;

        public TrackableRewardsCatalog(string jsonPath) : this(
            JsonConvert.DeserializeObject<TrackableRewardsFile>(File.ReadAllText(jsonPath))
            ?? new TrackableRewardsFile()) {
        }

        public TrackableRewardsCatalog(TrackableRewardsFile file) {
            _byScheduleName = new Dictionary<string, TrackableRewardDefinition>(StringComparer.OrdinalIgnoreCase);

            foreach (var segment in file.Segments) {
                Register(segment.ScheduleName, segment);
                foreach (var alias in segment.AlsoAppliesTo ?? Enumerable.Empty<string>()) {
                    Register(alias, segment);
                }
            }
        }

        private void Register(string name, TrackableRewardDefinition definition) {
            if (string.IsNullOrWhiteSpace(name)) {
                return;
            }

            _byScheduleName[name] = definition;
        }

        public bool TryGet(string scheduleName, out TrackableRewardDefinition definition) {
            return _byScheduleName.TryGetValue(scheduleName, out definition!);
        }

        public bool MatchesScheduleName(string scheduleName, TrackableRewardDefinition definition) {
            if (string.IsNullOrWhiteSpace(scheduleName)) {
                return false;
            }

            if (definition.ScheduleName.Equals(scheduleName, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }

            return definition.AlsoAppliesTo?.Any(alias =>
                alias.Equals(scheduleName, StringComparison.OrdinalIgnoreCase)) == true;
        }

        public IEnumerable<TrackableRewardDefinition> UniqueRewards =>
            _byScheduleName.Values
                .GroupBy(r => r.ApiId, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First());

        public bool IsCompleted(string scheduleName, ISet<string> worldBosses, ISet<string> mapChests) {
            if (!_byScheduleName.TryGetValue(scheduleName, out var reward)) {
                return false;
            }

            return reward.TrackType.Equals("worldboss", StringComparison.OrdinalIgnoreCase)
                ? worldBosses.Contains(reward.ApiId)
                : mapChests.Contains(reward.ApiId);
        }

        public CompletionState GetCompletionState(string scheduleName, ISet<string> worldBosses, ISet<string> mapChests) {
            if (!_byScheduleName.TryGetValue(scheduleName, out _)) {
                return CompletionState.NotTrackable;
            }

            return IsCompleted(scheduleName, worldBosses, mapChests)
                ? CompletionState.Completed
                : CompletionState.Pending;
        }
    }

}
