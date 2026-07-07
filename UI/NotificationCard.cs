using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ghost.Gw2EventTracker.UI {

    internal class NotificationCard : Container {

        public const int CardWidth = 280;
        public const int CardHeight = 64;
        public const int CardSpacing = 15;
        protected const int ActionStripWidth = 72;

        private readonly AsyncTexture2D _icon;
        private readonly Texture2D _textureBackground;
        private Rectangle _layoutIconBounds;

        public NotificationCard(
            string title,
            AsyncTexture2D icon,
            string message,
            string? tooltip = null,
            bool reserveActionStrip = false) {
            _icon = icon;
            _textureBackground = EventTrackerModule.Instance.ContentsManager.GetTexture("textures/ns-button.png");

            Size = new Point(CardWidth, CardHeight);
            BasicTooltipText = tooltip ?? string.Empty;

            var actionReserve = reserveActionStrip ? ActionStripWidth : 0;
            var textWidth = Width - CardHeight - 10 - actionReserve;

            var wrappedTitle = DrawUtil.WrapText(Content.DefaultFont14, title, textWidth);
            _ = new Label {
                Parent = this,
                Location = new Point(CardHeight + 10, 5),
                Size = new Point(textWidth, Height / 2),
                Font = Content.DefaultFont14,
                BasicTooltipText = BasicTooltipText,
                Text = wrappedTitle
            };

            var wrappedMessage = DrawUtil.WrapText(Content.DefaultFont14, message, textWidth);
            _ = new Label {
                Parent = this,
                Location = new Point(CardHeight + 10, Height / 2),
                Size = new Point(textWidth, Height / 2),
                Font = Content.DefaultFont14,
                BasicTooltipText = BasicTooltipText,
                Text = wrappedMessage
            };
        }

        public override void RecalculateLayout() {
            const int iconSize = 52;
            _layoutIconBounds = new Rectangle(
                CardHeight / 2 - iconSize / 2,
                CardHeight / 2 - iconSize / 2,
                iconSize,
                iconSize);
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds) {
            spriteBatch.DrawOnCtrl(this, _textureBackground, bounds, Color.White * 0.85f);

            if (_icon != null) {
                spriteBatch.DrawOnCtrl(this, _icon, _layoutIconBounds);
            }
        }
    }

}
