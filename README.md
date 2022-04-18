# SimHub ACC Leaderboard Plugin

This is an ACC specific (at least at the moment) leaderboard plugin providing simple switching between overall/class/relative leaderboards. 

The reason for this plugin is that I found myself creating effectively the same dash leaderboard layout several times for overall leaderboard and then again for class leaderboard and so on. With this plugin you need to create only one SimHub dash and assign buttons to swap between different leaderboard types. I provide example dash (named AccDynLeaderboard) which I created for my own use. It's relatively simple one and designed to be used on smartphone.

## Using the plugin

* Download the latest release from Racedepartment or here
* Copy all the files to the SimHub root
* Open SimHub and enable the plugin
* Check plugin settings for correct "ACC configuration location" under "General settings". This location is used to read information needed to connct to ACC broadcasting client.
* If you needed to change the location, restart SimHub.
* Go to "Controls and events" from SimHub sidebar and add mappings for "LeaderboardPlugin.&lt;leaderboard name&gt;.NextLeaderboard" and "LeaderboardPlugin.&lt;leaderboard name&gt;.PreviousLeaderboard" actions. 

	For mapping to controller inputs you need to enable "Controllers input" plugin and to keyboard inputs "Keyboard Input" plugin.
    
	Note that if you add multiple dynamic leaderboards, you need to add mappings for each leaderboard. It can be the same button for all of them.
* Now the AccDynLeaderboard dash should work.
 
## Available leaderboard types

We provide several different leaderboard orderings or types. 

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

Again see the example dash to see exactly what each dash looks like.

## Properties

All available properties are listed in SimHub under Leadeboard plugin settings with more detailed description. You can also disable any of the properties that you don't need.

Couple of things to know about properties:

- Default value if property is not available is null. This happens if session is not started yet, there are fewer cars/drivers in session than positions available for leaderboard or no lap time is available.
- First driver is always current driver.

## Configuring the leaderboards

By default the plugin is configured to run provided AccDynLeaderboard dash but you can add and configure more leaderboards under "Dynamic leaderboard". Further explanation of each option is explained directly in the setting pages and by the tooltips of settings. 

Settings under "General settings" are common to all leaderboards.

***IMPORTANT*: For the changes to take effect you need to restart SimHub.**

If something is unclear or you have suggestions, let me know.

## Troubleshooting

- No data available
    - Check the property "Leaderboard.IsBroadcastClientConnected"
	- If it's `False` then the plugin couldn't connect to ACC and you need to leave and rejoin the session. This can sometimes happen if you close SimHub and reopen it without leaving the session. 
	- If it's `True` then the plugin haven't just recieved the first update and you need to wait a bit.
- Wrong leaderboard order
    - If you opened SimHub mid session or joined the mid race session, restart SimHub or leave and rejoin session. There is slight possibility if the car is in the exact right place at the time you joined, it will gain a lap. At the moment it's a TODO to figure out how to fix it without SimHub restart.
	- It's an unknown bug, please report it as an issue here or in RaceDepartment forum.

## SH version

Last SimHub version this plugin was tested is 7.4.23.