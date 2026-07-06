using System;
using System.Collections.Generic;
using System.Linq;
using Gw2EventTracker.Models;
using Gw2EventTracker.Services;
using Xunit;

namespace Gw2EventTracker.Tests {

    public sealed class EventScheduleEngineTests {

        [Fact]
        public void Constructor_DoesNotFireDailyResetOnFirstLoad() {
            var fired = false;
            var engine = CreateEngine(refreshOnLoad: false);
            engine.DailyReset += (_, __) => fired = true;

            engine.Refresh(new DateTime(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc));

            Assert.False(fired);
        }

        [Fact]
        public void Refresh_FiresDailyResetWhenUtcDayChanges() {
            var fireCount = 0;
            var engine = CreateEngine(refreshOnLoad: false);
            engine.DailyReset += (_, __) => fireCount++;

            var day = new DateTime(2026, 7, 6, 23, 59, 0, DateTimeKind.Utc);
            engine.Refresh(day);
            engine.Refresh(day.AddDays(1).AddSeconds(1));

            Assert.Equal(1, fireCount);
        }

        [Fact]
        public void Refresh_ResetsAlertStateOnUtcDayChange() {
            var engine = CreateEngine(refreshOnLoad: false);
            var tracked = engine.Events.First();

            tracked.AlertedLeadTimes.Add(15);
            tracked.AlertedStarted = true;

            var day = new DateTime(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);
            engine.Refresh(day);
            engine.Refresh(day.AddDays(1).AddSeconds(1));

            Assert.Empty(tracked.AlertedLeadTimes);
            Assert.False(tracked.AlertedStarted);
        }

        [Fact]
        public void Refresh_ComputesActiveEventWithinPattern() {
            var engine = CreateEngine(refreshOnLoad: false);
            var utcNow = new DateTime(2026, 7, 6, 1, 30, 0, DateTimeKind.Utc);

            engine.Refresh(utcNow);

            var active = engine.Events.First(e => e.SegmentId == 1);
            Assert.True(active.IsActive);
            Assert.Equal(new DateTime(2026, 7, 6, 1, 0, 0, DateTimeKind.Utc), active.NextStartUtc);
            Assert.Equal(new DateTime(2026, 7, 6, 2, 0, 0, DateTimeKind.Utc), active.NextEndUtc);
        }

        private static EventScheduleEngine CreateEngine(bool refreshOnLoad = false) {
            var sections = new List<EventSectionDefinition> {
                new EventSectionDefinition {
                    Id = 1,
                    Active = true,
                    Name = "Test Map",
                    Category = "Core",
                    Segments = new List<EventSegmentDefinition> {
                        new EventSegmentDefinition { Id = 0, Name = "Downtime" },
                        new EventSegmentDefinition { Id = 1, Name = "Meta Phase" }
                    },
                    Sequences = new EventSequences {
                        Pattern = new List<SequenceStep> {
                            new SequenceStep { SegmentRef = 0, DurationMinutes = 60 },
                            new SequenceStep { SegmentRef = 1, DurationMinutes = 60 }
                        }
                    }
                }
            };

            return new EventScheduleEngine(sections, refreshOnLoad);
        }
    }

}
