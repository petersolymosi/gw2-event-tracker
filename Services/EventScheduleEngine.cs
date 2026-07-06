using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gw2EventTracker.Models;
using Newtonsoft.Json;

namespace Gw2EventTracker.Services {

    public sealed class EventScheduleEngine {

        private readonly List<EventSectionDefinition> _sections;
        private readonly List<TrackedEvent> _events = new List<TrackedEvent>();
        private DateTime _lastUtcMidnight = DateTime.MinValue;

        public IReadOnlyList<TrackedEvent> Events => _events;

        public IReadOnlyList<EventSectionDefinition> Sections => _sections;

        public EventScheduleEngine(string eventsJsonPath) : this(
            JsonConvert.DeserializeObject<List<EventSectionDefinition>>(File.ReadAllText(eventsJsonPath))
            ?? new List<EventSectionDefinition>()) {
        }

        public EventScheduleEngine(IReadOnlyList<EventSectionDefinition> sections)
            : this(sections, refreshOnLoad: true) {
        }

        internal EventScheduleEngine(IReadOnlyList<EventSectionDefinition> sections, bool refreshOnLoad) {
            _sections = sections.ToList();
            BuildTrackedEvents();
            if (refreshOnLoad) {
                Refresh(DateTime.UtcNow);
            }
        }

        private void BuildTrackedEvents() {
            _events.Clear();

            foreach (var section in _sections.Where(s => s.Active)) {
                foreach (var segment in section.Segments.Where(s => !string.IsNullOrWhiteSpace(s.Name))) {
                    var category = EventCategoryMapper.Resolve(section.Name, segment.Name);

                    _events.Add(new TrackedEvent {
                        Key = $"{section.Id}:{segment.Id}",
                        SectionId = section.Id,
                        SegmentId = segment.Id,
                        ExpansionCategory = section.Category,
                        Category = category,
                        SectionName = section.Name,
                        SegmentName = segment.Name,
                        ChatLink = segment.ChatLink ?? string.Empty,
                        IconUrl = segment.Icon ?? string.Empty,
                        WikiUrl = WikiUrlHelper.ResolveWikiUrl(section, segment),
                        AccentColor = EventRewardsFormatter.ParseAccentColor(segment.BackgroundRgb),
                        Rewards = segment.Rewards,
                        RecommendedLfg = segment.RecommendedLfg
                    });
                }
            }
        }

        public event EventHandler? DailyReset;

        public void Refresh(DateTime utcNow) {
            var utcMidnight = utcNow.Date;
            if (_lastUtcMidnight != utcMidnight) {
                foreach (var tracked in _events) {
                    tracked.ResetAlerts();
                }

                if (_lastUtcMidnight != DateTime.MinValue) {
                    DailyReset?.Invoke(this, EventArgs.Empty);
                }

                _lastUtcMidnight = utcMidnight;
            }

            var horizon = utcNow.AddHours(26);

            foreach (var section in _sections.Where(s => s.Active)) {
                var occurrences = EnumerateOccurrences(section, utcNow.AddHours(-2), horizon).ToList();

                foreach (var tracked in _events.Where(e => e.SectionId == section.Id)) {
                    var nextIndex = occurrences.FindIndex(o => o.SegmentId == tracked.SegmentId && o.EndUtc > utcNow);
                    if (nextIndex >= 0) {
                        var next = occurrences[nextIndex];
                        tracked.NextStartUtc = next.StartUtc;
                        tracked.NextEndUtc = next.EndUtc;
                        tracked.IsActive = next.StartUtc <= utcNow && utcNow < next.EndUtc;
                    } else {
                        tracked.IsActive = false;
                    }
                }
            }
        }

        public IEnumerable<TrackedEvent> GetUpcoming(double hours) {
            var utcNow = DateTime.UtcNow;
            return _events
                .Where(e => e.NextStartUtc >= utcNow && e.NextStartUtc <= utcNow.AddHours(hours))
                .OrderBy(e => e.NextStartUtc);
        }

        private static IEnumerable<ScheduleOccurrence> EnumerateOccurrences(
            EventSectionDefinition section,
            DateTime fromUtc,
            DateTime toUtc) {

            if (section.Sequences == null) {
                yield break;
            }

            var partial = section.Sequences.Partial ?? new List<SequenceStep>();
            var pattern = section.Sequences.Pattern ?? new List<SequenceStep>();

            var dayStart = DateTime.SpecifyKind(fromUtc.Date, DateTimeKind.Utc);
            var lastDay = DateTime.SpecifyKind(toUtc.Date, DateTimeKind.Utc);

            for (var day = dayStart; day <= lastDay; day = day.AddDays(1)) {
                var minuteCursor = 0d;
                var partialIndex = 0;
                var patternIndex = 0;
                var usingPartial = partial.Count > 0;

                while (minuteCursor < 1440) {
                    SequenceStep step;

                    if (usingPartial && partialIndex < partial.Count) {
                        step = partial[partialIndex++];
                        if (partialIndex >= partial.Count) {
                            usingPartial = false;
                        }
                    } else if (pattern.Count > 0) {
                        step = pattern[patternIndex % pattern.Count];
                        patternIndex++;
                    } else if (partialIndex < partial.Count) {
                        step = partial[partialIndex++];
                    } else {
                        break;
                    }

                    var start = day.AddMinutes(minuteCursor);
                    var end = start.AddMinutes(step.DurationMinutes);

                    if (end > fromUtc && start < toUtc) {
                        yield return new ScheduleOccurrence(step.SegmentRef, start, end);
                    }

                    minuteCursor += step.DurationMinutes;

                    if (pattern.Count == 0 && partialIndex >= partial.Count) {
                        break;
                    }
                }
            }
        }

        private readonly struct ScheduleOccurrence {
            public ScheduleOccurrence(int segmentId, DateTime startUtc, DateTime endUtc) {
                SegmentId = segmentId;
                StartUtc = startUtc;
                EndUtc = endUtc;
            }

            public int SegmentId { get; }
            public DateTime StartUtc { get; }
            public DateTime EndUtc { get; }
        }
    }

}