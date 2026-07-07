using System;
using System.Collections.Generic;
using Ghost.Gw2EventTracker.Services;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace Ghost.Gw2EventTracker.Models {

    public sealed class EventSectionDefinition {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("link")]
        public string Link { get; set; } = string.Empty;

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("segments")]
        public List<EventSegmentDefinition> Segments { get; set; } = new List<EventSegmentDefinition>();

        [JsonProperty("sequences")]
        public EventSequences Sequences { get; set; } = new EventSequences();
    }

    public sealed class EventSegmentDefinition {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("chatlink")]
        public string ChatLink { get; set; } = string.Empty;

        [JsonProperty("link")]
        public string Link { get; set; } = string.Empty;

        [JsonProperty("icon")]
        public string Icon { get; set; } = string.Empty;

        [JsonProperty("bg")]
        public List<int> BackgroundRgb { get; set; } = new List<int>();

        [JsonProperty("lfg")]
        public int? RecommendedLfg { get; set; }

        [JsonProperty("rewards")]
        public EventRewardsDefinition? Rewards { get; set; }
    }

    public sealed class EventRewardsDefinition {
        [JsonProperty("random_items")]
        public List<int> RandomItems { get; set; } = new List<int>();

        [JsonProperty("achievements")]
        public List<int> Achievements { get; set; } = new List<int>();

        [JsonProperty("items_for_achievements")]
        public List<AchievementItemReward> ItemsForAchievements { get; set; } = new List<AchievementItemReward>();
    }

    public sealed class AchievementItemReward {
        [JsonProperty("a")]
        public int AchievementId { get; set; }

        [JsonProperty("i")]
        public int ItemId { get; set; }
    }

    public sealed class EventSequences {
        [JsonProperty("partial")]
        public List<SequenceStep> Partial { get; set; } = new List<SequenceStep>();

        [JsonProperty("pattern")]
        public List<SequenceStep> Pattern { get; set; } = new List<SequenceStep>();
    }

    public sealed class SequenceStep {
        [JsonProperty("r")]
        public int SegmentRef { get; set; }

        [JsonProperty("d")]
        public int DurationMinutes { get; set; }
    }

    public sealed class TrackableRewardsFile {
        [JsonProperty("segments")]
        public List<TrackableRewardDefinition> Segments { get; set; } = new List<TrackableRewardDefinition>();
    }

    public sealed class TrackableRewardDefinition {
        [JsonProperty("scheduleName")]
        public string ScheduleName { get; set; } = string.Empty;

        [JsonProperty("trackType")]
        public string TrackType { get; set; } = string.Empty;

        [JsonProperty("apiId")]
        public string ApiId { get; set; } = string.Empty;

        [JsonProperty("alsoAppliesTo")]
        public List<string> AlsoAppliesTo { get; set; } = new List<string>();
    }

    public enum CompletionState {
        NotTrackable,
        Pending,
        Completed
    }

    public sealed class TrackedEvent {
        public string Key { get; set; } = string.Empty;
        public int SectionId { get; set; }
        public int SegmentId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string ExpansionCategory { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public string SegmentName { get; set; } = string.Empty;
        public string ChatLink { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string WikiUrl { get; set; } = string.Empty;
        public Color? AccentColor { get; set; }
        public EventRewardsDefinition? Rewards { get; set; }
        public int? RecommendedLfg { get; set; }
        public bool IsWatched { get; set; } = true;
        public DateTime NextStartUtc { get; set; }
        public DateTime NextEndUtc { get; set; }
        public bool IsActive { get; set; }
        public CompletionState CompletionState { get; set; } = CompletionState.NotTrackable;
        public HashSet<int> AlertedLeadTimes { get; } = new HashSet<int>();
        public bool AlertedStarted { get; set; }

        public string DisplayLabel => EventCategoryMapper.FormatDisplayLabel(SectionName, SegmentName, Category);

        public void ResetAlerts() {
            AlertedLeadTimes.Clear();
            AlertedStarted = false;
        }
    }

    public sealed class RewardProgressSummary {
        public int Completed { get; set; }
        public int Trackable { get; set; }
    }

}