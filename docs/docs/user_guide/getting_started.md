# Getting started

* Download the latest release from [OverTake][OverTakeDownload] or [GitHub][GitHubReleases].
* To install provided dashboard run *"AccDynLeaderboard_v8.simhubdash"*.
* Copy all the files from folder *"SimHub"* to the SimHub root.
* Open SimHub and enable the plugin.
* Check plugin settings for correct "ACC configuration location" under "General settings".  
  If it's background is green, then we found needed files, if it's red there's something wrong with the location. 
  We need to find the file *"...\Documents\Assetto Corsa Competizione\Config\broadcasting.json"*. 
  It is used to read information needed to connct to ACC broadcasting client.
* If you needed to change the location, restart SimHub.
* Go to "Controls and events" from SimHub sidebar and add mappings for 
  `DynLeaderboardsPlugin.Dynamic.NextLeaderboard` and `DynLeaderboardsPlugin.Dynamic.PreviousLeaderboard` actions. 

	For mapping to controller inputs you need to enable "Controllers input" plugin and to keyboard inputs "Keyboard Input" plugin.
    
* Now the AccDynLeaderboard dash should work.

[OverTakeDownload]: https://www.racedepartment.com/downloads/acc-simhub-dynamic-leaderboards-plugin.50424/
[GitHubReleases]: https://github.com/kaiusl/KLPlugins.Leaderboard/releases

