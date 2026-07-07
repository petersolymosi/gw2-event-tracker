using System.Collections.Generic;
using System.Linq;
using Ghost.Gw2EventTracker.Models;
using Ghost.Gw2EventTracker.Services;
using Xunit;

namespace Ghost.Gw2EventTracker.Tests {

    public sealed class EventSchedulePatcherTests {

        [Fact]
        public void Apply_RenamesDefenseOfAmnytasToBattleForAmnytas() {
            var sections = new List<EventSectionDefinition> {
                new EventSectionDefinition {
                    Name = "Amnytas",
                    Segments = new List<EventSegmentDefinition> {
                        new EventSegmentDefinition { Name = "Defense of Amnytas" }
                    }
                }
            };

            EventSchedulePatcher.Apply(sections);

            Assert.Equal("The Battle for Amnytas", sections[0].Segments[0].Name);
        }

        [Fact]
        public void Apply_AddsInnerNayosWhenMissing() {
            var sections = new List<EventSectionDefinition>();

            EventSchedulePatcher.Apply(sections);

            var innerNayos = sections.Single(s => s.Name == "Inner Nayos");
            Assert.Contains(innerNayos.Segments, s => s.Name == "Into the Spider's Lair");
            Assert.Empty(innerNayos.Sequences.Pattern);
        }

        [Fact]
        public void Apply_DoesNotDuplicateInnerNayos() {
            var sections = new List<EventSectionDefinition> {
                new EventSectionDefinition { Name = "Inner Nayos" }
            };

            EventSchedulePatcher.Apply(sections);

            Assert.Single(sections.Where(s => s.Name == "Inner Nayos"));
        }
    }

}
