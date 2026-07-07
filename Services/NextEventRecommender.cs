using System;
using System.Collections.Generic;
using System.Linq;
using Ghost.Gw2EventTracker.Models;

namespace Ghost.Gw2EventTracker.Services {

    public sealed class RecommenderResult {
        public TrackedEvent Event { get; set; } = null!;
        public double Score { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public static class NextEventRecommender {

        public static RecommenderResult? Recommend(
            IEnumerable<TrackedEvent> events,
            ModuleSettings settings,
            int? currentMapId) {

            var utcNow = DateTime.UtcNow;
            RecommenderResult? best = null;

            foreach (var tracked in events) {
                if (settings.IsSnoozed(tracked.Key)) {
                    continue;
                }

                if (tracked.NextStartUtc == default && !tracked.IsActive) {
                    continue;
                }

                var score = ScoreEvent(tracked, settings, currentMapId, utcNow);
                if (score <= 0) {
                    continue;
                }

                if (best == null || score > best.Score) {
                    best = new RecommenderResult {
                        Event = tracked,
                        Score = score,
                        Reason = BuildReason(tracked, currentMapId, utcNow)
                    };
                }
            }

            return best;
        }

        private static double ScoreEvent(
            TrackedEvent tracked,
            ModuleSettings settings,
            int? currentMapId,
            DateTime utcNow) {

            var score = 0d;

            if (tracked.IsActive) {
                score += 120;
            }

            if (tracked.IsWatched) {
                score += 35;
            }

            if (tracked.CompletionState == CompletionState.Pending) {
                score += 40;
            } else if (tracked.CompletionState == CompletionState.NotTrackable) {
                score += 5;
            }

            if (!tracked.IsActive) {
                var minutesUntil = (tracked.NextStartUtc - utcNow).TotalMinutes;
                if (minutesUntil <= 10) {
                    score += 45;
                } else if (minutesUntil <= 30) {
                    score += 30;
                } else if (minutesUntil <= 90) {
                    score += 15;
                } else if (minutesUntil > 240) {
                    score -= 25;
                }
            }

            if (currentMapId.HasValue && EventMapMapper.IsOnMap(tracked, currentMapId.Value)) {
                score += tracked.IsActive ? 50 : 30;
            }

            if (tracked.Category == "World Bosses" && tracked.IsWatched) {
                score += 10;
            }

            return score;
        }

        private static string BuildReason(TrackedEvent tracked, int? currentMapId, DateTime utcNow) {
            if (tracked.IsActive) {
                if (currentMapId.HasValue && EventMapMapper.IsOnMap(tracked, currentMapId.Value)) {
                    return "Active now on your map";
                }

                return "Active now";
            }

            var minutesUntil = Math.Max(0, (tracked.NextStartUtc - utcNow).TotalMinutes);
            if (currentMapId.HasValue && EventMapMapper.IsOnMap(tracked, currentMapId.Value)) {
                return $"On your map in ~{Math.Ceiling(minutesUntil)} min";
            }

            if (tracked.CompletionState == CompletionState.Pending) {
                return $"Daily reward pending, starts in ~{Math.Ceiling(minutesUntil)} min";
            }

            if (tracked.IsWatched) {
                return $"Watched event in ~{Math.Ceiling(minutesUntil)} min";
            }

            return $"Starts in ~{Math.Ceiling(minutesUntil)} min";
        }
    }

}
