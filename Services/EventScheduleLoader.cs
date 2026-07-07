using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Debug;
using Ghost.Gw2EventTracker.Models;
using Newtonsoft.Json;

namespace Ghost.Gw2EventTracker.Services {

    public static class EventScheduleLoader {

        private static readonly Logger Logger = Logger.GetLogger(typeof(EventScheduleLoader));

        private const int HttpAccessDeniedHResult = unchecked((int)0x80004005);

        private const string RemoteEventsUrl =
            "https://raw.githubusercontent.com/giovazz89/gw2-api-event-timers/master/events.json";

        public static async Task<EventScheduleLoadResult> LoadSectionsAsync(bool preferRemote = true) {
            if (!preferRemote) {
                return LoadEmbedded();
            }

            try {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(12) }) {
                    var json = await client.GetStringAsync(RemoteEventsUrl).ConfigureAwait(false);
                    var sections = JsonConvert.DeserializeObject<List<EventSectionDefinition>>(json);

                    if (sections == null || sections.Count == 0) {
                        throw new InvalidOperationException("Remote events.json was empty.");
                    }

                    Logger.Info("Loaded {Count} schedule sections from remote events.json.", sections.Count);
                    EventSchedulePatcher.Apply(sections);
                    return new EventScheduleLoadResult(sections, usedRemote: true);
                }
            } catch (Exception ex) {
                if (IsHttpAccessDenied(ex)) {
                    Contingency.NotifyHttpAccessDenied("fetching the remote event schedule");
                }

                Logger.Warn(ex, "Failed to load remote events.json; using embedded schedule.");
                return LoadEmbedded();
            }
        }

        private static EventScheduleLoadResult LoadEmbedded() {
            try {
                var embedded = JsonConvert.DeserializeObject<List<EventSectionDefinition>>(
                    ModuleData.ReadEmbedded("events.json")) ?? new List<EventSectionDefinition>();

                EventSchedulePatcher.Apply(embedded);
                return new EventScheduleLoadResult(embedded, usedRemote: false);
            } catch (Exception ex) {
                Logger.Warn(ex, "Failed to load embedded events.json; schedule will be empty.");
                return new EventScheduleLoadResult(new List<EventSectionDefinition>(), usedRemote: false);
            }
        }

        private static bool IsHttpAccessDenied(Exception ex) {
            for (var current = ex; current != null; current = current.InnerException) {
                if (current.HResult == HttpAccessDeniedHResult) {
                    return true;
                }
            }

            return false;
        }
    }

    public sealed class EventScheduleLoadResult {
        public EventScheduleLoadResult(IReadOnlyList<EventSectionDefinition> sections, bool usedRemote) {
            Sections = sections;
            UsedRemote = usedRemote;
        }

        public IReadOnlyList<EventSectionDefinition> Sections { get; }
        public bool UsedRemote { get; }
    }

}
