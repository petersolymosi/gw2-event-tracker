using System;
using System.Collections.Generic;
using System.Linq;
using Ghost.Gw2EventTracker.Models;

namespace Ghost.Gw2EventTracker.Services {

    internal static class EventSchedulePatcher {

        private static readonly (string SectionName, string FromName, string ToName)[] SegmentRenames = {
            ("Amnytas", "Defense of Amnytas", "The Battle for Amnytas"),
            ("Amnytas", "The Defense of Amnytas", "The Battle for Amnytas")
        };

        public static void Apply(IList<EventSectionDefinition> sections) {
            RenameSegments(sections);
            EnsureInnerNayosSection(sections);
        }

        private static void RenameSegments(IList<EventSectionDefinition> sections) {
            foreach (var (sectionName, fromName, toName) in SegmentRenames) {
                var section = sections.FirstOrDefault(s =>
                    s.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase));

                if (section == null) {
                    continue;
                }

                foreach (var segment in section.Segments) {
                    if (segment.Name.Equals(fromName, StringComparison.OrdinalIgnoreCase)) {
                        segment.Name = toName;
                        if (string.IsNullOrWhiteSpace(segment.Link)) {
                            segment.Link = "The Defense of Amnytas";
                        }
                    }
                }
            }
        }

        private static void EnsureInnerNayosSection(IList<EventSectionDefinition> sections) {
            if (sections.Any(s => s.Name.Equals("Inner Nayos", StringComparison.OrdinalIgnoreCase))) {
                return;
            }

            sections.Add(new EventSectionDefinition {
                Id = 42,
                Active = true,
                Category = "Midnight King",
                Name = "Inner Nayos",
                Segments = new List<EventSegmentDefinition> {
                    new EventSegmentDefinition {
                        Id = 0,
                        Name = string.Empty,
                        BackgroundRgb = new List<int> { 200, 160, 220 }
                    },
                    new EventSegmentDefinition {
                        Id = 1,
                        Name = "Into the Spider's Lair",
                        Link = "Into the Spider's Lair",
                        ChatLink = "[&BG8OAAA=]",
                        BackgroundRgb = new List<int> { 180, 130, 200 },
                        Icon = "https://render.guildwars2.com/file/0339A231A4973455D69AD7DCF222F50C1148206E/3124847.png",
                        RecommendedLfg = 50
                    }
                },
                Sequences = new EventSequences()
            });
        }
    }

}
