using System;
using System.Diagnostics;
using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Gw2EventTracker.Models;
using Microsoft.Xna.Framework;

namespace Gw2EventTracker.UI {

    internal sealed class DailyRewardCard {

        public DetailsButton Button { get; }
        public Label CompletionLabel { get; }
        public GlowButton? WikiButton { get; }
        public GlowButton? WaypointButton { get; }

        private DailyRewardCard(
            DetailsButton button,
            Label completionLabel,
            GlowButton? wikiButton,
            GlowButton? waypointButton) {
            Button = button;
            CompletionLabel = completionLabel;
            WikiButton = wikiButton;
            WaypointButton = waypointButton;
        }

        public static DailyRewardCard Create(
            Container parent,
            TrackedEvent? tracked,
            string displayName,
            bool isCompleted,
            int width) {
            var card = new DetailsButton {
                Parent = parent,
                Width = width,
                Text = displayName,
                IconDetails = string.Empty,
                IconSize = DetailsIconSize.Small,
                ShowVignette = false,
                HighlightType = DetailsHighlightType.LightHighlight,
                ShowToggleButton = false,
                BasicTooltipText = isCompleted ? "Daily reward claimed" : "Daily reward not yet claimed"
            };

            card.Icon = ResolveIcon(tracked);
            if (isCompleted) {
                card.Opacity = 0.72f;
            }

            GlowButton? wikiButton = null;
            GlowButton? waypointButton = null;

            if (tracked != null && !string.IsNullOrWhiteSpace(tracked.WikiUrl)) {
                wikiButton = new GlowButton {
                    Icon = GameService.Content.GetTexture("102530"),
                    ActiveIcon = GameService.Content.GetTexture("glow-wiki"),
                    BasicTooltipText = "Read about this event on the wiki.",
                    Parent = card,
                    GlowColor = Color.White * 0.1f
                };
                wikiButton.Click += (_, __) => OpenWiki(tracked.WikiUrl);
            }

            if (tracked != null && !string.IsNullOrWhiteSpace(tracked.ChatLink)) {
                waypointButton = new GlowButton {
                    Icon = GameService.Content.GetTexture("waypoint"),
                    ActiveIcon = GameService.Content.GetTexture("glow-waypoint"),
                    BasicTooltipText = $"Nearby waypoint: {tracked.ChatLink}",
                    Parent = card,
                    GlowColor = Color.White * 0.1f
                };
                waypointButton.Click += (_, __) => CopyWaypoint(tracked.ChatLink);
            }

            var completionLabel = new Label {
                Size = new Point(24, card.ContentRegion.Height),
                Text = isCompleted ? "[x]" : "[ ]",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle,
                BasicTooltipText = card.BasicTooltipText,
                Parent = card
            };

            var rewardCard = new DailyRewardCard(card, completionLabel, wikiButton, waypointButton);
            rewardCard.LayoutActions();

            card.Resized += (_, __) => rewardCard.LayoutActions();
            if (wikiButton != null) {
                wikiButton.Resized += (_, __) => rewardCard.LayoutActions();
            }

            if (waypointButton != null) {
                waypointButton.Resized += (_, __) => rewardCard.LayoutActions();
            }

            return rewardCard;
        }

        public void LayoutActions() {
            var buttonHeight = WaypointButton?.Height ?? WikiButton?.Height ?? 32;
            var actionTop = Math.Max(0, (Button.ContentRegion.Height - buttonHeight) / 2);
            var rightEdge = Button.Width - 8;

            if (WaypointButton != null) {
                WaypointButton.Top = actionTop;
                WaypointButton.Left = rightEdge - WaypointButton.Width;
                rightEdge = WaypointButton.Left - 4;
            }

            if (WikiButton != null) {
                WikiButton.Top = actionTop;
                WikiButton.Left = rightEdge - WikiButton.Width;
                rightEdge = WikiButton.Left - 4;
            }

            CompletionLabel.Top = actionTop;
            CompletionLabel.Left = Math.Max(64, rightEdge - CompletionLabel.Width);
            CompletionLabel.Height = Button.ContentRegion.Height;
        }

        public static AsyncTexture2D ResolveIcon(TrackedEvent? tracked) {
            if (tracked == null
                || tracked.Category == "Day-Night Cycle"
                || string.IsNullOrWhiteSpace(tracked.IconUrl)) {
                return new AsyncTexture2D(GameService.Content.GetTexture("102377"));
            }

            return GameService.Content.GetRenderServiceTexture(tracked.IconUrl);
        }

        private static void OpenWiki(string wikiUrl) {
            if (!Uri.TryCreate(wikiUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) {
                return;
            }

            try {
                Process.Start(wikiUrl);
            } catch (Exception) {
                ScreenNotification.ShowNotification("Failed to open wiki page.", ScreenNotification.NotificationType.Red, duration: 2);
            }
        }

        private static async void CopyWaypoint(string chatLink) {
            try {
                await ClipboardUtil.WindowsClipboardService.SetTextAsync(chatLink).ConfigureAwait(false);
                ScreenNotification.ShowNotification("Copied waypoint to clipboard!", duration: 2);
            } catch (Exception) {
                ScreenNotification.ShowNotification("Failed to copy waypoint to clipboard. Try again.", ScreenNotification.NotificationType.Red, duration: 2);
            }
        }
    }

}
