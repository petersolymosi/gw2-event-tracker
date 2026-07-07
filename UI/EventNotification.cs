using System;
using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;

namespace Ghost.Gw2EventTracker.UI {

    internal sealed class EventNotification : SnoozeNotificationCard {

        public const int NotificationWidth = NotificationCard.CardWidth;
        public const int NotificationHeight = NotificationCard.CardHeight;
        public const int NotificationStackSpacing = NotificationCard.CardSpacing;

        private static int _visibleNotifications;

        private readonly string _waypoint;

        private EventNotification(
            string eventKey,
            string title,
            AsyncTexture2D icon,
            string message,
            string waypoint)
            : base(
                eventKey,
                title,
                icon,
                message,
                "Left click: copy waypoint. Snooze button: silence until daily reset.") {
            _waypoint = waypoint;
            Opacity = 0f;
            Location = new Point(
                EventTrackerModule.Instance.NotificationPosition.X,
                EventTrackerModule.Instance.NotificationPosition.Y + (NotificationHeight + NotificationStackSpacing) * _visibleNotifications);

            _visibleNotifications++;
        }

        protected override void OnSnoozed() => Dispose();

        protected override void OnCardClicked() {
            if (!string.IsNullOrWhiteSpace(_waypoint)) {
                EventCardUiHelper.CopyWaypoint(_waypoint);
            }

            Dispose();
        }

        private void Show(float duration) {
            if (EventTrackerModule.Instance.ChimeEnabled) {
                Content.PlaySoundEffectByName("audio/color-change");
            }

            Animation.Tweener
                .Tween(this, new { Opacity = 1f }, 0.2f)
                .Repeat(1)
                .RepeatDelay(duration)
                .Reflect()
                .OnComplete(Dispose);
        }

        public static void ShowNotification(
            string title,
            AsyncTexture2D icon,
            string message,
            float duration,
            string waypoint,
            string eventKey) {
            var notification = new EventNotification(eventKey, title, icon, message, waypoint) {
                Parent = Graphics.SpriteScreen
            };

            notification.Show(duration);
        }

        protected override void DisposeControl() {
            _visibleNotifications--;
            base.DisposeControl();
        }
    }

}
