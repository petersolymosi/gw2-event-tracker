using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Modules.Managers;

namespace Ghost.Gw2EventTracker.Services {

    public sealed class AchievementInfoCache {

        private static readonly Logger Logger = Logger.GetLogger<AchievementInfoCache>();

        private readonly Gw2ApiManager _apiManager;
        private readonly Dictionary<int, string> _names = new Dictionary<int, string>();

        public AchievementInfoCache(Gw2ApiManager apiManager) {
            _apiManager = apiManager;
        }

        public async Task PrefetchAsync(IEnumerable<int> achievementIds) {
            var ids = achievementIds
                .Distinct()
                .Where(id => id > 0 && !_names.ContainsKey(id))
                .ToList();

            if (ids.Count == 0) {
                return;
            }

            try {
                var client = _apiManager.Gw2ApiClient.V2.Achievements;

                foreach (var id in ids) {
                    var achievement = await client.GetAsync(id).ConfigureAwait(false);
                    _names[achievement.Id] = achievement.Name;
                }
            } catch (Exception ex) {
                Logger.Warn(ex, "Failed to prefetch achievement names from GW2 API.");
            }
        }

        public string GetName(int achievementId) {
            return _names.TryGetValue(achievementId, out var name)
                ? name
                : $"Achievement {achievementId}";
        }
    }

}
