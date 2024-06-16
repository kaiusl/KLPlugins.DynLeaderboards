
??? info "Symbols used"

      {{ prop("PropertyName", "1.0.0", ["ACC", "rF2", "LMU"], defv="Default value", ty="type") }}
      : Description.


      **`PropertyName`**
      : Part of the property name used to access it from SimHub. How a full property name is formed
        is described before each section below.

      `type`
      : Corresponding C# type to this property. Note that if it is followed by `?`
        then the property can be `null`. For example `int?`.

      {{ defValueIcon }} `value`
      : This is the value of property if it's not available. 
        This includes cases like:

        * it's not available for given game
        * or it's not set yet 
        * or it's a car property and there are less cars in the session
        * or it's a driver property and there are less drivers in the car.

        If this symbol is missing from the property description, then there is no default value and
        the property is always available.

      {{ versionIcon }} 1.0.0
      : This marks the version a given property was added to the plugin.

      {{ supportedGames("AC", "AMS2") }}
      : This shows the list of games that a given property is available. In above the property is available for AC and AMS2.
        
        Note that this list only includes the games that the plugin has been tested with.



## General
These properties are available as `DynLeaderboardsPlugin.<property name>`, 
for example `DynLeaderboardsPlugin.Color.Class.GT3`.

<div class="props" markdown>

{{ prop("Session.Phase", "1.0.0", ["ACC", "rF2", "LMU"], defv="\"Unknown\"", ty="string") }}
: Session phase.

{{ prop("Session.MaxStintTime", "1.0.0", ["ACC"], defv=-1.0, ty="double") }}
: Maximum driver stint time.

{{ prop("Session.MaxDriveTime", "1.0.0", ["ACC"], defv=-1.0, ty="double") }}
: Maximum total driving time for driver for player car. This can be different for other teams if they have different 
  number of drivers.

{{ prop("Session.NumberOfClasses", "2.0.0", ["all"], defv=0, ty="int") }}
: Number of different classes in current session.

{{ prop("Session.NumberOfCups", "2.0.0", ["all"], defv=0, ty="int") }}
: Number of different cups (class and team cup category combinations) in current session.

{{ prop("Color.Class.<class>", "1.0.0", ["all"], ty="string", defv="\"#000000\"") }}
: Background color for car class.

      Note that this export one property for every class color in the plugin settings [Colors](../user_guide/config.md#colors) tab where the
      `<class>` in property name is replaced by the class name.
      For example `Color.Class.GT3`.

{{ prop("Color.Class.<class>.Text", "2.0.0", ["all"], ty="string", defv="\"#FFFFFF\"") }}
: Text color for car class.

      Note that this export one property for every class color in the plugin settings [Colors](../user_guide/config.md#colors) tab where the
      `<class>` in property name is replaced by the class name.
      For example `Color.Class.GT3.Text`.

{{ prop("Color.Cup.<category>", "1.0.0", ["all"], ty="string", defv="\"#000000\"") }}
: Background color for team cup category.

      Note that this export one property for every team cup category color in the plugin settings [Colors](../user_guide/config.md#colors) tab where the
      `<cup category>` in property name is replaced by the team cup category name.
      For example `Color.Cup.Overall`.

{{ prop("Color.Cup.<category>.Text", "1.0.0", ["all"], ty="string", defv="\"#FFFFFF\"") }}
: Text color for team cup category. 

      Note that this export one property for every team cup category color in the plugin settings [Colors](../user_guide/config.md#colors) tab where the
      `<category>` in property name is replaced by the team cup category name.
      For example `Color.Cup.Overall.Text`.

{{ prop("Color.DriverCategory.<category>", "1.0.0", ["all"], ty="string", defv="\"#000000\"") }}
: Background color for driver category.

      Note that this export one property for every driver category color in the plugin settings [Colors](../user_guide/config.md#colors) tab where the
      `<category>` in property name is replaced by the driver category name.
      For example `Color.DriverCategory.Platinum`.

{{ prop("Color.DriverCategory.<category>.Text", "2.0.0", ["all"], ty="string", defv="\"#FFFFFF\"") }}
: Text color for driver category.

      Note that this export one property for every driver category color in the plugin settings [Colors](../user_guide/config.md#colors) tab where the
      `<category>` in property name is replaced by the driver category name.
      For example `Color.DriverCategory.Platinum.Text`.

</div>

## For each dynamic leaderboard

These properties are available as `DynLeaderboardsPlugin.<leaderboard name>.<property name>`, 
for example `DynLeaderboardsPlugin.Dynamic.CurrentLeaderboard`.

<div class="props" markdown>

{{ prop("Currentleaderboard", "1.0.0", ["all"], ty="string") }}
: Name of the currently selected leaderboard. See [the reference](leaderboards.md) for more info about what types are available.

{{ prop("FocusedPosInCurrentLeaderboard", "1.0.0", ["all"], defv="null", ty="int?") }}
: Integer that shows the position of focused car in currently selected leaderboard. Note that it is 0 based like an 
  index to array.

</div>

### For each car

These properties are available as `DynLeaderboardsPlugin.<leaderboard name>.<position>.<property name>`, 
for example `DynLeaderboardsPlugin.Dynamic.5.Car.Number`.

#### Car info

<div class="props" markdown>

{{ prop("Car.Number", "1.0.0", ["all", "!AC", "!AMS2"], defv="null", ty="int?") }}
: Car number as an integer.

      Note that this property cannot differentiate leading zeros.
      For example in rF2 `01` and `1` are both possible and are different cars but
      this property will report both as number `1`.

      Prefer to use `Car.Number.Text` which can differentiate between the two.

{{ prop("Car.Number.Text", "2.0.0", ["all", "!AC", "!AMS2"], defv="null", ty="string?") }}
: Car number as a text. This allows to differentiate leading zeros in number.

{{ prop("Car.Model", "1.0.0", ["all"], defv="null", ty="string?") }}
: Car model name.

{{ prop("Car.Manufacturer", "1.0.0", ["all"], defv="null", ty="string?") }}
: Car manufacturer.

{{ prop("Car.Class", "1.0.0", ["all"], defv="null", ty="string?") }}
: Car class.

{{ prop("Car.Class.Color", "1.0.0", ["all"], defv="null", ty="string?") }}
: Car class color.

{{ prop("Car.Class.TextColor", "2.0.0", ["all"], defv="null", ty="string?") }}
: Car class text color.

</div>

#### Team info

<div class="props" markdown>

{{ prop("Team.Name", "1.0.0", ["all"], defv="null", ty="string?") }}
: Team name.

{{ prop("Team.CupCategory", "1.0.0", ["all"], defv="null", ty="string?") }}
: Team cup category.

      This is effectively an ACC specific property but for the sake of consistency
      and ease of use all other games default to "`Overall`" category. 

{{ prop("Team.CupCategory.Color", "1.0.0", ["all"], defv="null", ty="string?") }}
: Team cup category main color. The intention was to be used as background color.

{{ prop("Team.CupCategory.TextColor", "1.0.0", ["all"], defv="null", ty="string?") }}
:  Team cup category secondary color. The intention was to be used as text color.

</div>

#### Lap info

<div class="props" markdown>

{{ prop("Laps.Count", "1.0.0", ["all"], defv="null", ty="int?") }}
: Number of completed laps.

{{ prop("Laps.Last.Time", "1.0.0", ["all"], defv="null", ty="double?") }}
: Last lap time.

{{ prop("Laps.Last.<sector>", "1.0.0", ["all"], defv="null", ty="double?") }}
: Last lap sector time. &lt;sector&gt; can be `S1`, `S2`, `S3`.

{{ prop("Laps.Last.IsValid", "1.2.1", ["all"], defv="null", ty="int?") }}
: Was last lap valid?

{{ prop("Laps.Last.IsOutLap", "1.2.1", ["all"], defv="null", ty="int?") }}
: Was last lap an out lap?

{{ prop("Laps.Last.IsInLap", "1.2.1", ["all"], defv="null", ty="int?") }}
: Was last lap an in lap?

{{ prop("Laps.Best.Time", "1.0.0", ["all"], defv="null", ty="double?") }}
: Best lap time.

{{ prop("Laps.Best.<sector>", "1.0.0", ["all"], defv="null", ty="double?") }}
: Best lap sector time. &lt;sector&gt; can be `S1`, `S2`, `S3`

{{ prop("Laps.Current.Time", "1.0.0", ["all"], defv="null", ty="double?") }}
: Current lap time.

{{ prop("Laps.Current.IsValid", "1.2.1", ["all"], defv="null", ty="int?") }}
: Is current lap valid?

{{ prop("Laps.Current.IsOutLap", "1.2.1", ["all"], defv="null", ty="int?") }}
: Is current lap an out lap?

{{ prop("Laps.Current.IsInLap", "1.2.1", ["all"], defv="null", ty="int?") }}
: Is current lap an in lap?

{{ prop("Best<sector>", "1.0.0", ["all"], defv="null", ty="double?") }}
: Best sector time. &lt;sector&gt; can be `S1`, `S2`, `S3`

***Deltas***

*Best to best*

{{ prop("Laps.Best.Delta.ToOverallBest", "1.0.0", ["all"], defv="null", ty="double?") }}
: Best lap delta to overall best lap.

{{ prop("Laps.Best.Delta.ToClassBest", "1.0.0", ["all"], defv="null", ty="double?") }}
: Best lap delta to class best lap.

{{ prop("Laps.Best.Delta.ToCupBest", "1.4.0", ["all"], defv="null", ty="double?") }}
: Best lap delta to cup best lap.

      This is effectively an ACC specific property. In all other games it is
      equivalent to `Laps.Best.Delta.ToClassBest`.

{{ prop("Laps.Best.Delta.ToLeaderBest", "1.0.0", ["all"], defv="null", ty="double?") }}
: Best lap delta to leader's best lap.

{{ prop("Laps.Best.Delta.ToClassLeaderBest", "1.0.0", ["all"], defv="null", ty="double?") }}
: Best lap delta to class leader's best lap.

{{ prop("Laps.Best.Delta.ToCupLeaderBest", "1.4.0", ["all"], defv="null", ty="double?") }}
: Best lap delta to cup leader's best lap.

      This is effectively an ACC specific property. In all other games it is
      equivalent to `Laps.Best.Delta.ToClassLeaderBest`.

{{ prop("Laps.Best.Delta.ToFocusedBest", "1.0.0", ["all"], defv="null", ty="double?") }}
: Best lap delta to focused car's best lap.

{{ prop("Laps.Best.Delta.ToAheadBest", "1.0.0", ["all"], defv="null", ty="double?") }}
: Best lap delta to ahead car's best lap.

{{ prop("Laps.Best.Delta.ToAheadInClassBest", "1.0.0", ["all"], defv="null", ty="double?") }}
: Best lap delta to class ahead car's best lap.

{{ prop("Laps.Best.Delta.ToAheadInCupBest", "1.4.0", ["ACC"], defv="null", ty="double?") }}
: Best lap delta to cup ahead car's best lap.

      This is effectively an ACC specific property. In all other games it is
      equivalent to `Laps.Best.Delta.ToAheadInClassBest`.

*Last to best*                                 

{{ prop("Laps.Last.Delta.ToOverallBest", "1.0.0", ["all"], defv="null", ty="double?") }}
: Last lap delta to overall best lap.

{{ prop("Laps.Last.Delta.ToClassBest", "1.0.0", ["all"], defv="null", ty="double?") }}
: Last lap delta to class best lap.

{{ prop("Laps.Last.Delta.ToCupBest", "1.4.0", ["ACC"], defv="null", ty="double?") }}
: Last lap delta to cup best lap.

      This is effectively an ACC specific property. In all other games it is
      equivalent to `Laps.Last.Delta.ToClassBest`.

{{ prop("Laps.Last.Delta.ToLeaderBest", "1.0.0", ["all"], defv="null", ty="double?") }}
: Last lap delta to leader's best lap.

{{ prop("Laps.Last.Delta.ToClassLeaderBest", "1.0.0", ["all"], defv="null", ty="double?") }}
: Last lap delta to class leader's best lap.

{{ prop("Laps.Last.Delta.ToCupLeaderBest", "1.4.0", ["ACC"], defv="null", ty="double?") }}
: Last lap delta to cup leader's best lap.

      This is effectively an ACC specific property. In all other games it is
      equivalent to `Laps.Last.Delta.ToCupLeaderBest`.

{{ prop("Laps.Last.Delta.ToFocusedBest", "1.0.0", ["all"], defv="null", ty="double?") }}
: Last lap delta to focused car's best lap.

{{ prop("Laps.Last.Delta.ToAheadBest", "1.0.0", ["all"], defv="null", ty="double?") }}
: Last lap delta to ahead car's best lap.

{{ prop("Laps.Last.Delta.ToAheadInClassBest", "1.0.0", ["all"], defv="null", ty="double?") }}
: Last lap delta to class ahead car's best lap.

{{ prop("Laps.Last.Delta.ToAheadInCupBest", "1.4.0", ["ACC"], defv="null", ty="double?") }}
: Last lap delta to cup ahead car's best lap.

      This is effectively an ACC specific property. In all other games it is
      equivalent to `Laps.Last.Delta.ToAheadInClassBest`.

{{ prop("Laps.Last.Delta.ToOwnBest", "1.0.0", ["all"], defv="null", ty="double?") }}
: Last lap delta to own best lap.

*Last to last*                             

{{ prop("Laps.Last.Delta.ToLeaderLast", "1.0.0", ["all"], defv="null", ty="double?") }}
: Last lap delta to leader's last lap.

{{ prop("Laps.Last.Delta.ToClassLeaderLast", "1.0.0", ["all"], defv="null", ty="double?") }}
: Last lap delta to class leader's last lap.

{{ prop("Laps.Last.Delta.ToCupLeaderLast", "1.4.0", ["ACC"], defv="null", ty="double?") }}
: Last lap delta to cup leader's last lap.

      This is effectively an ACC specific property. In all other games it is
      equivalent to `Laps.Last.Delta.ToClassLeaderLast`.

{{ prop("Laps.Last.Delta.ToFocusedLast", "1.0.0", ["all"], defv="null", ty="double?") }}
: Last lap delta to focused car's last lap.

{{ prop("Laps.Last.Delta.ToAheadLast", "1.0.0", ["all"], defv="null", ty="double?") }}
: Last lap delta to ahead car's last lap.

{{ prop("Laps.Last.Delta.ToAheadInClassLast", "1.0.0", ["all"], defv="null", ty="double?") }}
: Last lap delta to class ahead car's last lap.

{{ prop("Laps.Last.Delta.ToAheadInCupLast", "1.4.0", ["ACC"], defv="null", ty="double?") }}
: Last lap delta to cup ahead car's last lap.

      This is effectively an ACC specific property. In all other games it is
      equivalent to `Laps.Last.Delta.ToAheadInClassLasts`.

*Dynamic*                                  

{{ prop("Laps.Best.Delta.Dynamic.ToFocusedBest", "1.0.0", ["all"], defv="null", ty="double?") }}
: Best lap delta to the car's best based on currently displayed leaderboard type.

      * `Overall` -> Best lap delta to leader's best lap.
      * `Class` -> Best lap delta to class leader's best lap.
      * `Cup` -> Best lap delta to cup leader's best lap.
      * any relative leaderboard -> Best lap delta to focused car's best lap.

{{ prop("Laps.Last.Delta.Dynamic.ToFocusedBest", "1.0.0", ["all"], defv="null", ty="double?") }}
: Same as above but last lap delta to other car's best lap.

{{ prop("Laps.Last.Delta.Dynamic.ToFocusedLast", "1.0.0", ["all"], defv="null", ty="double?") }}
: Same as above but last lap delta to other car's last lap.

</div>

#### Gaps

<div class="props" markdown>

{{ prop("Gap.ToOverallLeader", "1.0.0", ["all"], defv="null", ty="double?") }}
: Total gap to the leader.                                                     

{{ prop("Gap.ToClassLeader", "1.0.0", ["all"], defv="null", ty="double?") }}
: Total gap to the class leader.

{{ prop("Gap.ToCupLeader", "1.4.0", ["all"], defv="null", ty="double?") }}
: Total gap to the cup leader.   

      This is effectively an ACC specific property. In all other games it is
      equivalent to `Gap.ToClassLeader`.

{{ prop("Gap.ToFocused.Total", "1.0.0", ["all"], defv="null", ty="double?") }}
: Total gap to the focused car.                                                

{{ prop("Gap.ToFocused.OnTrack", "1.0.0", ["all"], defv="null", ty="double?") }}
: Relative on track gap to the focused car.                                    

{{ prop("Gap.ToAhead.Overall", "1.0.0", ["all"], defv="null", ty="double?") }}
: Total gap to the car ahead in overall order.                                 

{{ prop("Gap.ToAhead.Class", "1.0.0", ["all"], defv="null", ty="double?") }}
: Total gap to the car ahead in class.                                         

{{ prop("Gap.ToAhead.Cup", "1.4.0", ["all"], defv="null", ty="double?") }}
: Total gap to the car ahead in cup.        

      This is effectively an ACC specific property. In all other games it is
      equivalent to `Gap.ToAhead.Class`.

{{ prop("Gap.ToAhead.OnTrack", "1.0.0", ["all"], defv="null", ty="double?") }}
: Relative gap to the car ahead on track.                                      

{{ prop("Gap.Dynamic.ToFocused", "1.0.0", ["all"], defv="null", ty="double?") }}
: Gap that changes based on the currently selected leaderboard type.

      * `Overall` -> Total gap to the leader.
      * `Class` -> Total gap to the class leader.
      * `Cup` -> Total gap to the cup leader.
      * `(Partial)RelativeOverall`, `(Partial)RelativeClass`, `(Partial)RelativeCup` -> Total gap to the focused car.
      * `RelativeOnTrack(WoPit)` -> Relative on track gap to the focused car.

{{ prop("Gap.Dynamic.ToAhead", "1.0.0", ["all"], defv="null", ty="double?") }}
: Gap to the car ahead that changes based on the currently selected leaderboard type.

      * `(Partial)(Relative)Overall` -> Total gap to the car ahead in overall order.
      * `(Partial)(Relative)Class` -> Total gap to the car ahead in class.
      * `(Partial)(Relative)Cup` -> Total gap to the car ahead in cup.
      * `RelativeOnTrack(WoPit)` -> Relative gap to the car ahead on track.

</div>

#### Positions

<div class="props" markdown>

{{ prop("Position.Overall", "1.0.0", ["all"], defv="null", ty="int?") }}
: Current overall position.

{{ prop("Position.Overall.Start", "1.0.0", ["all"], defv="null", ty="int?") }}
: Overall position at the race start.

{{ prop("Position.Class", "1.0.0", ["all"], defv="null", ty="int?") }}
: Current class position.

{{ prop("Position.Class.Start", "1.0.0", ["all"], defv="null", ty="int?") }}
: Class position at the race start.

{{ prop("Position.Cup", "1.4.0", ["ACC"], defv="null", ty="int?") }}
: Current cup position.

      This is effectively an ACC specific property. In all other games it is
      equivalent to `Position.Class`.

{{ prop("Position.Cup.Start", "1.4.0", ["ACC"], defv="null", ty="int?") }}
: Cup position at the race start.

      This is effectively an ACC specific property. In all other games it is
      equivalent to `Position.Class.Start`.

{{ prop("Position.Dynamic", "1.2.0", ["all"], defv="null", ty="int?") }}
: Position that changes based of currently displayed leaderboard type. 

      * `(Partial)(Relative)Overall` -> overall position
      * `(Partial)(Reltaive)Class` -> class position
      * `(Partial)(Relative)Cup` -> cup position,
      * `RelativeOnTrack(WoPit)` -> overall position
  
{{ prop("Position.Dynamic.Start", "1.2.0", ["all"], defv="null", ty="int?") }}
: Position at race start that changes based of currently displayed leaderboard type.

      * `(Partial)(Relative)Overall` -> overall position
      * `(Partial)(Reltaive)Class` -> class position
      * `(Partial)(Relative)Cup` -> cup position,
      * `RelativeOnTrack(WoPit)` -> overall position

</div>

#### Stint info

<div class="props" markdown>

{{ prop("Stint.Current.Time", "1.0.0", ["all"], defv="null", ty="double?") }}
: Current stint time.

{{ prop("Stint.Current.Laps", "1.0.0", ["all"], defv="null", ty="int?") }}
: Number of laps completed in current stint.

{{ prop("Stint.Last.Time", "1.0.0", ["all"], defv="null", ty="double?") }}
: Last stint time.

{{ prop("Stint.Last.Laps", "1.0.0", ["all"], defv="null", ty="int?") }}
: Number of laps completed in last stint.

</div>

#### Pit info

<div class="props" markdown>
{{ prop("Pit.IsIn", "1.0.0", ["all"], defv="null", ty="int?") }}
: Is the car in pit lane?

{{ prop("Pit.Count", "1.0.0", ["all"], defv="null", ty="int?") }}
: Number of pitstops.

{{ prop("Pit.Time.Total", "1.0.0", ["all"], defv="null", ty="double?") }}
: Total time spent in pits.

{{ prop("Pit.Time.Last", "1.0.0", ["all"], defv="null", ty="double?") }}
: Last pit time.

{{ prop("Pit.Time.Current", "1.0.0", ["all"], defv="null", ty="double?") }}
: Current time in pits.

</div>

Note that pit time counts time from the start of pitlane to the end, not just the stationary time in pit box.

#### Misc

<div class="props" markdown>

{{ prop("IsFinished", "1.0.0", ["all"], defv="null", ty="int?") }}
: Is the car finished?

{{ prop("MaxSpeed", "1.0.0", ["all"], defv="null", ty="double?") }}
: Maximum speed in this session.

{{ prop("IsFocused", "1.0.0", ["all"], defv="null", ty="int?") }}
: Is this the focusd car?

{{ prop("IsOverallBestLapCar", "1.0.0", ["all"], defv="null", ty="int?") }}
: Is this the car that has overall best lap?

{{ prop("IsClassBestLapCar", "1.0.0", ["all"], defv="null", ty="int?") }}
: Is this the car that has class best lap?

{{ prop("IsCupBestLapCar", "1.4.0", ["ACC"], defv="null", ty="int?") }}
: Is this the car that has cup best lap?

      This is effectively an ACC specific property. In all other games it is
      equivalent to `IsClassBestLapCar`.

{{ prop("RelativeOnTrackLapDiff", "1.1.0", ["all"], defv="null", ty="int?") }}
: Show if this car is ahead or behind by one lap (or more) of the focused car in relative on track leaderboard.

      * `1` -> ahead
      * `0` -> same lap
      * `-1` -> behind

</div>

#### For each driver
`DynLeaderboardsPlugin.<leaderboard name>.<position>.Driver.<driver number>.<property name>`, 
for example `DynLeaderboardsPlugin.Dynamic.5.Driver.1.FirstName`.

<div class="props" markdown>

{{ prop("FirstName", "1.0.0", ["ACC"], defv="null", ty="string?") }}
: First name (Abcde)

{{ prop("LastName", "1.0.0", ["ACC"], defv="null", ty="string?") }}
: Last name (Fghij)

{{ prop("ShortName", "1.0.0", ["all"], defv="null", ty="string?") }}
: Short name (AFG)

{{ prop("FullName", "1.0.0", ["all"], defv="null", ty="string?") }}
: Full name (Abcde Fghij)

{{ prop("InitialPlusLastName", "1.0.0", ["all"], defv="null", ty="string?") }}
: Initial + last name (A. Fghij)

{{ prop("Nationality", "1.0.0", ["ACC"], defv="null", ty="string?") }}
: Nationality

{{ prop("Category", "1.0.0", ["all"], defv="null", ty="string?") }}
: Driver category.

      This is effectively an ACC specific property but for the sake of consistency
      and ease of use all other games default to "`Platinum`" category. 

{{ prop("TotalLaps", "1.0.0", ["all"], defv="null", ty="int?") }}
: Total number of completed laps.

{{ prop("TotalDrivingTime", "1.0.0", ["all"], defv="null", ty="double?") }}
: Total driving time.

{{ prop("BestLapTime", "1.0.0", ["all"], defv="null", ty="double?") }}
: Best lap time.

{{ prop("CategoryColor", "1.0.0", ["ACC"], defv="null", ty="string?", deprecated="2.0.0", deprecatedTooltip="Use `Category.Color` instead.") }}
: Background color for driver category.

      **DEPRECATED**. Use **`Category.Color`** instead.

{{ prop("Category.Color", "2.0.0", ["ACC"], defv="null", ty="string?") }}
: Background color for driver category.

{{ prop("Category.TextColor", "2.0.0", ["ACC"], defv="null", ty="string?") }}
: Text color for driver category.

</div>