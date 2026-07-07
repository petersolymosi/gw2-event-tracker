using System;
using System.Diagnostics;
using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Ghost.Gw2EventTracker.Models;
using Ghost.Gw2EventTracker.Services;

namespace Ghost.Gw2EventTracker.UI {

    internal static class EventCardUiHelper {

        public static AsyncTexture2D ResolveIcon(TrackedEvent? tracked) {
            if (tracked == null
                || tracked.Category == "Day-Night Cycle"
                || string.IsNullOrWhiteSpace(tracked.IconUrl)) {
                return ModuleTextures.DefaultEventIcon;
            }

            return GameService.Content.GetRenderServiceTexture(tracked.IconUrl);
        }

        public static void OpenWiki(string wikiUrl) {
            if (!Uri.TryCreate(wikiUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) {
                return;
            }

            try {
                Process.Start(wikiUrl);
            } catch (Exception) {
                ScreenNotification.ShowNotification("Failed to open wiki page.", ScreenNotification.NotificationType.Red, duration: 2);
            }
        }

        public static async void CopyWaypoint(string chatLink) {
            try {
                await ClipboardUtil.WindowsClipboardService.SetTextAsync(chatLink).ConfigureAwait(false);
                ScreenNotification.ShowNotification("Copied waypoint to clipboard!", duration: 2);
            } catch (Exception) {
                ScreenNotification.ShowNotification("Failed to copy waypoint to clipboard. Try again.", ScreenNotification.NotificationType.Red, duration: 2);
            }
        }
    }

}
