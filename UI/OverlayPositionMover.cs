using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gw2EventTracker.UI {

    internal sealed class OverlayPositionMover : Control {

        private const int HandleSize = 40;

        private readonly SpriteBatchParameters _clearDrawParameters;
        private readonly ScreenRegion[] _screenRegions;
        private ScreenRegion? _activeScreenRegion;
        private Point _grabPosition = Point.Zero;
        private readonly Texture2D _handleTexture;

        public OverlayPositionMover(params ScreenRegion[] screenPositions) : this((IEnumerable<ScreenRegion>)screenPositions) {
        }

        public OverlayPositionMover(IEnumerable<ScreenRegion> screenPositions) {
            ZIndex = int.MaxValue - 10;
            _clearDrawParameters = new SpriteBatchParameters(SpriteSortMode.Deferred, BlendState.Opaque);
            _screenRegions = screenPositions.ToArray();
            _handleTexture = EventTrackerModule.Instance.ContentsManager.GetTexture("textures/handle.png");
        }

        protected override void OnLeftMouseButtonPressed(MouseEventArgs e) {
            if (_activeScreenRegion == null) {
                return;
            }

            _grabPosition = RelativeMousePosition;
        }

        public override void DoUpdate(GameTime gameTime) {
            base.DoUpdate(gameTime);

            if (GameService.Input.Keyboard.KeysDown.Contains(Microsoft.Xna.Framework.Input.Keys.Escape)) {
                Dispose();
            }
        }

        protected override void OnLeftMouseButtonReleased(MouseEventArgs e) {
            _grabPosition = Point.Zero;
        }

        protected override void OnMouseMoved(MouseEventArgs e) {
            if (_grabPosition != Point.Zero && _activeScreenRegion != null) {
                var lastPos = _grabPosition;
                _grabPosition = RelativeMousePosition;
                _activeScreenRegion.Location += _grabPosition - lastPos;
            } else {
                _activeScreenRegion = _screenRegions.FirstOrDefault(region => region.Bounds.Contains(RelativeMousePosition));
            }
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds) {
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, bounds, Color.Black * 0.8f);
            spriteBatch.End();
            spriteBatch.Begin(_clearDrawParameters);

            foreach (var region in _screenRegions) {
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.TransparentPixel, region.Bounds, Color.Transparent);
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteBatchParameters);

            foreach (var region in _screenRegions) {
                if (region == _activeScreenRegion) {
                    spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(region.Location, region.Size), Color.White * 0.5f);
                }

                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, region.Bounds, Color.Black * 0.55f);
                spriteBatch.DrawStringOnCtrl(
                    this,
                    "Next Up",
                    GameService.Content.DefaultFont14,
                    new Rectangle(region.Bounds.X + 8, region.Bounds.Y + 4, region.Bounds.Width - 16, 20),
                    Color.White,
                    false,
                    HorizontalAlignment.Left,
                    VerticalAlignment.Top);

                var handleOrigin = new Vector2(HandleSize / 2f, HandleSize / 2f);
                spriteBatch.DrawOnCtrl(this, _handleTexture, new Rectangle(region.Bounds.Left, region.Bounds.Top, HandleSize, HandleSize), _handleTexture.Bounds, Color.White * 0.6f);
                spriteBatch.DrawOnCtrl(this, _handleTexture, new Rectangle(region.Bounds.Right - HandleSize / 2, region.Bounds.Top + HandleSize / 2, HandleSize, HandleSize), _handleTexture.Bounds, Color.White * 0.6f, MathHelper.PiOver2, handleOrigin);
                spriteBatch.DrawOnCtrl(this, _handleTexture, new Rectangle(region.Bounds.Left + HandleSize / 2, region.Bounds.Bottom - HandleSize / 2, HandleSize, HandleSize), _handleTexture.Bounds, Color.White * 0.6f, MathHelper.PiOver2 * 3, handleOrigin);
                spriteBatch.DrawOnCtrl(this, _handleTexture, new Rectangle(region.Bounds.Right - HandleSize / 2, region.Bounds.Bottom - HandleSize / 2, HandleSize, HandleSize), _handleTexture.Bounds, Color.White * 0.6f, MathHelper.Pi, handleOrigin);
            }

            spriteBatch.DrawStringOnCtrl(
                this,
                "Drag to set where the Next Up overlay appears. Press ESC to close.",
                GameService.Content.DefaultFont32,
                bounds,
                Color.White,
                false,
                HorizontalAlignment.Center);
        }
    }

}
