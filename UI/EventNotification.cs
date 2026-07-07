using System;
using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;

namespace Ghost.Gw2EventTracker.UI {

    internal sealed class EventNotification : NotificationCard {

        public const int NotificationWidth = NotificationCard.CardWidth;
        public const int NotificationHeight = NotificationCard.CardHeight;
        public const int NotificationStackSpacing = NotificationCard.CardSpacing;

        private static int _visibleNotifications;

        private readonly string _eventKey;

        private EventNotification(
            string eventKey,
            string title,
            AsyncTexture2D icon,
            string message,
            string waypoint)
            : base(title, icon, message, "Left click: copy waypoint. Right click: snooze until daily reset.") {
            _eventKey = eventKey;
            Opacity = 0f;
            Location = new Point(
                EventTrackerModule.Instance.NotificationPosition.X,
                EventTrackerModule.Instance.NotificationPosition.Y + (NotificationHeight + NotificationStackSpacing) * _visibleNotifications);

            _visibleNotifications++;

            RightMouseButtonReleased += (_, __) => {
                EventTrackerModule.Instance.SnoozeEvent(_eventKey);
                Dispose();
            };
            LeftMouseButtonReleased += (_, __) => {
                if (!string.IsNullOrWhiteSpace(waypoint)) {
                    CopyWaypoint(waypoint);
                }

                Dispose();
            };
        }

        protected override CaptureType CapturesInput() => CaptureType.Mouse;

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

        private static async void CopyWaypoint(string waypoint) {
            try {
                await ClipboardUtil.WindowsClipboardService.SetTextAsync(waypoint).ConfigureAwait(false);
                ScreenNotification.ShowNotification("Copied waypoint to clipboard!", duration: 2);
            } catch (Exception) {
                ScreenNotification.ShowNotification("Failed to copy waypoint to clipboard. Try again.", ScreenNotification.NotificationType.Red, duration: 2);
            }
        }
    }

}
