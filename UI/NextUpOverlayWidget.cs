using System;
using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Ghost.Gw2EventTracker.Models;
using Ghost.Gw2EventTracker.Services;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.Xna.Framework;

namespace Ghost.Gw2EventTracker.UI {

    public sealed class NextUpOverlayWidget : Container {

        public static int WidgetWidth => NotificationCard.CardWidth;

        private readonly EventScheduleEngine _scheduleEngine;
        private readonly ModuleSettings _settings;

        public NextUpOverlayWidget(EventScheduleEngine scheduleEngine, ModuleSettings settings) {
            _scheduleEngine = scheduleEngine;
            _settings = settings;

            ZIndex = 5;
            Location = settings.OverlayPosition;
            Width = WidgetWidth;

            Refresh();
        }

        protected override CaptureType CapturesInput() => CaptureType.Mouse;

        public static int CalculateHeight(int rowCount) {
            var rows = Math.Max(1, rowCount);
            return rows * NotificationCard.CardHeight + Math.Max(0, rows - 1) * NotificationCard.CardSpacing;
        }

        public void Refresh() {
            if (!_settings.OverlayEnabled || !ShouldShowOverlay()) {
                Visible = false;
                return;
            }

            Visible = true;
            Opacity = _settings.OverlayOpacity;
            Location = _settings.OverlayPosition;

            ClearChildren();

            var utcNow = DateTime.UtcNow;
            var maxEvents = _settings.OverlayMaxEvents;
            var entries = new List<(TrackedEvent Event, bool IsActive)>();

            foreach (var tracked in _scheduleEngine.Events
                .Where(e => e.IsWatched && e.IsActive && !_settings.IsSnoozed(e.Key))
                .OrderBy(e => e.NextEndUtc)
                .Take(maxEvents)) {
                entries.Add((tracked, isActive: true));
            }

            var remaining = maxEvents - entries.Count;
            if (remaining > 0) {
                foreach (var tracked in _scheduleEngine.Events
                    .Where(e => e.IsWatched && !e.IsActive && e.NextStartUtc > utcNow && !_settings.IsSnoozed(e.Key))
                    .OrderBy(e => e.NextStartUtc)
                    .Take(remaining)) {
                    entries.Add((tracked, isActive: false));
                }
            }

            if (entries.Count == 0) {
                _ = new NotificationCard(
                    "Next Up",
                    EventCardUiHelper.ResolveIcon(null),
                    "No watched events upcoming") {
                    Parent = this
                };
                Size = new Point(WidgetWidth, NotificationCard.CardHeight);
                return;
            }

            var y = 0;
            foreach (var (tracked, isActive) in entries) {
                var (title, message) = FormatCardText(tracked, isActive);
                var card = new SnoozeNotificationCard(
                    tracked.Key,
                    title,
                    EventCardUiHelper.ResolveIcon(tracked),
                    message,
                    BuildTooltip(tracked, isActive)) {
                    Location = new Point(0, y),
                    Parent = this
                };

                y += NotificationCard.CardHeight + NotificationCard.CardSpacing;
            }

            Size = new Point(WidgetWidth, Math.Max(NotificationCard.CardHeight, y - NotificationCard.CardSpacing));
        }

        public void UpdateVisibility() {
            if (!_settings.OverlayEnabled) {
                Visible = false;
                return;
            }

            Visible = ShouldShowOverlay();
        }

        private static bool ShouldShowOverlay() {
            return GameService.GameIntegration.Gw2Instance.IsInGame
                && !GameService.Gw2Mumble.UI.IsMapOpen;
        }

        private static (string Title, string Message) FormatCardText(TrackedEvent tracked, bool isActive) {
            if (isActive) {
                if (tracked.Category == "Day-Night Cycle" && tracked.NextEndUtc != default) {
                    var endsIn = tracked.NextEndUtc.ToLocalTime() - DateTime.Now;
                    if (endsIn.TotalMinutes > 0) {
                        var endsLabel = endsIn.Humanize(
                            maxUnit: TimeUnit.Hour,
                            minUnit: TimeUnit.Minute,
                            precision: 1,
                            collectionSeparator: null);
                        return (tracked.DisplayLabel, $"Active now — ends in {endsLabel}");
                    }
                }

                return (tracked.DisplayLabel, "Active now");
            }

            var startsIn = tracked.NextStartUtc.ToLocalTime() - DateTime.Now;
            var timeLabel = startsIn.TotalMinutes > 0
                ? startsIn.Humanize(maxUnit: TimeUnit.Hour, minUnit: TimeUnit.Minute, precision: 1, collectionSeparator: null)
                : tracked.NextStartUtc.ToLocalTime().ToShortTimeString();

            return (tracked.DisplayLabel, $"Starts in {timeLabel}");
        }

        private static string BuildTooltip(TrackedEvent tracked, bool isActive) {
            if (isActive) {
                return tracked.Category;
            }

            return $"{tracked.Category}{Environment.NewLine}Starts {tracked.NextStartUtc.ToLocalTime():t}";
        }
    }

}
