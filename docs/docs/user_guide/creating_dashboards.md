## Things to know

- First driver is always current driver.
- All times and gaps are given in seconds.
- In relative leaderboards positive gap means the car is ahead of the car that we are comparing to, negative gap means behind.
  In overall leaderboards the gap is always positive as we are comparing to the overall/class leader and no one can be ahead of them.
- If the gap is larger than 1 lap, only the lap part of the gap is shown. To differentiate between gap in seconds and 
  full laps we add 100 000 to the gap if it's larger than 1 lap. In dash you can show the gap then as follows
    ```javascript
	var v = $prop('DynLeaderboardsPlugin.Dynamic.' + repeatindex() + '.Gap.Dynamic.ToFocused')
	if (v == null) { return '' }
	// No gap can realistically be 50000 seconds without being more than a lap
	// and you cannot realistically be more than 50000 laps behind to break following
	if (v > 50000) { return format(v - 100000, '0', true) + 'L' }
	return format(v, '0.0', true)
	```
- There are dynamic gaps and deltas to laps that change based on the currently selected leaderboard to show meaningful 
  gaps for the current leaderboard. 
  For example in overall leaderboard we show gaps to the overall leader, in class leaderboards to the class leader and so on.

## Patterns

### Access properties

=== "Javascript"

    ```javascript
    return $prop('DynLeaderboardsPlugin.Dynamic.' + repeatindex() + '.Laps.Last.Time')
    ```

=== "NCalc"

    ```javascript
    prop('DynLeaderboardsPlugin.Dynamic.' + repeatindex() + '.Laps.Last.Time')
    ```

### Gap formatting

```javascript
var v = $prop('DynLeaderboardsPlugin.Dynamic.' + repeatindex() + '.Gap.Dynamic.ToFocused')
if (v == null) { return '' }
// No gap can realistically be 50000 seconds without being more than a lap
// and you cannot realistically be more than 50000 laps behind to break following
if (v > 50000) { return format(v - 100000, '0', true) + 'L' }
return format(v, '0.00', true)
```

### Gap to behind

The plugin provides a property for a gap to car ahead but not to the car behind. The latter is found by shifting the index by one.

```javascript
var gap_to_ahead = $prop('DynLeaderboardsPlugin.Dynamic.' + repeatindex() + '.Gap.Dynamic.ToAhead')
var gap_to_behind = $prop('DynLeaderboardsPlugin.Dynamic.' + (repeatindex() + 1) + '.Gap.Dynamic.ToAhead')
// gap formatting
```

### Properties of the cars right ahead/behind of the focused car

Also see [#21](https://github.com/kaiusl/KLPlugins.DynLeaderboards/discussions/21) for more discussion.

*Gap*

```javascript
var idx = $prop('DynLeaderboardsPlugin.Dynamic.FocusedPosInCurrentLeaderboard') + 1
var ahead = $prop('DynLeaderboardsPlugin.Dynamic.' + idx + '.Gap.Dynamic.ToAhead')
// gap formatting
```
```javascript
var idx = $prop('DynLeaderboardsPlugin.Dynamic.FocusedPosInCurrentLeaderboard') + 2
var behind = $prop('DynLeaderboardsPlugin.Dynamic.' + idx + '.Gap.Dynamic.ToAhead')
// gap formatting
```

*Other*

```javascript
var idx = $prop('DynLeaderboardsPlugin.Dynamic.FocusedPosInCurrentLeaderboard')
var ahead = $prop('DynLeaderboardsPlugin.Dynamic.' + idx + '.Laps.Last.Time')
// gap formatting
```
```javascript
var idx = $prop('DynLeaderboardsPlugin.Dynamic.FocusedPosInCurrentLeaderboard') + 2
var behind = $prop('DynLeaderboardsPlugin.Dynamic.' + idx + '.Laps.Last.Time')
// gap formatting
```

### Color cars on different laps differently on RelativeOnTrack leaderboards

```javascript
var current_lb = $prop('DynLeaderboardsPlugin.Dynamic.CurrentLeaderboard')
var isRelative = current_lb == 'RelativeOnTrack' || current_lb == 'RelativeOnTrackWoPit'

if (isRelative) {
    var lapdiff = $prop('DynLeaderboardsPlugin.Dynamic.' + repeatindex() + '.RelativeOnTrackLapDiff')
    if (lapdiff == 1) {
        // Ahead
        return "#FF8C00"
    } else if (lapdiff == -1) {
        // Behind
        return "#00BFFF"
    }
}
// Same lap
return "#FFFFFF"
```

--8<-- "includes/abbreviations.md"