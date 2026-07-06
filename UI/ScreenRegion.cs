using Blish_HUD.Settings;
using Microsoft.Xna.Framework;

namespace Gw2EventTracker.UI {

    internal sealed class ScreenRegion {

        private Rectangle? _bounds;

        public Rectangle Bounds => _bounds ?? (_bounds = new Rectangle(Location, Size)).Value;

        private readonly SettingEntry<Point> _location;
        private readonly SettingEntry<Point> _size;

        public string RegionName { get; }

        public Point Location {
            get => _location.Value;
            set {
                _location.Value = value;
                _bounds = null;
            }
        }

        public Point Size {
            get => _size.Value;
            set {
                _size.Value = value;
                _bounds = null;
            }
        }

        public ScreenRegion(string regionName, SettingEntry<Point> location, SettingEntry<Point> size) {
            RegionName = regionName;
            _location = location;
            _size = size;
        }
    }

}