# SimHub ACC Leaderboard Plugin

This is an ACC specific (at least at the moment) leaderboard plugin providing simple switching between overall/class/relative leaderboards. 

The reason for this plugin is that I found myself creating effectively the same dash leaderboard layout several times for overall leaderboard and then again for class leaderboard and so on. With this plugin you need to create only one SimHub dash and assign buttons to swap between different leaderboard types. I provide example dash (named AccDynLeaderboard) which I created for my own use. It's relatively simple one and designed to be used on smartphone.

## Using the plugin for the first time

* Download the latest release from Racedepartment or here
* Copy all the files to the SimHub root
* Open SimHub and enable the plugin
* Check plugin settings for correct "ACC configuration location" under "General settings".  If it's background is green, then we found needed files, if it's red there's something wrong with the location. This location is used to read information needed to connct to ACC broadcasting client.
* If you needed to change the location, restart SimHub.
* Go to "Controls and events" from SimHub sidebar and add mappings for "DynLeaderboardsPlugin.Dynamic.NextLeaderboard" and "DynLeaderboardsPlugin.Dynamic.PreviousLeaderboard" actions. 

	For mapping to controller inputs you need to enable "Controllers input" plugin and to keyboard inputs "Keyboard Input" plugin.
    
* Now the AccDynLeaderboard dash should work.
 
 ## More detailed information

Most of the information below is also available directly in SimHub under the plugin settings pages for easy access later but I do recommend going through it here once.

### Available leaderboard types

There are several different leaderboard types to use within dynamic leaderboards:

- Overall leaderboards

    `N` top positions. There are two types:
    - In overall order
	- In the order of focused car's class
- Relative leaderboards

	`2N + 1` relative positions to the focused car. There are three types:
    - In overall order
	- In the order of focused car's class
	- In the relative track order
- Partial relative leaderboards

    `N` top positions and `2M + 1` relative positions. If the focused car is inside the first `N + M + 1` positions the order will be just as the overall leaderboard. There are two types:
	- In overall order
	- In the order of foused car's class

Again see the AccDynLeaderboard dash to see exactly what each dash looks like.

### Properties

All available properties are listed in SimHub under Leadeboard plugin settings with more detailed description. You can also disable any of the properties that you don't need.

Couple of things to know about properties:

- Default value if property is not available is null. This happens if session is not started yet, there are fewer cars/drivers in session than positions available for leaderboard or no lap time is available.
- First driver is always current driver.
- All times and gaps are given in seconds.
- In relative leaderboards positive gap means the car is ahead of the car that we are comparing to, negative gap means behind. In overall leaderboards the gap is always positive as we are comparing to the overall/class leader and no one can be ahead of them.
- If the gap is larger than 1 lap, only the lap part of the gap is shown. To differentiate between gap in seconds and full laps we add 100 000 to the gap if it's larger than 1 lap. In dash you can show the gap then as follows
    ```javascript
	var v = $prop('DynLeaderboardsPlugin.Dynamic.' + repeatindex() + '.Gap.Dynamic.ToFocused')
	if (v == null) { return '' }
	// No gap can realistically be 50000 seconds without being more than a lap
	// and you cannot realistically be more than 50000 laps behind to break following
	if (v > 50000) { return format(v - 100000, '0', true) + 'L' }
	return format(v, '0.0', true)
	```
- There are dynamic gaps and deltas to laps that change based on the currently selected leaderboard to show meaningful gaps for the current leaderboard. For example in overall leaderboard we show gaps to the overall leader, in class leaderboards to the class leader and so on.

### Configuring the leaderboards

By default the plugin is configured to run provided AccDynLeaderboard dash but you can add and configure more leaderboards under "Dynamic leaderboards" tab. Further explanation of each option is explained directly in the setting pages and by the tooltips of settings. 

If you add multiple dynamic leaderboards, you need to add mappings for each leaderboard. It can be the same button for all of them. 

Settings under "General settings" are common to all leaderboards.

***IMPORTANT*: For the changes to take effect you need to restart SimHub.**

If something is unclear or you have suggestions, let me know.

### Troubleshooting

- No data available
    - Check the property "DynLeaderboardsPlugin.IsBroadcastClientConnected"
	- If it's `False` then the plugin couldn't connect to ACC and you need to leave and rejoin the session. This can sometimes happen if you close SimHub and reopen it without leaving the session. 
	- If it's `True` then the plugin haven't just recieved the first update and you need to wait a bit.
- Wrong leaderboard order
    - If you opened SimHub mid session or joined the mid race session, restart SimHub or leave and rejoin session. There is slight possibility if the car is in the exact right place at the time you joined, it will gain a lap. At the moment it's a TODO to figure out how to fix it without SimHub restart.
	- It's an unknown bug, please report it as an issue here or in RaceDepartment forum.

### SH version

Last SimHub version this plugin was tested is 7.4.23.