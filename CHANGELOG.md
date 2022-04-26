# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]

### Changed
- Gaps are now set to null if one of the cars is finished and they are on the same lap. If they are not on the same lap, lap difference is shown. Gap is finalized once both cars have crossed the finish line. Previously showed last available gap, which resulted in some weird jumps in the gaps.
- Non relative gaps are now shown only after the race start.

### Fixed
- Possible name conflicts between dynamic leaderboards.
- Wrong results after the race finish:
    - Wrong order of cars whose lap count differed by one.
    - Wrong order if cars finished very close.
    - `Position.Overall` property showed wrong positions.
    - Wrong class gaps if some cars were lapped and some were not.
    - Gaps not updating if focused car was changed after finish.
- Wrong sign of relative gaps if the focused car was class/overall leader.
- Wrong order if joined race session in the middle of session or opened SimHub in the middle of race session.


## [1.0.0] - 2022-04-19
- Initial public release

[Unreleased]: https://github.com/kaiusl/KLPlugins.Leaderboard/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases/tag/v1.0.0
