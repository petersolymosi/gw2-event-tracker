using System;
using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Controls;
using Gw2EventTracker.Models;
using Gw2EventTracker.Services;
using Microsoft.Xna.Framework;

namespace Gw2EventTracker.UI {

    public sealed class DailyProgressView : Panel {

        private const int Pad = 16;
        private const int CardPadding = 8;
        private const int ColumnGap = 24;
        private const int ScrollbarInset = 24;
        private const int MinColumnWidth = 180;
        private const int RowGap = 8;

        private readonly AccountProgressService _progressService;
        private readonly EventScheduleEngine _scheduleEngine;

        private readonly Label _summaryLabel;
        private readonly Checkbox _hideCompletedCheckbox;
        private readonly Panel _progressTrack;
        private readonly Panel _progressFill;
        private readonly FlowPanel _worldBossColumn;
        private readonly FlowPanel _metaColumn;
        private readonly StandardButton _refreshButton;
        private Label? _apiMessageLabel;

        private bool _hideCompleted;
        private int _columnTop;
        private int _columnWidth;
        private int _cardWidth;

        public DailyProgressView(
            Rectangle bounds,
            AccountProgressService progressService,
            EventScheduleEngine scheduleEngine) {
            _progressService = progressService;
            _scheduleEngine = scheduleEngine;

            Size = bounds.Size;
            CanScroll = false;

            _summaryLabel = new Label {
                AutoSizeWidth = false,
                WrapText = true,
                Parent = this,
                Location = new Point(Pad, 12)
            };

            _hideCompletedCheckbox = new Checkbox {
                Text = "Hide completed",
                Checked = false,
                Parent = this
            };
            _hideCompletedCheckbox.CheckedChanged += (_, e) => {
                _hideCompleted = e.Checked;
                RefreshList();
            };

            _progressTrack = new Panel {
                Height = 10,
                BackgroundColor = new Color(40, 40, 40, 200),
                Parent = this
            };

            _progressFill = new Panel {
                Height = 10,
                BackgroundColor = new Color(80, 170, 90, 220),
                Parent = _progressTrack
            };

            _refreshButton = new StandardButton {
                Text = "Refresh now",
                Parent = this
            };
            _refreshButton.Click += async (_, __) => {
                _refreshButton.Enabled = false;
                _refreshButton.Text = "Refreshing...";
                try {
                    await EventTrackerModule.Instance.ForceRefreshProgressAsync().ConfigureAwait(false);
                    GameService.Overlay.QueueMainThreadUpdate(_ => RefreshList());
                } finally {
                    GameService.Overlay.QueueMainThreadUpdate(_ => {
                        _refreshButton.Enabled = true;
                        _refreshButton.Text = "Refresh now";
                    });
                }
            };

            _worldBossColumn = CreateColumn();
            _metaColumn = CreateColumn();
            _worldBossColumn.Parent = this;
            _metaColumn.Parent = this;

            Resized += (_, __) => {
                LayoutChrome();
                if (_progressService.HasApiAccess) {
                    RefreshList();
                }
            };

            LayoutChrome();
            RefreshList();
        }

        private static FlowPanel CreateColumn() {
            return new FlowPanel {
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                ControlPadding = new Vector2(0, CardPadding),
                CanScroll = true
            };
        }

        private void LayoutChrome() {
            var contentWidth = Math.Max(200, Width - Pad * 2);

            _hideCompletedCheckbox.Location = new Point(
                Math.Max(Pad, Width - _hideCompletedCheckbox.Width - Pad),
                12);

            var summaryWidth = Math.Max(
                120,
                contentWidth - (_hideCompletedCheckbox.Visible ? _hideCompletedCheckbox.Width + RowGap : 0));
            _summaryLabel.Width = summaryWidth;
            _summaryLabel.Height = GetSummaryHeight();
            _summaryLabel.Location = new Point(Pad, 12);

            var y = 12 + _summaryLabel.Height + RowGap;

            if (_progressTrack.Visible) {
                _progressTrack.Width = contentWidth;
                _progressTrack.Location = new Point(Pad, y);
                y = _progressTrack.Bottom + RowGap;
            }

            _refreshButton.Location = new Point(Pad, y);
            y = _refreshButton.Bottom + RowGap + 4;
            _columnTop = y;

            LayoutColumns();
        }

        private void LayoutColumns() {
            var columnHeight = Math.Max(100, Height - _columnTop - Pad);
            var available = Math.Max(MinColumnWidth * 2 + ColumnGap, Width - Pad * 2);
            _columnWidth = Math.Max(MinColumnWidth, (available - ColumnGap) / 2);
            _cardWidth = Math.Max(140, _columnWidth - ScrollbarInset);

            _worldBossColumn.Location = new Point(Pad, _columnTop);
            _worldBossColumn.Size = new Point(_columnWidth, columnHeight);

            _metaColumn.Location = new Point(Pad + _columnWidth + ColumnGap, _columnTop);
            _metaColumn.Size = new Point(_columnWidth, columnHeight);
        }

        public void RefreshList() {
            UpdateSummary();

            _worldBossColumn.ClearChildren();
            _metaColumn.ClearChildren();

            _worldBossColumn.Visible = _progressService.HasApiAccess;
            _metaColumn.Visible = _progressService.HasApiAccess;
            _progressTrack.Visible = _progressService.HasApiAccess;
            _hideCompletedCheckbox.Visible = _progressService.HasApiAccess;
            _refreshButton.Visible = true;

            if (!_progressService.HasApiAccess) {
                if (_apiMessageLabel == null) {
                    _apiMessageLabel = new Label {
                        Width = Width - Pad * 2,
                        Height = 88,
                        WrapText = true,
                        Parent = this
                    };
                }

                _apiMessageLabel.Text = _progressService.StatusMessage;
                _apiMessageLabel.Width = Math.Max(200, Width - Pad * 2);
                LayoutChrome();
                _apiMessageLabel.Location = new Point(Pad, _columnTop);
                _apiMessageLabel.Visible = true;
                return;
            }

            if (_apiMessageLabel != null) {
                _apiMessageLabel.Visible = false;
            }

            LayoutChrome();

            var statuses = _progressService.GetTrackableRewardStatuses();
            var worldBosses = FilterAndSort(statuses, "worldboss");
            var mapChests = FilterAndSort(statuses, "mapchest");

            var worldBossTotal = statuses.Count(s => s.TrackType.Equals("worldboss", StringComparison.OrdinalIgnoreCase));
            var worldBossCompleted = statuses.Count(s =>
                s.TrackType.Equals("worldboss", StringComparison.OrdinalIgnoreCase) && s.IsCompleted);
            var mapChestTotal = statuses.Count(s => s.TrackType.Equals("mapchest", StringComparison.OrdinalIgnoreCase));
            var mapChestCompleted = statuses.Count(s =>
                s.TrackType.Equals("mapchest", StringComparison.OrdinalIgnoreCase) && s.IsCompleted);

            PopulateColumn(
                _worldBossColumn,
                "World Boss Dailies",
                worldBosses,
                worldBossCompleted,
                worldBossTotal);

            PopulateColumn(
                _metaColumn,
                "Meta Hero's Choice Chests",
                mapChests,
                mapChestCompleted,
                mapChestTotal);
        }

        private List<TrackableRewardStatus> FilterAndSort(
            IReadOnlyList<TrackableRewardStatus> statuses,
            string trackType) {
            return statuses
                .Where(s => s.TrackType.Equals(trackType, StringComparison.OrdinalIgnoreCase))
                .Where(s => !_hideCompleted || !s.IsCompleted)
                .OrderBy(s => s.IsCompleted)
                .ThenBy(s => s.DisplayName)
                .ToList();
        }

        private void PopulateColumn(
            FlowPanel column,
            string title,
            IReadOnlyList<TrackableRewardStatus> items,
            int completedCount,
            int totalCount) {
            var remaining = totalCount - completedCount;
            var claimedLabel = _hideCompleted
                ? $"{remaining} remaining"
                : $"{completedCount}/{totalCount} claimed";

            _ = new Panel {
                Title = $"{title} ({claimedLabel})",
                ShowBorder = true,
                Width = _cardWidth,
                Height = 40,
                Parent = column
            };

            if (items.Count == 0) {
                _ = new Label {
                    Text = _hideCompleted ? "All claimed today." : "No rewards in this category.",
                    Width = _cardWidth,
                    Height = 24,
                    Parent = column
                };
                return;
            }

            foreach (var status in items) {
                var tracked = FindTrackedEvent(status.DisplayName);
                var card = DailyRewardCard.Create(
                    column,
                    tracked,
                    status.DisplayName,
                    status.IsCompleted,
                    _cardWidth);

                GameService.Overlay.QueueMainThreadUpdate(_ => card.LayoutActions());
            }
        }

        private TrackedEvent? FindTrackedEvent(string scheduleName) {
            var direct = _scheduleEngine.Events.FirstOrDefault(e =>
                e.SegmentName.Equals(scheduleName, StringComparison.OrdinalIgnoreCase));

            if (direct != null) {
                return direct;
            }

            foreach (var tracked in _scheduleEngine.Events) {
                if (_progressService.MatchesTrackableName(tracked.SegmentName, scheduleName)) {
                    return tracked;
                }
            }

            return null;
        }

        private int GetSummaryHeight() =>
            _progressService.HasApiAccess ? 40 : 22;

        private void UpdateSummary() {
            var reset = DailyResetHelper.FormatCountdown(DateTime.UtcNow);

            if (!_progressService.HasApiAccess) {
                _summaryLabel.Text = $"Daily reset in {reset} (UTC midnight)";
                UpdateProgressBar(0, 1);
                return;
            }

            var summary = _progressService.GetSummary();
            var syncAge = _progressService.LastSuccessfulRefreshUtc is DateTime lastSync
                ? $"Last synced {HumanizeSyncAge(DateTime.UtcNow - lastSync)}"
                : "Not synced yet";

            _summaryLabel.Text =
                $"Today: {summary.Completed}/{summary.Trackable} claimed  |  Reset in {reset}{Environment.NewLine}{syncAge}";
            UpdateProgressBar(summary.Completed, summary.Trackable);
        }

        private static string HumanizeSyncAge(TimeSpan age) {
            if (age.TotalSeconds < 60) {
                return $"{Math.Max(1, (int)age.TotalSeconds)}s ago";
            }

            if (age.TotalMinutes < 60) {
                return $"{(int)age.TotalMinutes}m ago";
            }

            return $"{(int)age.TotalHours}h ago";
        }

        private void UpdateProgressBar(int completed, int total) {
            var max = Math.Max(1, total);
            var trackWidth = Math.Max(100, Width - Pad * 2);
            _progressTrack.Width = trackWidth;

            var fillWidth = (int)Math.Round(trackWidth * (completed / (double)max));
            _progressFill.Width = Math.Max(0, Math.Min(trackWidth, fillWidth));
        }
    }

}
