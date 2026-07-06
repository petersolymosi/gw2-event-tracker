using System;
using System.Collections.Generic;

namespace Gw2EventTracker.Services {

    public static class EventCategoryMapper {

        public static readonly IReadOnlyList<string> MenuCategories = new[] {
            "Day-Night Cycle",
            "World Bosses",
            "Meta Event",
            "Group Event",
            "Adventure",
            "Convergence",
            "Other"
        };

        private static readonly HashSet<string> MetaSections = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "Dry Top",
            "Verdant Brink", "Auric Basin", "Tangled Depths", "Dragon's Stand",
            "Lake Doric",
            "Crystal Oasis", "Desert Highlands", "Elon Riverlands", "The Desolation", "Domain of Vabbi",
            "Domain of Istan", "Jahai Bluffs", "Thunderhead Peaks",
            "Bjora Marches", "Grothmar Valley", "Dragonstorm",
            "Seitung Province", "New Kaineng City", "The Echovald Wilds", "Dragon's End",
            "Skywatch Archipelago", "Amnytas", "Inner Nayos",
            "Janthir Syntri", "Bava Nisos",
            "Shipwreck Strand", "Starlit Weald"
        };

        public static string FormatDisplayLabel(string sectionName, string segmentName, string category) {
            if (string.Equals(category, "Day-Night Cycle", StringComparison.OrdinalIgnoreCase)) {
                if (sectionName.IndexOf("Cantha", StringComparison.OrdinalIgnoreCase) >= 0) {
                    return $"{segmentName} - Cantha";
                }

                return segmentName;
            }

            if (string.IsNullOrWhiteSpace(sectionName) || sectionName == segmentName) {
                return segmentName;
            }

            return $"{segmentName} ({sectionName})";
        }

        public static int GetDayNightSortOrder(string sectionName, string segmentName) {
            var regionOffset = sectionName.IndexOf("Cantha", StringComparison.OrdinalIgnoreCase) >= 0 ? 10 : 0;

            var phaseOffset = segmentName switch {
                "Dawn" => 0,
                "Day" => 1,
                "Dusk" => 2,
                "Night" => 3,
                _ => 9
            };

            return regionOffset + phaseOffset;
        }


        public static string Resolve(string sectionName, string segmentName) {
            if (sectionName.IndexOf("Day and night", StringComparison.OrdinalIgnoreCase) >= 0) {
                return "Day-Night Cycle";
            }

            if (sectionName.Equals("World bosses", StringComparison.OrdinalIgnoreCase)
                || sectionName.Equals("Hard world bosses", StringComparison.OrdinalIgnoreCase)) {
                return "World Bosses";
            }

            if (sectionName.Equals("Ley-Line Anomaly", StringComparison.OrdinalIgnoreCase)
                || sectionName.Equals("Eye of the North", StringComparison.OrdinalIgnoreCase)
                || sectionName.Equals("Scarlet's Invasion", StringComparison.OrdinalIgnoreCase)) {
                return "Group Event";
            }

            if (sectionName.Equals("PvP Tournaments", StringComparison.OrdinalIgnoreCase)) {
                return "Other";
            }

            if (sectionName.Equals("Convergences", StringComparison.OrdinalIgnoreCase)
                || sectionName.Equals("Mount Balrior", StringComparison.OrdinalIgnoreCase)
                || segmentName.IndexOf("Convergence", StringComparison.OrdinalIgnoreCase) >= 0) {
                return "Convergence";
            }

            if (segmentName.IndexOf("Target Practice", StringComparison.OrdinalIgnoreCase) >= 0
                || segmentName.IndexOf("Fly by Night", StringComparison.OrdinalIgnoreCase) >= 0) {
                return "Adventure";
            }

            if (MetaSections.Contains(sectionName)) {
                return "Meta Event";
            }

            return "Other";
        }
    }

}
