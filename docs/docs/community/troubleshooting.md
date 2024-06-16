## Issues

In case you find issues or bugs, feel free to report them. 
If at all possible we would prefer issues to be reported on [GitHub], just to keep them clean and all in one place. 
If that is not possible the [discussion thread on OverTake forums] is fine as well.

Before reporting check through existing [issues] and [discussion thread on OverTake forums] for similar issues.
Maybe it has already been reported or even better has some solution.

When reporting issues, please include as much information as possible. 
This will help to solve any issues faster. 
Of course not all of the item below are necessary or relevant for all issues but if you think it could be helpful please include it.
That information includes:

* Game and session info (SP or MP, practice/race etc).
* SimHub, plugin and game versions.
* SimHub log files from {{ path("../SimHub/Logs") }} (usually it is best to zip all of them together)
    * Basic logging information should show most direct errors 
      however more information is logged by turning on `Log` option in the plugin settings menu under ["General settings -> DEBUG"](../user_guide/config.md#debug). 
      If possible you could turn that on and try to reproduce the issue with it.
* A SimHub replay displaying the issue.
* A screenshot displaying the issue.
* The dashboard where issue appeared (if it is not provided by this plugin).

## Compatibility

* Any game other than ACC requires plugin 2.0.0+. 
* ACC 1.10.2+ requires plugin 1.4.5+. 
* ACC 1.9.6+ requires plugin 1.4.0+.
* ACC 1.9.3+ requires plugin 1.3.2+.
* SimHub 8.3.0+ requires plugin 1.3.0+.

The table below reports the latest version of games and SimHub each plugin version has been tested with.

| Plugin | SimHub | ACC    | AC     | AMS2    | rF2    | R3E      |
| ------ | ------ | ------ | ------ | ------- | ------ | -------- |
| 2.0.0  | 9.3.4  | 1.10.2 | 1.16.4 | 1.5.6.3 | 1.1134 | 0.9.5.52 |

Any game missing from the table has not been tested and is not officially supported.
Note however that since the plugin mostly uses generic SimHub data then non-tested games may also be working.


[GitHub]: https://github.com/kaiusl/KLPlugins.DynLeaderboards/issues
[issues]: https://github.com/kaiusl/KLPlugins.DynLeaderboards/issues
[discussion thread on OverTake forums]: https://www.overtake.gg/threads/acc-simhub-dynamic-leaderboards-plugin.229921/

--8<-- "includes/abbreviations.md"