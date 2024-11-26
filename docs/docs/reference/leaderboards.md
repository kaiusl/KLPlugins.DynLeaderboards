??? info "Symbols used"

    {{ versionIcon }} 1.0.0
    : This marks the version a given leaderboard type was added to the plugin.

There are several different leaderboard types to use within dynamic leaderboards.

## Overall leaderboards

`N` top positions. There are two types:

- `Overall`{{ sinceVersion("1.0.0") }} - in overall order
- `Class` {{ sinceVersion("1.0.0") }} - in the order of focused car's class
- `Cup` {{ sinceVersion("1.4.0") }} - in the order of focused car's class and cup (Overall, Pro-Am etc).

  This is effectively an ACC specific leaderboard. In all other games it is
  equivalent to `Class` leaderboard.

## Relative leaderboards

`2M + 1` relative positions to the focused car (`M` ahead and `M` behind). There are three types:

- `RelativeOverall` {{ sinceVersion("1.0.0") }} - in overall order
- `RelativeClass` {{ sinceVersion("1.0.0") }} - in the order of focused car's class
- `RelativeCup` {{ sinceVersion("1.4.0") }} - in the order of focused car's class and cup

  This is effectively an ACC specific leaderboard. In all other games it is
  equivalent to `RelativeClass` leaderboard.

- `RelativeOnTrack` {{ sinceVersion("1.0.0") }} - in the relative track order
- `RelativeOnTrackWoPit` {{ sinceVersion("1.2.2") }} - in the relative track order but excludes all the cars in the
  pitlane that are not on
  the same lap as the focused car

## Partial relative leaderboards

`N` top positions and `2M + 1` relative positions (`M` ahead and `M` behind). If the focused car is inside the first
`N + M + 1` positions the order
will be just as the overall leaderboard. There are two types:

- `PartialRelativeOverall` {{ sinceVersion("1.0.0") }} - in overall order
- `PartialRelativeClass` {{ sinceVersion("1.0.0") }} - in the order of focused car's class
- `PartialRelativeCup` {{ sinceVersion("1.4.0") }} - in the order of focused car's class and cup

  This is effectively an ACC specific leaderboard. In all other games it is
  equivalent to `PartialRelativeClass` leaderboard.

## A quick screenshot comparison of each type

{{ leaderboardTypePreviews() }}
