# GW2 Event Tracker

Standalone [Blish HUD](https://blishhud.com/) module for Guild Wars 2. Tracks meta and world boss schedules, alerts you before and when events start, and shows which daily rewards you have already claimed via the GW2 API.

**Repository:** [github.com/petersolymosi/gw2-event-tracker](https://github.com/petersolymosi/gw2-event-tracker)

## Features

### Schedules & alerts

- **Event schedules** — computed locally from timer data (`ref/Data/events.json`, [giovazz89 format](https://github.com/giovazz89/gw2-api-event-timers)), with local patches for renamed metas and chain-triggered dailies missing upstream
- **Remote schedule** — optional fetch of the latest `events.json` from GitHub on module reload
- **Lead-time alerts** — configurable minutes-before-start (default 15, 10, 5)
- **Event-started alerts** — notify when a watched event becomes active
- **Snooze** — use the snooze button on a notification or event card to silence alerts until daily reset
- **Smart filtering** — suppress alerts for events whose daily reward is already claimed

### Event Tracker tab

- Category filters (World Bosses, Meta Event, Day-Night Cycle, **Active Now**, **On Current Map**, etc.)
- Search, sort (Next Up / Alphabetical), watch toggles, wiki links, waypoint copy
- **Card accent stripe** — left color bar from schedule `bg` data
- **Achievement tooltips** — reward details on hover (cached from GW2 API)
- **Map highlighting** — gold stripe on events matching your current map (MumbleLink)
- **“What should I do next?”** — scored recommendation in the summary bar
- **Event profiles** — one-click presets (Meta Daily Runner, World Boss Train, Map Focus, Quiet Tracker) plus **custom profiles** you can save from current settings
- **Track Everything** — default profile on first load; watches all events with alerts and overlay on

### Daily Progress tab

- Trackable daily rewards with completion counts and reset countdown
- Tracker-style cards grouped by category

### Overlay & settings

- **Next Up overlay** — draggable on-screen list of upcoming watched events
- **Category watch defaults** — auto-watch entire categories
- **Show incomplete only** — hide events with claimed daily rewards
- Notification and overlay position movers

## Requirements

- [Blish HUD](https://blishhud.com/) 1.0+ (tested on 1.3.0)
- .NET SDK with .NET Framework 4.7.2 targeting pack (for building)
- Optional: GW2 API key with `account` and `progression` permissions (completion tracking and alert suppression)

Without an API key, schedules, alerts, map filtering, and the recommender still work; completion icons and reward-based filtering are limited.

## Build

```powershell
cd Gw2EventTracker
dotnet restore
dotnet build Gw2EventTracker.csproj -c Debug -p:Platform=x64
```

Output: `bin\x64\Debug\Gw2EventTracker.bhm`

Release build:

```powershell
dotnet build Gw2EventTracker.csproj -c Release -p:Platform=x64
```

Run unit tests:

```powershell
dotnet test Gw2EventTracker.Tests\Gw2EventTracker.Tests.csproj -c Debug -p:Platform=x64
```

## Install

1. Copy the `.bhm` file to your Blish HUD modules folder:
   - `%USERPROFILE%\Documents\Guild Wars 2\addons\blishhud\modules\`
   - Or `%OneDrive%\Documents\Guild Wars 2\addons\blishhud\modules\` if OneDrive syncs Documents
2. Enable **GW2 Event Tracker** in Blish HUD -> Settings -> Modules
3. For daily completion tracking: add a GW2 API key in Blish HUD (account + progression scopes), then enable both API permissions for this module under Settings -> Modules

After enabling, use the **Event Tracker** and **Daily Progress** tabs in the Blish HUD sidebar.

## Deploy (local dev)

`deploy.ps1` builds Debug, copies to the modules folder as `ghost.gw2eventtracker_0.1.2.bhm`, and optionally restarts Blish HUD:

```powershell
$env:GW2_BLISH_EXE = "C:\Path\To\Blish HUD.exe"
.\deploy.ps1
```

If `GW2_BLISH_EXE` is not set, the module is still copied; start Blish HUD manually.

## Usage

### Event Tracker

| Control | Action |
|---|---|
| Profile dropdown | Apply a preset (filters, watch defaults, overlay/alerts) |
| Category menu | Filter by type, **Active Now**, or **On Current Map** |
| Search | Filter events by name |
| Sort | Next Up or Alphabetical |
| Wiki / waypoint buttons | Open wiki page or copy chat link |
| `[x]` / `[ ]` | Daily reward claimed / not yet claimed |
| Eye button | Toggle notifications for that event |
| Snooze button | Snooze alerts until daily reset |

The summary bar shows daily reward progress, reset countdown, and a **What should I do next?** suggestion.

### Settings

Event profiles, custom profile save/delete, map highlighting, overlay options, alert lead times, category watch defaults, remote schedule toggle, and notification positions.

## Debug in Visual Studio

Set **Start external program** to your `Blish HUD.exe` with arguments:

```
--debug --module "C:\path\to\Gw2EventTracker\bin\x64\Debug\Gw2EventTracker.bhm"
```

## Project layout

```
Gw2EventTracker/
├── EventTrackerModule.cs           Module entry point
├── Gw2EventTracker.Tests/          xUnit tests (schedule, snooze, rewards)
├── ModuleData.cs                   Embedded resource helpers
├── Models/                         Schedule and progress models
├── Services/
│   ├── EventScheduleEngine.cs      UTC rotation schedule logic
│   ├── EventScheduleLoader.cs      Embedded + remote events.json
│   ├── AccountProgressService.cs   GW2 API polling
│   ├── AlertService.cs             Lead-time and started notifications
│   ├── NextEventRecommender.cs     “What should I do next?” scoring
│   ├── EventMapMapper.cs           Section/segment 뿯↽ map ID (MumbleLink)
│   ├── EventProfileStore.cs        Built-in and custom event profiles
│   ├── ModuleSettings.cs           Persisted module settings
│   └── …                           Rewards, wiki, categories, achievements
├── UI/
│   ├── EventTrackerView.cs         Main event list
│   ├── DailyProgressView.cs        Daily reward progress tab
│   ├── NextUpOverlayWidget.cs      On-screen upcoming events
│   ├── SettingsView.cs             Module settings
│   └── …                           Notifications, movers, cards
├── ref/Data/
│   ├── events.json                 Schedule definitions
│   └── trackable-rewards.json      World boss / map chest API mappings
├── manifest.json                   Blish HUD module manifest
└── deploy.ps1                      Build + deploy + restart script
```

Schedule JSON is embedded in the DLL at build time; `ref/Data/` is also copied into the `.bhm` package.

## Data sources

- Schedule format: [gw2-api-event-timers](https://github.com/giovazz89/gw2-api-event-timers) (`events.json`)
- Remote schedule URL: giovazz89 GitHub (toggle in settings; embedded fallback if unavailable)
- Wiki links resolved from each segment's `link` field (fallback: segment or section name)
- Map IDs for MumbleLink filtering: static lookup table in `EventMapMapper.cs`

## Blish Module Repo

Install from Blish HUD without building locally:

1. Open **Settings -> Module Repo**
2. Search for **GW2 Event Tracker**
3. Install and enable the module

The module is listed in the official [Blish HUD Module Repo](https://blishhud.com/docs/modules/ssrd/overview) after SSRD publication. For manual install, build Release and copy the `.bhm` to your modules folder (see **Install** above), or use `deploy.ps1` for local development.

## License

MIT — see [LICENSE](LICENSE).
