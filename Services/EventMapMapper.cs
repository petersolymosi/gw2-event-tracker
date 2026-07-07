using System;
using System.Collections.Generic;
using Ghost.Gw2EventTracker.Models;

namespace Ghost.Gw2EventTracker.Services {

    public static class EventMapMapper {

        private static readonly Dictionary<string, int> SectionMapIds =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) {
                ["Dry Top"] = 1195,
                ["Verdant Brink"] = 1052,
                ["Auric Basin"] = 1041,
                ["Tangled Depths"] = 1043,
                ["Dragon's Stand"] = 1045,
                ["Lake Doric"] = 1185,
                ["Crystal Oasis"] = 1210,
                ["Desert Highlands"] = 1211,
                ["Elon Riverlands"] = 1228,
                ["The Desolation"] = 1226,
                ["Domain of Vabbi"] = 1248,
                ["Domain of Istan"] = 1263,
                ["Jahai Bluffs"] = 1301,
                ["Thunderhead Peaks"] = 1310,
                ["Bjora Marches"] = 1319,
                ["Grothmar Valley"] = 1330,
                ["Dragonstorm"] = 1374,
                ["Seitung Province"] = 1370,
                ["New Kaineng City"] = 1438,
                ["The Echovald Wilds"] = 1452,
                ["Dragon's End"] = 1422,
                ["Skywatch Archipelago"] = 1526,
                ["Amnytas"] = 1517,
                ["Inner Nayos"] = 1550,
                ["Janthir Syntri"] = 1554,
                ["Bava Nisos"] = 1556,
                ["Shipwreck Strand"] = 1596,
                ["Starlit Weald"] = 1604,
                ["Eye of the North"] = 872,
                ["Mount Balrior"] = 1550
            };

        private static readonly Dictionary<string, int> SegmentMapIds =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) {
                ["Admiral Taidha Covington"] = 73,
                ["Claw of Jormag"] = 31,
                ["Fire Elemental"] = 23,
                ["Golem Mark II"] = 39,
                ["Great Jungle Wurm"] = 34,
                ["Megadestroyer"] = 39,
                ["Modniir Ulgoth"] = 25,
                ["Shadow Behemoth"] = 17,
                ["Svanir Shaman Chief"] = 30,
                ["The Shatterer"] = 20,
                ["Evolved Jungle Wurm"] = 34,
                ["Karka Queen"] = 873,
                ["Tequatl the Sunless"] = 53,
                ["Timberline Falls"] = 27,
                ["Iron Marches"] = 24,
                ["Gendarran Fields"] = 13
            };

        public static int? GetMapId(TrackedEvent tracked) {
            if (SectionMapIds.TryGetValue(tracked.SectionName, out var sectionMapId)) {
                return sectionMapId;
            }

            if (SegmentMapIds.TryGetValue(tracked.SegmentName, out var segmentMapId)) {
                return segmentMapId;
            }

            if (SectionMapIds.TryGetValue(tracked.SegmentName, out var segmentAsSection)) {
                return segmentAsSection;
            }

            return null;
        }

        public static bool IsOnMap(TrackedEvent tracked, int mapId) {
            var eventMapId = GetMapId(tracked);
            return eventMapId.HasValue && eventMapId.Value == mapId;
        }
    }

}
