# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]

### Added
- Automatic migration between setting versions

### Improvements
- Leaderboard configurations are now saved separately ([issue #7](https://github.com/kaiusl/KLPlugins.DynLeaderboards/issues/7)).

### Fixed
- Removed non letter or digit characters from leaderboard names to fix possible issues.
- PartialRelativeClass showed N-1 position ahead instead of N position as set in settings ([issue #6](https://github.com/kaiusl/KLPlugins.DynLeaderboards/issues/6)).

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

[Unreleased]: https://github.com/kaiusl/KLPlugins.Leaderboard/compare/v1.1.1...HEAD
[1.1.1]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases/tag/v1.1.1
[1.1.0]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases/tag/v1.1.0
[1.0.0]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases/tag/v1.0.0
