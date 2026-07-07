using System;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Content;
using Ghost.Gw2EventTracker.Models;
using Ghost.Gw2EventTracker.UI;
using Humanizer;
using Humanizer.Localisation;

namespace Ghost.Gw2EventTracker.Services {

    public sealed class AlertService {

        private static readonly Logger Logger = Logger.GetLogger<AlertService>();

        private readonly EventScheduleEngine _scheduleEngine;
        private readonly AccountProgressService _progressService;
        private readonly ModuleSettings _settings;

        private double _elapsedSeconds;

        public AlertService(
            EventScheduleEngine scheduleEngine,
            AccountProgressService progressService,
            ModuleSettings settings) {
            _scheduleEngine = scheduleEngine;
            _progressService = progressService;
            _settings = settings;
        }

        public void Update(double elapsedSeconds) {
            _elapsedSeconds += elapsedSeconds;

            if (_elapsedSeconds < _settings.AlertCheckIntervalSeconds) {
                return;
            }

            _elapsedSeconds = 0;

            if (!_settings.NotificationsEnabled) {
                return;
            }

            var utcNow = DateTime.UtcNow;
            var leadTimes = _settings.AlertLeadTimes;

            foreach (var tracked in _scheduleEngine.Events.Where(e => e.IsWatched)) {
                CheckStartedAlert(tracked);

                if (tracked.NextStartUtc <= utcNow) {
                    continue;
                }

                var minutesUntil = (tracked.NextStartUtc - utcNow).TotalMinutes;

                foreach (var lead in leadTimes) {
                    if (minutesUntil > lead || tracked.AlertedLeadTimes.Contains(lead)) {
                        continue;
                    }

                    if (ShouldSuppressAlert(tracked)) {
                        tracked.AlertedLeadTimes.Add(lead);
                        Logger.Debug("Skipping alert for completed event {Event}.", tracked.DisplayLabel);
                        continue;
                    }

                    if (_settings.IsSnoozed(tracked.Key)) {
                        tracked.AlertedLeadTimes.Add(lead);
                        continue;
                    }

                    ShowUpcomingAlert(tracked, minutesUntil, lead);
                    tracked.AlertedLeadTimes.Add(lead);
                }
            }
        }

        public void OnScheduleRefreshed() {
            if (!_settings.NotificationsEnabled || !_settings.EventStartedAlerts) {
                return;
            }

            foreach (var tracked in _scheduleEngine.Events.Where(e => e.IsWatched && e.IsActive)) {
                CheckStartedAlert(tracked);
            }
        }

        private void CheckStartedAlert(TrackedEvent tracked) {
            if (!tracked.IsActive || tracked.AlertedStarted) {
                return;
            }

            if (ShouldSuppressAlert(tracked) || _settings.IsSnoozed(tracked.Key)) {
                tracked.AlertedStarted = true;
                return;
            }

            ShowStartedAlert(tracked);
            tracked.AlertedStarted = true;
        }

        private bool ShouldSuppressAlert(TrackedEvent tracked) {
            return _settings.SuppressCompletedAlerts
                && _progressService.HasApiAccess
                && _progressService.IsRewardCompleted(tracked);
        }

        private void ShowUpcomingAlert(TrackedEvent tracked, double minutesUntil, int leadMinutes) {
            var message = $"Starts in {TimeSpan.FromMinutes(Math.Max(0, minutesUntil)).Humanize(maxUnit: TimeUnit.Hour, minUnit: TimeUnit.Minute, precision: 2, collectionSeparator: null)}";
            ShowNotification(tracked, message);
            Logger.Info("Alert for {Event} ({Lead} min lead).", tracked.DisplayLabel, leadMinutes);
        }

        private void ShowStartedAlert(TrackedEvent tracked) {
            var message = tracked.Category == "Day-Night Cycle" && tracked.NextEndUtc != default
                ? $"Active now — ends {tracked.NextEndUtc.ToLocalTime():t}"
                : "Active now";

            ShowNotification(tracked, message);
            Logger.Info("Started alert for {Event}.", tracked.DisplayLabel);
        }

        private static void ShowNotification(TrackedEvent tracked, string message) {
            AsyncTexture2D icon = string.IsNullOrWhiteSpace(tracked.IconUrl)
                ? ModuleTextures.DefaultEventIcon
                : GameService.Content.GetRenderServiceTexture(tracked.IconUrl);

            EventNotification.ShowNotification(
                tracked.DisplayLabel,
                icon,
                message,
                EventTrackerModule.Instance.NotificationDuration,
                tracked.ChatLink,
                tracked.Key);
        }
    }

}
