# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.3] - 2026-07-08

### Fixed

- Daily Progress tab now refreshes boss and map chest completions without disabling the module.
- Bypass Blish HUD's shared Gw2Sharp memory cache for account daily-progress API calls via `NullCacheMethod`.
- Cross-midnight API fetches schedule a follow-up refresh instead of leaving stale data.

### Changed

- Daily progress API polling aligned to the GW2 API ~5 minute cache (was 60 seconds).
- Uncached daily-progress fetches use one `Gw2Client` per refresh and share Blish's `TokenComplianceMiddleware` for rate-limit compliance.

### Notes

- Install package: `ghost.gw2eventtracker_0.1.3.bhm`

## [0.1.2] - 2026-07-07

### Added

- Dedicated snooze buttons on event alerts, event cards, and the Next Up overlay.
- `SnoozeNotificationCard` for one-tap snooze from alert popups.

### Changed

- Notification titles and messages truncate with ellipsis instead of overflowing the card.
- Shared event card actions (icon, wiki, waypoint) consolidated into `EventCardUiHelper`.
- Schedule loading and module unload hardened for stability with Module Citizen.
- Blish HUD dependency pinned to `~1.3.0`.

### Removed

- Unused schedule engine and trackable rewards catalog APIs.

### Notes

- Install package: `ghost.gw2eventtracker_0.1.2.bhm`

## [0.1.1] - 2026-07-07

### Changed

- Module namespace is now `ghost.gw2eventtracker` (was `gw2eventtracker`) for Blish Module Repo / SSRD requirements.
- C# namespaces renamed to `Ghost.Gw2EventTracker.*`.
- GW2 UI icons load through Blish HUD `DatAssetCache` instead of Blish HUD bundled `ref.dat` textures.

### Removed

- Redundant bundled dat icon PNGs (`605021`, `605019`, `1466345`) from module ref; these are fetched at runtime by asset ID.

### Notes

- Updating from `gw2eventtracker` resets saved module settings (snoozes, profiles, overlay position, etc.) because Blish HUD keys settings by namespace.
- Install package: `ghost.gw2eventtracker_0.1.1.bhm`

## [0.1.0] - 2026-07-06

### Added

- Initial public release of GW2 Event Tracker for Blish HUD.
- Event schedules from embedded giovazz89 timer data with optional remote updates.
- Lead-time and event-started alerts, snooze until daily reset, and Next Up overlay.
- Daily Progress tab with GW2 API completion tracking for trackable daily rewards.
- Event profiles (built-in presets and custom save/load), map-aware filtering, and event recommendations.
- Improved daily completion tracking and Daily Progress UI.
