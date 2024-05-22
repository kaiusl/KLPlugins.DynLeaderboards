This is an leaderboard plugin providing simple switching between overall/class/relative (and more) leaderboards. [^1]

[^1]: Prior to version 2.0.0 this plugin only supported ACC.

The reason for this plugin is that I found myself creating effectively the same leaderboard layout several times for 
overall leaderboard and then again for class leaderboard and so on. 
And then again when I decided to change something. 
With this plugin you need to create only one SimHub dash and assign buttons to swap between different leaderboard types. 
I provide example dash (named "DynLeaderboard") which I created for my own use. 
It's relatively simple one and designed to be used on smartphone.

## Features

- Provide a way to switch between leaderboard types on a single dash screen with a single click.
    - Also provide gaps and lap deltas that change based on currently selected leaderboard.
- Provide more leaderboard types ([see here](reference/leaderboards.md)).
- Calculate bunch of new properties ([see here](reference/properties.md)).
- More stable calculation of gaps between the cars (no more gap changing by 1s depending if you are in the corner or straights).

--8<-- "includes/abbreviations.md"