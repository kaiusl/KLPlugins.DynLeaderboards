# SimHub ACC Dynamic Leaderboards Plugin

This is an ACC specific (at least at the moment) leaderboard plugin providing simple switching between overall/class/relative leaderboards. 

The reason for this plugin is that I found myself creating effectively the same dash leaderboard layout several times for overall leaderboard and then again for class leaderboard and so on. And then again when I decided to change something. With this plugin you need to create only one SimHub dash and assign buttons to swap between different leaderboard types. Also there seem to be some issues with SimHub's ACC leaderboard data, which I set on to fix with this plugin. I provide example dash (named AccDynLeaderboard) which I created for my own use. It's relatively simple one and designed to be used on smartphone.

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

Head over to the [wiki](https://github.com/kaiusl/KLPlugins.DynLeaderboards/wiki). It describes all available options, properties, how to use, configure and troubleshoot known issues.

### SimHub and ACC version

Last tested on ACC 1.8.14 and SimHub 7.4.23.
