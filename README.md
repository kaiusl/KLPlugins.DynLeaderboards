# SimHub ACC Leaderboard Plugin

This is an ACC specific (at least at the moment) leaderboard plugin. 
I know that there are other leaderboard plugins around but I wanted to try my own, maybe someone else finds it useful.

## Features

### Available leaderboard types

We provide several different leaderboard orderings or types. 

- Overall leaderboards

    `N` top positions. There are two types:
    - Overall
	- In the order of focused car's class
- Relative leaderboards

	`2N+1` relative positions to the focused car. There are three types:
    - In overall order
	- In the order of focused car's class
	- In the relative track order
- Partial relative leaderboards

    `N` top positions and `2M+1` relative positions. If the focused car is inside the first `N + M + 1` positions the order will be just as the overall leaderboard. There are two types:
	- In overall order
	- In the order of foused car's class

### Properties

All available properties are listed in SimHub under Leadeboard plugin settings with more detailed description. You can also disable any of the properties that you don't need.

## Using the plugin

The thing to know about this plugin is that we expose car properties only once in overall order through `Overall.xx.<property name>` properties. This means that we don't need to expose same property in multiple leaderboards like overall, class or relative. This also means that number of overall positions exported should be larger or equal to the number of total cars. 
Otherwise class or relative leaderboards may not have access to all the cars they need. The number of shown overall positions can be changed in settings.


### Constructing leaderboards in different order

Class or relative leaderboards can be constructed through `InClass.xx.OverallPosition` which returns the overall position of xx-th car in class.
Then we can access all of the properties of that car from overall order.
This means doing following in javascript
```javascript
var overallPos = $prop('LeaderboardPlugin.InClass.' + repeatindex() + '.OverallPosition')
return $prop('LeaderboardPlugin.Overall.' + overallPos + '.CarNumber')
```
or in NCalc similarly
```javascript
prop('LeaderboardPlugin.Overall.' + prop('LeaderboardPlugin.InClass.' + repeatindex() + '.OverallPosition') + '.CarNumber')
```

Granted this complicates accessing car properties a little but with the plugin is provided a JavaScript extension file 
which provies functions to do above and simplify accessing car properties. For example the car number of 5th car in class can be accessed by 

```javascript
return InClass(5, 'CarNumber')
```
and similarly for other orderings.

***IMPORTANT:*** Note that the NCalc version is the fastest of three, followed by the 1st version and the javascript extension is the slowest. The difference between NCalc and javascipt extension seems to be around 1.5x for me.


Currently we provide:
 - `Overall(pos, propname)`: Get property `propname` for `pos`-th car overall.
 - `InClass(pos, propname)`: Get property `propname` for `pos`-th car in class.
 - `OverallRelativeToFocused(pos, propname, numRelPos)`: Get property `propname` for `pos`-th car relative to currently focused car in overall order.
 
	Note that `pos` starts from 1 and that would be the car that is `numRelPos` positions ahead of the focused car. `pos == numRelPos + 1` is the focused car and `pos == 2numRelPos + 1` is the last car shown and `numRelPos` behind the focused car. The reason for this is that in SimHub you probably use repeated group to build the leaderboard and it's indexer `repeatindex()` starts at 1. So we also start counting at 1.
 - `OverallRelativeToFocusedPartial(pos, propname, numRelPos, numOverallPos)`: Get property `propname` for `pos`-th car relative to currently focused car in overall order or `pos`-th car overall if `pos < numOverallPos`. That is we show `numOverallPos` positions from the top of overall standings and then `numRelPos` realative positions around each side of focused car. See "Relative overall" screen on example dash.
 - `RelativeOnTrack(pos, propname)`: Get property `propname` for `pos`-th car relative on track to currently focused car. 
 
	Note that `pos` starts from 1. That is if `n` is the number of relative position specified in settings then `pos=1` is the car that is `n` positions ahead of focused car. `pos == n+1` is the focused car and `pos == 2n+1` is the last car, `n` positions behind focused car.
 - `Focused(propname)`: Get property `propname` for currently focused car.
 - `OverallBestLap(propname)`: Get property `propname` for the car that has best lap overall.
 - `InClassBestLap(propname)`: Get property `propname` for the car that has best lap in class.

For more examples you can see example dashboard provided with the plugin.




## SH version

Last SimHub version this plugin was tested is 7.4.23.