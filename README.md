# SimHub ACC Dynamic Leaderboards Plugin

This is an ACC specific (at least at the moment) leaderboard plugin providing simple switching between overall/class/relative leaderboards. 

The reason for this plugin is that I found myself creating effectively the same dash leaderboard layout several times for overall leaderboard and then again for class leaderboard and so on. With this plugin you need to create only one SimHub dash and assign buttons to swap between different leaderboard types. Also there seem to be some issues with SimHub's ACC leaderboard data, which I set on to fix with this plugin. I provide example dash (named AccDynLeaderboard) which I created for my own use. It's relatively simple one and designed to be used on smartphone.

## Features
- Connect directly to ACC broadcasting server to have most control and try to provide reliable results.
- Provide a way to switch between leaderboard types on a single dash screen with a single click.
    - Also provide gaps and lap deltas that change based on currently selected leaderboard
- Provide more leaderboard types ([see below](#available-leaderboard-types)).
- Calculate bunch of new properties. (Not going to list them here, but download the plugin and all of them are listed inside the settings menu).
- More stable calculation of gaps between the cars.

## Using the plugin for the first time

* Download the latest release from [Racedepartment](https://www.racedepartment.com/downloads/acc-simhub-dynamic-leaderboards-plugin.50424/) or [here](https://github.com/kaiusl/KLPlugins.Leaderboard/releases)
* Copy all the files from folder SimHub to the SimHub root
* Open SimHub and enable the plugin
* Check plugin settings for correct "ACC configuration location" under "General settings".  If it's background is green, then we found needed files, if it's red there's something wrong with the location. We need to find the file "...\Documents\Assetto Corsa Competizione\Config\broadcasting.json". It is used to read information needed to connct to ACC broadcasting client.
* If you needed to change the location, restart SimHub.
* Go to "Controls and events" from SimHub sidebar and add mappings for `DynLeaderboardsPlugin.Dynamic.NextLeaderboard` and `DynLeaderboardsPlugin.Dynamic.PreviousLeaderboard` actions. 

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

### Notes on available properties

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

### Notes on configuring the leaderboards

- By default the plugin is configured to run provided AccDynLeaderboard dash but you can add and configure more leaderboards under "Dynamic leaderboards" tab. Further explanation of each option is explained directly in the setting pages and by the tooltips of settings. 

- If you add multiple dynamic leaderboards, you need to add mappings for each leaderboard. It can be the same button for all of them. 

- Settings under "General settings" are common to all leaderboards.

- ***IMPORTANT*: For the settings changes to take effect you need to restart SimHub.**

If something is unclear or you have suggestions, let me know.

### Creating your own dynamic leadeboard

First head over to the "Dynamic leaderboards" tab in the settings and add a new dynamic leaderboard or edit the one already present. You can change the name by opening the dropdown menu and writing inside the corresponding box. Then all properties will be available as `DynLeaderboardsPlugin.<chosen name>.<pos>.<property name>`. For example you named your dynamic leaderboard as "MyDynLeaderboard" then you can do `DynLeaderboardsPlugin.MyDynLeaderboard.5.Car.Number` to get the fifth car's number in whatever leaderboard type is currently. If it's overall order you get the fifth car overall, if it's on track relative leaderboard you get the car that is in fifth position in that order.

Next you need to set the leaderboards rotation and which ones you even want to see in your rotation. Check the toggle buttons and move leaderboards up and down. If there are unchecked leaderboards are simply ignored, doesn't matter in which location they are.

Next section provides an option to set number of positions exported as properties. The actual number of car's exported is the maximum positions needed for any leaderboard. For relative leaderboards you set the number of cars shown ahead and behind the focused car. Drivers are ordered such that the current driver is always the first one and if you set number of drivers to 1, we only export current driver. Note that you can set any of the properties to 0 to not export them.

Then scroll through the properties and select all of the ones you want. For dynamic leaderboard I recommend to use `...Gaps.Dynamic...` and `...Delta.Dynamic...` properties which change according to the currently selected leaderboard. This way you don't need to switch between the gaps and lap deltas yourself.

Final configuration step is to go to the "Controls and events" from SimHub sidebar and add mappings for `DynLeaderboardsPlugin.<name>.NextLeaderboard` and `DynLeaderboardsPlugin.<name>.PreviousLeaderboard` actions. As stated above for mapping to controller inputs you need to enable "Controllers input" plugin and to keyboard inputs "Keyboard Input" plugin.

Now restart SimHub and start create your dashboard. You can use the AccDynLeaderboard as an example or modify it directly.

### Troubleshooting

- No data available
    - Check the property `DynLeaderboardsPlugin.IsBroadcastClientConnected`
	- If it's `False` then the plugin couldn't connect to ACC and you need to leave and rejoin the session. This can sometimes happen if you close SimHub and reopen it without leaving the session. 
	- If it's `True` then the plugin haven't just recieved the first update and you need to wait a bit.
- Wrong leaderboard order
    - If you opened SimHub mid session or joined the mid race session, restart SimHub or leave and rejoin session. There is slight possibility if the car is in the exact right place at the time you joined, it will gain a lap. At the moment it's a TODO to figure out how to fix it without SimHub restart.
	- It's an unknown bug, please report it as an issue here or in RaceDepartment forum.

### SH version

Last SimHub version this plugin was tested is 7.4.23.
