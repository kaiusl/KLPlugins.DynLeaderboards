This is an leaderboard plugin providing simple switching between overall/class/relative (and more) leaderboards. [^1]

[^1]: Prior to version 2.0.0 this plugin only supported ACC.

The reason for this plugin is that I found myself creating effectively the same leaderboard layout several times for 
overall leaderboard and then again for class leaderboard and so on. 
And then again when I decided to change something. 
With this plugin you need to create only one SimHub dash and assign buttons to swap between different leaderboard types. 
I provide example dash (named "DynLeaderboard") which I created for my own use. 
It's relatively simple one and designed to be used on smartphone.

## Features

- Dynamic leaderboards.
    - Has a easy a way to switch between leaderboard types on a single dash screen with a single click.
    - Properties that change based on currently selected leaderboard.
    - Support multiple different dynamic leaderboards simultaneously.
- New leaderboard types ([see here](reference/leaderboards.md)).
- New properties ([see here](reference/properties.md)).
- More stable calculation of gaps between the cars (no more gap changing by seconds depending if you are in the corner or straights).
- Easy configuration through SimHub.

## Next steps

<div class="grid cards" markdown>

- :material-clock-fast:{ .lg .middle } [**Getting started**](user_guide/getting_started.md)
- :material-wrench-outline:{ .lg .middle } [**Configuration**](user_guide/config.md)
- :material-view-dashboard-edit:{ .lg .middle } [**Create custom dashboards**](user_guide/creating_dashboards.md)
- :simple-databricks:{ .lg .middle } [**Properties reference**](reference/properties.md)
- :material-message-question-outline:{ .lg .middle } [**Troubleshooting**](user_guide/troubleshooting.md)

</div>

--8<-- "includes/abbreviations.md"