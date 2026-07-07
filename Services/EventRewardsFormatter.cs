using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ghost.Gw2EventTracker.Models;
using Microsoft.Xna.Framework;

namespace Ghost.Gw2EventTracker.Services {

    public static class EventRewardsFormatter {

        public static bool HasRewards(EventRewardsDefinition? rewards) {
            if (rewards == null) {
                return false;
            }

            return rewards.Achievements.Count > 0
                || rewards.ItemsForAchievements.Count > 0
                || rewards.RandomItems.Count > 0;
        }

        public static string FormatSummary(EventRewardsDefinition? rewards, AchievementInfoCache? cache) {
            if (!HasRewards(rewards)) {
                return string.Empty;
            }

            var parts = new List<string>();

            if (rewards!.Achievements.Count > 0) {
                parts.Add($"{rewards.Achievements.Count} achievement{(rewards.Achievements.Count == 1 ? string.Empty : "s")}");
            }

            if (rewards.ItemsForAchievements.Count > 0) {
                parts.Add($"{rewards.ItemsForAchievements.Count} daily item{(rewards.ItemsForAchievements.Count == 1 ? string.Empty : "s")}");
            }

            if (rewards.RandomItems.Count > 0) {
                parts.Add($"{rewards.RandomItems.Count} random item{(rewards.RandomItems.Count == 1 ? string.Empty : "s")}");
            }

            return $"Rewards: {string.Join(", ", parts)}";
        }

        public static string FormatTooltip(EventRewardsDefinition? rewards, AchievementInfoCache? cache) {
            if (!HasRewards(rewards)) {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.AppendLine(FormatSummary(rewards, cache));

            if (rewards!.Achievements.Count > 0 && cache != null) {
                builder.AppendLine("Achievements:");
                foreach (var id in rewards.Achievements.Take(6)) {
                    builder.AppendLine($"  {cache.GetName(id)}");
                }

                if (rewards.Achievements.Count > 6) {
                    builder.AppendLine($"  +{rewards.Achievements.Count - 6} more");
                }
            }

            if (rewards.ItemsForAchievements.Count > 0) {
                builder.AppendLine("Daily items tied to achievements.");
            }

            return builder.ToString().TrimEnd();
        }

        public static IEnumerable<int> CollectAchievementIds(IEnumerable<EventSectionDefinition> sections) {
            foreach (var section in sections) {
                foreach (var segment in section.Segments) {
                    if (segment.Rewards == null) {
                        continue;
                    }

                    foreach (var id in segment.Rewards.Achievements) {
                        yield return id;
                    }

                    foreach (var item in segment.Rewards.ItemsForAchievements) {
                        yield return item.AchievementId;
                    }
                }
            }
        }

        public static Color? ParseAccentColor(IReadOnlyList<int>? rgb) {
            if (rgb == null || rgb.Count < 3) {
                return null;
            }

            return new Color(
                ClampByte(rgb[0]),
                ClampByte(rgb[1]),
                ClampByte(rgb[2]));
        }

        private static byte ClampByte(int value) {
            if (value < 0) {
                return 0;
            }

            if (value > 255) {
                return 255;
            }

            return (byte)value;
        }
    }

}
