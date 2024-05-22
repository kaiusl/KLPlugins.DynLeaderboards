So you have created your own dynamic leaderboard and a custom dashboard that uses it and want to upload it for other to use. 
Since version [1.2.0](https://github.com/kaiusl/KLPlugins.DynLeaderboards/releases/tag/v1.2.0) the leaderboard 
configurations are saved in separate files so you could package them with your download and simplify the configuration 
needed by the end user. 

How to include?

- Go to {{ path("..\SimHub\PluginsData\KLPlugins\DynLeaderboards\leaderboardConfigs") }}.
- Pack the file with your dynamic leaderboard's name with your download.
- The end user must copy the file back to that location and the configuration will be automatically loaded. 
  (I suggest to include the proper folder structure inside the download).

Now the only configuration needed is to assign the buttons to change leaderboard types.