# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]

### Added

- Support for other games, not only ACC.
    - Currently tested with AC, ACC, AMS2, RF2 and R3E.
    - Other games should work as the plugin relies on the data provided by SimHub
      but they haven't been tested if any special handling is needed.
- New car specific properties:
    - `Driver.Category.Color`
    - `Driver.Category.TextColor`
    - `Car.Class.TextColor`
    - `Car.Number.Text` - string representation of number. Allows to differentiate leading zeros in the number.
- New general properties:
    - `Color.Class.<class>.Text`
    - `Color.DriverCategory.<category>.Text`
    - `Session.NumberOfClasses`
    - `Session.NumberOfCups`
- New config UI to edit car settings (name, manufacturer and class).
- Option to read AC car info directly from AC files.
- AC root location in the settings.
- Configuration options to set controls for "NextLeaderboard" and "PreviousLeaderboards"
  actions directly in this plugins settings menu.
- Option to remove class and/or cup leaderboards from the rotation in case
  there is only a single class or cup in the session.

### Changed

- Rewrite internals using the data provided by SimHub. 
  Our own connection to ACC is completely removed (at least for now).
- Deprecate `Driver.CategoryColor` property in favour of `Driver.Category.Color` 
  which matches the names of other similar properties.
- New config UI to edit class, team cup and driver category colors.
- Lap interpolators are built on need to basis to avoid unnecessary waste of 
  resources.
- Automatically generate lap data for gap calculation.
- Automatically generate spline position offsets.

### Removed

- Pregenerated lap data files since they are not needed anymore.
- Option to set data update interval. The data update rate is defined by SimHub.
  For dashboards it's recommended to set min. update intervals in the dash studio.
- Toggle option to include ST21 and CHL (ACC) classes in GT2 class.
  The same thing can now be achieved by changing the class of car directly.

### Fixed

- Possible `NullReferenceException` while disposing `Values`.
- Don't overwrite log files when a game in changed within SimHub.
- Added missing ACC driver nationalities
- Possible missing leaderboard configuration when importing configuration from 
  older plugin version.
- Position flickering if two cars have same finishing time.
- On track gap had different signs when using naive and interpolator based methods.
- Overly bright separators in settings menu.

## [1.4.5] - 2024-05-07

- Support Ford Mustang GT3 which was added in ACC v1.10.2.

## [1.4.4] - 2024-04-30

### Fixed

- Missing properties after a fresh install on first SimHub start.

## [1.4.3] - 2024-04-16

### Fixed

- N24 wrong track id
- Improve detection of in/out laps
- First hotlap wrongly shown as out lap (in/out lap flags were sometimes not reset after actual out lap)
- N24 short lap didn't start a new flying lap in non-race sessions

## [1.4.2] - 2024-04-04

### Added

- New docs site [
Dynamic Leaderboards Plugin for SimHub](https://kaiusl.github.io/KLPlugins.DynLeaderboards/stable/).

### Changed

- Updated links to point to new docs site.

## [1.4.1] - 2024-04-02

### Fixed

- Workaround for missing N24 track data

### Added

- N24 lap data for gap calculation

## [1.4.0] - 2024-01-25

### Fixed

- Relative leaderboards showing wrong car for one update after a position change.

### Added

- New Cup, RelativeCup, PartialRelativeCup leaderboard types ([#24], [#25]). 
  These are effectively Class, RelativeClass and PartialRelativeClass except they
  also filter by the focused car's cup category (Pro/Overall, Pro-Am, Am, National).
- Few new properties related to new leaderboard types:
  - `Laps.Best.Delta.ToCupBest`
  - `Laps.Best.Delta.ToCupLeaderBest`
  - `Laps.Best.Delta.ToAheadInCupBest`
  - `Laps.Last.Delta.ToCupBest`
  - `Laps.Last.Delta.ToCupLeaderBest`
  - `Laps.Last.Delta.ToAheadInCupBest`
  - `Laps.Last.Delta.ToCupLeaderLast`
  - `Laps.Last.Delta.ToAheadInCupLast`
  - `Gap.ToCupLeader`
  - `Gap.ToAhead.Cup`
  - `Position.Cup`
  - `Position.Cup.Start`
  - `IsCupBestLapCar`
  - Dynamic properties have been updated to include cup specific properties.
- Support GT2 class and 6 new cars in it.
- An option to include Lamborghini Huracan ST EVO2 and Ferrari 488 Challenge Evo in GT2 class.
  The option is under the General settings tab.

[#24]: https://github.com/kaiusl/KLPlugins.DynLeaderboards/discussions/24
[#25]: https://github.com/kaiusl/KLPlugins.DynLeaderboards/pull/25

## [1.3.3] - 2023-10-24

### Fixed

- `Laps.Best.Sx` was showing last lap sectors not the best lap sectors ([#23])
- `Position.x.Start` not updating after race restart ([#23])

[#23]: https://github.com/kaiusl/KLPlugins.DynLeaderboards/issues/23

## [1.3.2] - 2023-05-26

### Added

- Support McLaren 720S GT3 Evo which was added in ACC v1.9.3.

  This also means that ACC v1.9.3 needs at least the v1.3.2 version of this plugin to fully function properly.

## [1.3.1] - 2023-05-15

### Added

- An option to disable given dynamic leaderboard from being calculated.

### Fixed

- Typo which caused broadcasting.json not to be read properly.

### Improvements

- Cache more properties instead of calculating them from scratch on each update.
- Remove bunch of unnecessary logging.
- Overhaul broadcasting network protocol for cleaner code and potentially slighly better performance.
- Upgrade to C# 9.0 to make use of some nice new syntax and clean up code.
- Enable strict null checking and fix potential issues identified by it.
- Apply code suggestions.

## [1.3.0] - 2023-04-20

### Added

- Support for new cars and track from 2023 GT World Challenge Pack ([#19]).
- Lap data for gap calculation for Valencia. Thank you [Mtrade] for running the laps and generating the needed data ([#16]).

### Fixed

- An error if the plugin is first launched while ACC is not the selected game ([#15]).
- A `NullReferenceException` if broadcasting server sends a `RealtimeUpdate` before entry list.
- If a new car is added to ACC but not yet to our enums, it's class is Unknown, which causes 
  a `IndexOutofBoundsException` in `CarClassArray` if we try to access class positions or lap data.
  Should avoid [#19] in the future until we get an update out.
- Use a naive distance and speed based gap calculation in case precise lap data is not available.
  (Should only happen if ACC get's a new track and we haven't pushed an update yet.) ([#20]).
- An exception if an BroadcastingEvent is sent before anything else. Can happen if one joins midrace and someone just had a crash or finished a lap.
- An exception in gap calcualtion if some cars do not have received their `RealtimeCarUpdate`. 
  Can happen on the first `RealtimeUpdate` where none of the car have received their `RealtimeCarUpdate` or if someone joins midrace.

[Mtrade]: https://github.com/Mtrade
[#16]: https://github.com/kaiusl/KLPlugins.DynLeaderboards/issues/16
[#15]: https://github.com/kaiusl/KLPlugins.DynLeaderboards/issues/15
[#19]: https://github.com/kaiusl/KLPlugins.DynLeaderboards/issues/19
[#20]: https://github.com/kaiusl/KLPlugins.DynLeaderboards/issues/20

## [1.2.3] - 2023-03-28

### Dash changes

- Moved leaderboard into an widget to fix a repetition bug after changing SimHub dashboard with `GraphicalDashPlugin.Cycle(Next/Previous)Dash`.
  See [SHWotever/SimHub#1262] for more details on the issue.

[SHWotever/SimHub#1262]: https://github.com/SHWotever/SimHub/issues/1262

## [1.2.2] - 2023-01-05

### Added
- New leaderboard type `RelativeOnTrackWoPit` which is the same as `RelativeOnTrack` but 
excludes all the cars in the pitlane which are not on the same lap as the focused car. ([#10], [#12])

### Fixed
- Jump to pits after forgetting to press "DRIVE" disabled total gap and `RelativeOnTrackLapDiff` calculations until the car finished it's the first lap on some tracks. 

### Dash changes
- Enabled new `RelativeOnTrackWoPit` leaderboard by default. 

[#10]: https://github.com/kaiusl/KLPlugins.DynLeaderboards/issues/10
[#12]: https://github.com/kaiusl/KLPlugins.DynLeaderboards/pull/12

## [1.2.1] - 2022-12-27

### Added
- 6 new properties for every car: ([#9])
    - `Laps.Current.IsValid`
    - `Laps.Current.IsOutLap`
    - `Laps.Current.IsInLap`
    - `Laps.Last.IsValid`
    - `Laps.Last.IsOutLap`
    - `Laps.Last.IsInLap`

[#9]: https://github.com/kaiusl/KLPlugins.DynLeaderboards/issues/9

## [1.2.0] - 2022-07-01

### Added
- Automatic migration between setting versions.
- American DLC tracks and necessary data for gap calculation.
- Properties `Position.Dynamic` and `Position.Dynamic.Start` that show overall positions in overall leaderboards and class position in class position.

### Improvements
- Leaderboard configurations are now saved separately to allow simpler configuration ([#7]).

### Fixed
- Disallowed non letter or digit characters from leaderboard names to fix possible issues where weird characters would essentially become variables inside SimHub.
- PartialRelativeClass showed N-1 position ahead instead of N position as set in settings ([#6]).

### Dash changes
- Add dash for SimHub v7.x and v8.x versions.
- Dash uses new dynamic position properties. 

[#6]: https://github.com/kaiusl/KLPlugins.DynLeaderboards/issues/6
[#7]: https://github.com/kaiusl/KLPlugins.DynLeaderboards/issues/7

## [1.1.1] - 2022-05-03

### Improvements
- Don't need to restart SimHub if you only change leaderboard rotation.

### Fixed
- Leaderboard rotation was not saved and thus on SimHub start the rotation was always default.
- Team cup category text color reset button set the color to background color.
- Number of partial relative *class* leaderboard relative positions actually changed number of partial relative *overall* leaderboard relative positions.
- Plugin and SimHub crashed if you added new leaderboard to the rotation and selected it without restarting SimHub. With this fix you actually don't have to restart SimHub if you change leaderboard rotation.

## [1.1.0] - 2022-04-30

### Added
- `RelativeOnTrackLapDiff` property to each car. It shows if the car is lap ahead or behind or on the same lap relative to the focused car on track. -1: lap behind, 0 same lap, 1 lap ahead.

### Changed
- Gaps are now set to `null` (instead of showing previous gap) if one of the cars is finished and they are on the same lap. If they are not on the same lap, lap difference is shown. Gap is finalized once both cars have crossed the finish line.
- Non relative gaps are now `null` before the car has crossed the line for the first time at the race start.
- Gap to itself is now `null` instead of `0`.

### Improvements
- Use broadcasting events + shared memory data to detect if car has finished. Should be more robust.
- Use spline position offset to sync update of number of completed laps and reset of spline position instead of separately calculating laps by spline position. 
- Try to detect missing/late broadcast events and try to fix the order if it happens at finish.

### Removed
- Hide everything from other assemblies for now. Normally this would definitely be a breaking change but as they were never really accessible from the plugin and inside SimHub everything is still exactly as before, I'll consider it not breaking. It is planned to expose parts of this plugin to be directly used from other plugins in future versions.

### Fixed
- Possible name conflicts between dynamic leaderboards.
- `Position.Overall` property showed wrong positions after the race finish.
- Orders
    - Occasional jumping of positions at lap finish.
    - Wrong order after the finish if cars lap count differed by one and some car's hadn't finished yet.
    - Wrong order if joined race session in the middle of session or opened SimHub in the middle of race session.
    - (Partial)RelativeClass positions were not calculated if the wanted position was outside the number of class positions shown. That is say you wanted to show 20 class positions, then cars lower than 20th in class were not shown in (Partial)RelativeClass leaderboards.
    - Car was showed falesly in the first position if it jumped to the pits, which would be correct if it could leave the pits immediately but as it cannot, more approptiate is to show it as last on it's lap.
- Gaps
    - Occasional gap jumping at lap finish.
    - Wrong sign of relative gaps in race sessions if the focused car was class/overall leader.
    - Gap to cars ahead in race sessions was calculated with 'this' car's position at previous update.
    - Wrong class gaps after race finish if some cars were lapped and some were not.
    - Gaps not updating if focused car was changed after finish.
- Maybe fixed longer data update intervals causing missed/false finishes and missed laps when crossing the line close to the clock reaching zero or just after. I think they are still technically possible but I haven't managed to see one, so it's definitely better than before.

## [1.0.0] - 2022-04-19
- Initial public release

[Unreleased]: https://github.com/kaiusl/KLPlugins.Leaderboard/compare/v1.4.5...HEAD
[1.4.5]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases/tag/v1.4.5
[1.4.4]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases/tag/v1.4.4
[1.4.3]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases/tag/v1.4.3
[1.4.2]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases/tag/v1.4.2
[1.4.1]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases/tag/v1.4.1
[1.4.0]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases/tag/v1.4.0
[1.3.3]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases/tag/v1.3.3
[1.3.2]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases/tag/v1.3.2
[1.3.1]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases/tag/v1.3.1
[1.3.0]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases/tag/v1.3.0
[1.2.3]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases/tag/v1.2.3
[1.2.2]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases/tag/v1.2.2
[1.2.1]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases/tag/v1.2.1
[1.2.0]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases/tag/v1.2.0
[1.1.1]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases/tag/v1.1.1
[1.1.0]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases/tag/v1.1.0
[1.0.0]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases/tag/v1.0.0
