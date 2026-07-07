using System;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;

namespace Ghost.Gw2EventTracker.UI {

    internal class SnoozeNotificationCard : NotificationCard {

        public const int ActionButtonWidth = 62;
        public const int ActionButtonHeight = 26;

        private readonly string _eventKey;
        private readonly StandardButton _snoozeButton;
        private readonly Panel _actionStrip;

        public SnoozeNotificationCard(
            string eventKey,
            string title,
            AsyncTexture2D icon,
            string message,
            string? tooltip = null)
            : base(title, icon, message, tooltip, reserveActionStrip: true) {
            _eventKey = eventKey;

            _actionStrip = new Panel {
                Parent = this,
                Width = ActionStripWidth,
                Height = CardHeight,
                BackgroundColor = new Color(0, 0, 0, 140),
                ZIndex = 5
            };

            _snoozeButton = new StandardButton {
                Text = "Snooze",
                Width = ActionButtonWidth,
                Height = ActionButtonHeight,
                BasicTooltipText = "Snooze alerts until daily reset.",
                Parent = this,
                ZIndex = 10
            };
            _snoozeButton.Click += (_, __) => {
                EventTrackerModule.Instance.SnoozeEvent(_eventKey);
                OnSnoozed();
            };

            LayoutActionStrip();
            Resized += (_, __) => LayoutActionStrip();
        }

        protected virtual void OnSnoozed() {
        }

        protected override CaptureType CapturesInput() => CaptureType.Mouse;

        protected bool IsOverSnoozeButton() {
            if (!_snoozeButton.Visible) {
                return false;
            }

            var area = new Rectangle(_snoozeButton.Left, _snoozeButton.Top, _snoozeButton.Width, _snoozeButton.Height);
            return area.Contains(RelativeMousePosition);
        }

        protected virtual void OnCardClicked() {
        }

        protected override void OnLeftMouseButtonReleased(MouseEventArgs e) {
            if (IsOverSnoozeButton()) {
                return;
            }

            OnCardClicked();
        }

        private void LayoutActionStrip() {
            var left = Width - ActionStripWidth;

            _actionStrip.Top = 0;
            _actionStrip.Left = left;
            _actionStrip.Height = Height;

            _snoozeButton.Top = Math.Max(0, (Height - ActionButtonHeight) / 2);
            _snoozeButton.Left = left + (ActionStripWidth - ActionButtonWidth) / 2;
        }
    }

}
