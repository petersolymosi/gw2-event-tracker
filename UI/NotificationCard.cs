using System;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;

namespace Ghost.Gw2EventTracker.UI {

    internal class NotificationCard : Container {

        public const int CardWidth = 280;
        public const int CardHeight = 64;
        public const int CardSpacing = 15;
        protected const int ActionStripWidth = 72;

        private const int TitleMaxLines = 2;
        private const int MessageMaxLines = 1;

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

            var displayTitle = TruncateWrappedText(Content.DefaultFont14, title, textWidth, TitleMaxLines);
            _ = new Label {
                Parent = this,
                Location = new Point(CardHeight + 10, 5),
                Size = new Point(textWidth, Height / 2),
                Font = Content.DefaultFont14,
                BasicTooltipText = title,
                Text = displayTitle
            };

            var displayMessage = TruncateWrappedText(Content.DefaultFont14, message, textWidth, MessageMaxLines);
            _ = new Label {
                Parent = this,
                Location = new Point(CardHeight + 10, Height / 2),
                Size = new Point(textWidth, Height / 2),
                Font = Content.DefaultFont14,
                BasicTooltipText = BasicTooltipText,
                Text = displayMessage
            };
        }

        private static string TruncateWrappedText(BitmapFont font, string text, int maxWidth, int maxLines) {
            if (string.IsNullOrEmpty(text)) {
                return text;
            }

            var wrapped = DrawUtil.WrapText(font, text, maxWidth);
            var lines = wrapped.Split('\n');
            if (lines.Length <= maxLines) {
                return wrapped;
            }

            var visible = lines.Take(maxLines).ToArray();
            visible[maxLines - 1] = Ellipsize(font, visible[maxLines - 1], maxWidth);
            return string.Join("\n", visible);
        }

        private static string Ellipsize(BitmapFont font, string line, int maxWidth) {
            const string suffix = "...";
            if (font.MeasureString(line + suffix).Width <= maxWidth) {
                return line + suffix;
            }

            while (line.Length > 0 && font.MeasureString(line + suffix).Width > maxWidth) {
                line = line.Substring(0, line.Length - 1);
            }

            return line + suffix;
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
