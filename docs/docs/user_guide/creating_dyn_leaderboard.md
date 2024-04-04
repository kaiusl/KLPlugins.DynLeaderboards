## Creating one

First head over to the "Dynamic leaderboards" tab in the settings and add a new dynamic leaderboard or edit the one 
already present. 
You can change the name by opening the dropdown menu and writing inside the corresponding box. 
Then all properties will be available as `DynLeaderboardsPlugin.<chosen name>.<pos>.<property name>`. 
For example you named your dynamic leaderboard as "MyDynLeaderboard" then you can do 
`DynLeaderboardsPlugin.MyDynLeaderboard.5.Car.Number` to get the fifth car's number in whatever leaderboard type is currently. 
If it's overall order you get the fifth car overall, if it's on track relative leaderboard you get the car that is in 
fifth position in that order.

Next you need to set the leaderboards rotation and which ones you even want to see in your rotation. 
Check the toggle buttons and move leaderboards up and down. 
If there are unchecked leaderboards are simply ignored, doesn't matter in which location they are.

Next section provides an option to set number of positions exported as properties. 
The actual number of car's exported is the maximum positions needed for any leaderboard. 
For relative leaderboards you set the number of cars shown ahead and behind the focused car. 
Drivers are ordered such that the current driver is always the first one and if you set number of drivers to 1, 
we only export current driver. 
Note that you can set any of the properties to 0 to not export them.

Then scroll through the properties and select all of the ones you want. 
For dynamic leaderboard I recommend to use `...Gaps.Dynamic...` and `...Delta.Dynamic...` properties which change 
according to the currently selected leaderboard. This way you don't need to switch between the gaps and lap deltas yourself.

Final configuration step is to go to the "Controls and events" from SimHub sidebar and add mappings 
for `DynLeaderboardsPlugin.<name>.NextLeaderboard` and `DynLeaderboardsPlugin.<name>.PreviousLeaderboard` actions. 
As stated above for mapping to controller inputs you need to enable "Controllers input" plugin and to keyboard inputs 
"Keyboard Input" plugin.

Now restart SimHub and start create your dashboard. 
You can use the AccDynLeaderboard as an example or modify it directly.

## Publishing your dashboard

So you have created your dynamic leaderboard and a custom dashboard that uses it and want to upload it. 
Since version [1.2.0](https://github.com/kaiusl/KLPlugins.DynLeaderboards/releases/tag/v1.2.0) the leaderboard 
configurations are saved in separate files so you could package them with your download and simplify the configuration 
needed by the end user. 

How to include?

- Go to *"..\SimHub\PluginsData\KLPlugins\DynLeaderboards\leaderboardConfigs"*.
- Pack the file with your dynamic leaderboard's name with your download.
- The end user must copy the file back to that location and the configuration will be automatically loaded. 
  (I suggest to include the proper folder structure inside the download).

Now the only configuration needed is to assign the buttons to change leaderboard types.

## Things to know

- Default value if property is not available is null. This happens if session is not started yet, there are fewer
  cars/drivers in session than positions available for leaderboard or no lap time is available.
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

### Properties of the cars right ahead/behind of the player

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