## Compatibility

| Plugin version | SimHub versions | ACC versions    |
| -------------- | --------------- | --------------- |
| 1.4.0 ..       | 8.3.0 ..        | 1.x.x ..        |
| 1.3.2 .. 1.3.3 | 8.3.0 ..        | 1.x.x .. 1.9.5  |
| 1.3.0 .. 1.3.1 | 8.3.0 ..        | 1.x.x .. 1.9.2  |
| .. 1.2.3       | .. 8.2.3        | 1.x.x .. 1.8.21 |

## Known issues

- No data available
    - Check the property `DynLeaderboardsPlugin.IsBroadcastClientConnected`
    - If it's `False` then the plugin couldn't connect to ACC and you need to leave and rejoin the session. 
      This can sometimes happen if you close SimHub and reopen it without leaving the session. 
    - If it's `True` then the plugin hasn't received the first update yet. 
      If the data doesn't appear after a while see the section below.

## Other

In case you find other issues, feel free to report them. 
If at all possible I'd prefer issues to be reported on [GitHub], just to keep them clean and all in one place. 
If that's not possible the [discussion thread on OverTake forums] is fine as well.

When reporting issues, please include as much information as possible. 
This will help to solve any issues much faster. 
This information includes:

* What session you were running (SP or MP, practice/race etc).
* SimHub log files from *"../SimHub/Logs"* (usually it's best to zip all of them together)
    * Basic logging information should show most direct errors 
      however more information is logged by turning on `Log` option in the plugin settings menu. 
      If possible you could turn that on and try to reproduce the issue with it.
* A screenshot displaying the issue.
* The dashboard where issue appeared (if it's not provided by this plugin).


[GitHub]: https://github.com/kaiusl/KLPlugins.DynLeaderboards
[discussion thread on OverTake forums]: https://www.overtake.gg/threads/acc-simhub-dynamic-leaderboards-plugin.229921/