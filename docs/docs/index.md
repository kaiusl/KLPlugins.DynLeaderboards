---
hide:
  - navigation
---

#          

<div style="margin-left:200px;" markdown>

This is an leaderboard plugin for [SimHub] providing simple switching between different leaderboard types.

We provide example dash (named "DynLeaderboards Example") which is relatively simple one and designed to be used on
smartphone.
It serves mostly as an example for how to create dashboards with this plugin.

[^1]: Prior to version 2.0.0 this plugin only supported ACC.

!!! Quote "Kaius <span style="font-weight:normal;">*(original author)*</span>"

    The reason for this plugin is that I found myself creating effectively the same leaderboard layout several times for 
    overall leaderboard and then again for class leaderboard and so on. 
    And then again when I decided to change something. 
    With this plugin you need to create only one [SimHub] dash and assign buttons to swap between different leaderboard types.

## Features

- Dynamic leaderboards.
    - Easy to switch between leaderboard types on a single dash screen with a single click.
    - Properties that change based on currently selected leaderboard.
    - Support multiple different dynamic leaderboards simultaneously.
- Support for multiple games. Tested with AC, ACC, AMS2, rF2 and R3E. [^1]
- New leaderboard types ([see here](reference/leaderboards.md)).
- New properties ([see here](reference/properties.md)).
- More stable calculation of gaps between the cars (no more gap changing by seconds depending if you are in the corner
  or straights).
- Easy configuration through [SimHub].
- Customizable car information (classes, names etc) and colors.

## How it works?

The plugin exports data for set number of positions as SimHub properties.
You can create a regular leaderboard dash using these properties.
This data however is populated based on the currently selected leaderboard type which can be easily changed using SimHub
actions.
This creates a dynamic leaderboard where you can change the leaderboard type on a single dash screen with a click of a
button.

## Next steps

<div class="grid cards" markdown>

- :material-clock-fast:{ .lg .middle } [**Getting started**](user_guide/getting_started.md)
- :material-wrench-outline:{ .lg .middle } [**Configuration**](user_guide/config.md)
- :material-view-dashboard-edit:{ .lg .middle } [**Create custom dashboards**](user_guide/creating_dashboards.md)
- :material-view-dashboard:{ .lg .middle } [**Find dashboards**](community/dashes.md)
- :simple-databricks:{ .lg .middle } [**Properties reference**](reference/properties.md)
- :simple-databricks:{ .lg .middle } [**Leaderboards reference**](reference/leaderboards.md)

</div>

[SimHub]: https://www.simhubdash.com/

</div>

--8<-- "includes/abbreviations.md"