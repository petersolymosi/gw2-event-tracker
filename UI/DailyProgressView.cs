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
        private const int HeaderRowHeight = 34;
        private const int CardPadding = 8;
        private const int ColumnGap = 24;
        private const int ScrollbarInset = 24;

        private readonly AccountProgressService _progressService;
        private readonly EventScheduleEngine _scheduleEngine;

        private readonly Label _summaryLabel;
        private readonly Checkbox _hideCompletedCheckbox;
        private readonly Panel _progressTrack;
        private readonly Panel _progressFill;
        private readonly FlowPanel _worldBossColumn;
        private readonly FlowPanel _metaColumn;
        private Label? _apiMessageLabel;

        private bool _hideCompleted;
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
                AutoSizeWidth = true,
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

            _worldBossColumn = CreateColumn();
            _metaColumn = CreateColumn();
            _worldBossColumn.Parent = this;
            _metaColumn.Parent = this;

            Resized += (_, __) => {
                LayoutColumns();
                if (_progressService.HasApiAccess) {
                    RefreshList();
                }
            };

            LayoutColumns();
            LayoutHeaderRow();
            RefreshList();
        }

        private static FlowPanel CreateColumn() {
            return new FlowPanel {
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                ControlPadding = new Vector2(0, CardPadding),
                CanScroll = true
            };
        }

        private void LayoutHeaderRow() {
            _hideCompletedCheckbox.Location = new Point(
                Math.Max(Pad, Width - _hideCompletedCheckbox.Width - Pad),
                10);

            _progressTrack.Width = Math.Max(100, Width - Pad * 2);
            _progressTrack.Location = new Point(Pad, HeaderRowHeight);
        }

        private void LayoutColumns() {
            LayoutHeaderRow();

            var columnTop = HeaderRowHeight + 28;
            var columnHeight = Math.Max(100, Height - columnTop - Pad);
            _columnWidth = Math.Max(280, (Width - Pad * 2 - ColumnGap) / 2);
            _cardWidth = Math.Max(240, _columnWidth - ScrollbarInset);

            _worldBossColumn.Location = new Point(Pad, columnTop);
            _worldBossColumn.Size = new Point(_columnWidth, columnHeight);

            _metaColumn.Location = new Point(Pad + _columnWidth + ColumnGap, columnTop);
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

            if (!_progressService.HasApiAccess) {
                if (_apiMessageLabel == null) {
                    _apiMessageLabel = new Label {
                        Text = "Add a GW2 API key with account and progression permissions to track daily rewards.",
                        Width = Width - Pad * 2,
                        Height = 44,
                        WrapText = true,
                        Parent = this
                    };
                }

                _apiMessageLabel.Width = Width - Pad * 2;
                _apiMessageLabel.Location = new Point(Pad, HeaderRowHeight + 40);
                _apiMessageLabel.Visible = true;
                return;
            }

            if (_apiMessageLabel != null) {
                _apiMessageLabel.Visible = false;
            }

            LayoutColumns();

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

        private void UpdateSummary() {
            var reset = DailyResetHelper.FormatCountdown(DateTime.UtcNow);

            if (!_progressService.HasApiAccess) {
                _summaryLabel.Text = $"Daily reset in {reset} (UTC midnight)";
                UpdateProgressBar(0, 1);
                return;
            }

            var summary = _progressService.GetSummary();
            _summaryLabel.Text = $"Today: {summary.Completed}/{summary.Trackable} claimed  |  Reset in {reset}";
            UpdateProgressBar(summary.Completed, summary.Trackable);
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
