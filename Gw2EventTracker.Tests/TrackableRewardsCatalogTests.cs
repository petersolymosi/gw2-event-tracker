using System.Collections.Generic;
using System.Linq;
using Ghost.Gw2EventTracker.Models;
using Ghost.Gw2EventTracker.Services;
using Xunit;

namespace Ghost.Gw2EventTracker.Tests {

    public sealed class TrackableRewardsCatalogTests {

        [Fact]
        public void MatchesScheduleName_ResolvesAlsoAppliesToAlias() {
            var catalog = CreateCatalog(new TrackableRewardDefinition {
                ScheduleName = "The Battle for Amnytas",
                TrackType = "mapchest",
                ApiId = "amnytas_heros_choice_chest",
                AlsoAppliesTo = new List<string> { "Defense of Amnytas" }
            });

            Assert.True(catalog.MatchesScheduleName("Defense of Amnytas", catalog.UniqueRewards.First()));
        }

        [Fact]
        public void IsCompleted_UsesAliasLookupForSegmentName() {
            var catalog = CreateCatalog(new TrackableRewardDefinition {
                ScheduleName = "The Battle for Amnytas",
                TrackType = "mapchest",
                ApiId = "amnytas_heros_choice_chest",
                AlsoAppliesTo = new List<string> { "Defense of Amnytas" }
            });

            var mapChests = new HashSet<string> { "amnytas_heros_choice_chest" };

            Assert.True(catalog.IsCompleted("Defense of Amnytas", new HashSet<string>(), mapChests));
        }

        [Fact]
        public void GetCompletionState_ReturnsNotTrackableForUnknownSegment() {
            var catalog = CreateCatalog(new TrackableRewardDefinition {
                ScheduleName = "Tequatl",
                TrackType = "worldboss",
                ApiId = "tequatl"
            });

            var state = catalog.GetCompletionState("Unknown Boss", new HashSet<string>(), new HashSet<string>());

            Assert.Equal(CompletionState.NotTrackable, state);
        }

        [Fact]
        public void GetCompletionState_ReturnsPendingWhenNotCompleted() {
            var catalog = CreateCatalog(new TrackableRewardDefinition {
                ScheduleName = "Tequatl",
                TrackType = "worldboss",
                ApiId = "tequatl"
            });

            var state = catalog.GetCompletionState("Tequatl", new HashSet<string>(), new HashSet<string>());

            Assert.Equal(CompletionState.Pending, state);
        }

        private static TrackableRewardsCatalog CreateCatalog(TrackableRewardDefinition definition) {
            return new TrackableRewardsCatalog(new TrackableRewardsFile {
                Segments = new List<TrackableRewardDefinition> { definition }
            });
        }
    }

}
