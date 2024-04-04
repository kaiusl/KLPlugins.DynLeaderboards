## General
These properties are available as `DynLeaderboardsPlugin.<property name>`, 
for example `DynLeaderboardsPlugin.Color.Class.GT3`.

<div class="props" markdown>

**`SessionPhase`**
: Session phase.

**`MaxStintTime`**
: Maximum driver stint time.

**`MaxDriveTime`**
: Maximum total driving time for driver for player car. This can be different for other teams if they have different 
  number of drivers.

**`Color.Class.<class>`**
: Color for car class.  &lt;class&gt; can be `GT3`, `GT4`, `CUP17`, `CUP21`, `ST15`, `ST21`, `CHL`, `TCX`.

**`Color.Cup.<cup category>`**
: Main color for team cup category. &lt;cup category&gt; can be `Overall`, `Silver`, `ProAm`, `Am`, `National`.

**`Color.Cup.<cup category>.Text`**
: Secondary color for team cup category. &lt;cup category&gt; can be `Overall`, `Silver`, `ProAm`, `Am`, `National`.

**`Color.DriverCategory.<category>`**
: Color for driver category. &lt;categort&gt; can be `Platinum`, `Gold`, `Silver`, `Bronze`.

</div>

## For each dynamic leaderboard

These properties are available as `DynLeaderboardsPlugin.<leaderboard name>.<property name>`, 
for example `DynLeaderboardsPlugin.Dynamic.CurrentLeaderboard`.

<div class="props" markdown>

**`CurrentLeaderboard`**
: Name of the currently selected leaderboard. Is one of (`Overall`, `Class`, `RelativeOverall`, `RelativeClass`,
  `PartialRelativeOverall`, `PartialRelativeClass`, `RelativeOnTrack`).
  See [Leaderboards](leaderboards.md) for more info about what each type means.

**`FocusedPosInCurrentLeaderboard`**
: Integer that shows the position of focused car in currently selected leaderboard. Note that it is 0 based like an 
  index to array positions.

</div>

### For each car

These properties are available as `DynLeaderboardsPlugin.<leaderboard name>.<position>.<property name>`, 
for example `DynLeaderboardsPlugin.Dynamic.5.Car.Number`.

#### Car info

<div class="props" markdown>

**`Car.Number`**
: Car number.

**`Car.Model`**
: Car model name.

**`Car.Manufacturer`**
: Car manufacturer.

**`Car.Class`**
: Car class. Is one of (`GT3`, `GT4`, `CUP17`, `CUP21`, `ST15`, `ST21`, `CHL`, `TCX`).

**`Car.Class.Color`**
: Car class color.

</div>

#### Team info

<div class="props" markdown>

**`Team.Name`**
: Team name.

**`Team.CupCategory`**
: Team cup category. Is one of (`Overall`, `Silver`, `ProAm`, `Am`, `National`).

**`Team.CupCategory.Color`**
: Team cup category main color. My intention was to use as background color.

**`Team.CupCategory.TextColor`**
:  Team cup category secondary color. My intention was to use as text color.

</div>

#### Lap info

<div class="props" markdown>

**`Laps.Count`**
: Number of completed laps.

**`Laps.Last.Time`**
: Last lap time.

**`Laps.Last.<sector>`**
: Last lap sector time. &lt;sector&gt; can be `S1`, `S2`, `S3`.

**`Laps.Last.IsValid`**
: Was last lap valid?

**`Laps.Last.IsOutLap`**
: Was last lap an out lap?

**`Laps.Last.IsInLap`**
: Was last lap an in lap?

**`Laps.Best.Time`**
: Best lap time.

**`Laps.Best.<sector>`**
: Best lap sector time. &lt;sector&gt; can be `S1`, `S2`, `S3`

**`Laps.Current.Time`**
: Current lap time.

**`Laps.Current.IsValid`**
: Is current lap valid?

**`Laps.Current.IsOutLap`**
: Is current lap an out lap?

**`Laps.Current.IsInLap`**
: Is current lap an in lap?

**`Best<sector>`**
: Best sector time. &lt;sector&gt; can be `S1`, `S2`, `S3`

***Deltas***

*Best to best*

**`Laps.Best.Delta.ToOverallBest`**
: Best lap delta to overall best lap.

**`Laps.Best.Delta.ToClassBest`**
: Best lap delta to class best lap.

**`Laps.Best.Delta.ToCupBest`**
: Best lap delta to cup best lap.

**`Laps.Best.Delta.ToLeaderBest`**
: Best lap delta to leader's best lap.

**`Laps.Best.Delta.ToClassLeaderBest`**
: Best lap delta to class leader's best lap.

**`Laps.Best.Delta.ToCupLeaderBest`**
: Best lap delta to cup leader's best lap.

**`Laps.Best.Delta.ToFocusedBest`**
: Best lap delta to focused car's best lap.

**`Laps.Best.Delta.ToAheadBest`**
: Best lap delta to ahead car's best lap.

**`Laps.Best.Delta.ToAheadInClassBest`**
: Best lap delta to class ahead car's best lap.

**`Laps.Best.Delta.ToAheadInCupBest`**
: Best lap delta to cup ahead car's best lap.

*Last to best*                                 

**`Laps.Last.Delta.ToOverallBest`**
: Last lap delta to overall best lap.

**`Laps.Last.Delta.ToClassBest`**
: Last lap delta to class best lap.

**`Laps.Last.Delta.ToCupBest`**
: Last lap delta to cup best lap.

**`Laps.Last.Delta.ToLeaderBest`**
: Last lap delta to leader's best lap.

**`Laps.Last.Delta.ToClassLeaderBest`**
: Last lap delta to class leader's best lap.

**`Laps.Last.Delta.ToCupLeaderBest`**
: Last lap delta to cup leader's best lap.

**`Laps.Last.Delta.ToFocusedBest`**
: Last lap delta to focused car's best lap.

**`Laps.Last.Delta.ToAheadBest`**
: Last lap delta to ahead car's best lap.

**`Laps.Last.Delta.ToAheadInClassBest`**
: Last lap delta to class ahead car's best lap.

**`Laps.Last.Delta.ToAheadInCupBest`**
: Last lap delta to cup ahead car's best lap.

**`Laps.Last.Delta.ToOwnBest`**
: Last lap delta to own best lap.

*Last to last*                             

**`Laps.Last.Delta.ToLeaderLast`**
: Last lap delta to leader's last lap.

**`Laps.Last.Delta.ToClassLeaderLast`**
: Last lap delta to class leader's last lap.

**`Laps.Last.Delta.ToCupLeaderLast`**
: Last lap delta to cup leader's last lap.

**`Laps.Last.Delta.ToFocusedLast`**
: Last lap delta to focused car's last lap.

**`Laps.Last.Delta.ToAheadLast`**
: Last lap delta to ahead car's last lap.

**`Laps.Last.Delta.ToAheadInClassLast`**
: Last lap delta to class ahead car's last lap.

**`Laps.Last.Delta.ToAheadInCupLast`**
: Last lap delta to cup ahead car's last lap.

*Dynamic*                                  

**`Laps.Best.Delta.Dynamic.ToFocusedBest`**
: Best lap delta to the car's best based on currently displayed leaderboard.

      * `Overall` -> Best lap delta to leader's best lap.
      * `Class` -> Best lap delta to class leader's best lap.
      * `Cup` -> Best lap delta to cup leader's best lap.
      * for any relative leaderboard it's delta to focused car's best lap.

**`Laps.Last.Delta.Dynamic.ToFocusedBest`**
: Same as above but last lap delta to other car's best lap.

**`Laps.Last.Delta.Dynamic.ToFocusedLast`**
: Same as above but last lap delta to other car's last lap.

</div>

#### Gaps

<div class="props" markdown>

**`Gap.ToOverallLeader`**
: Total gap to the leader.                                                     

**`Gap.ToClassLeader`**
: Total gap to the class leader.                                               

**`Gap.ToCupLeader`**
: Total gap to the cup leader.                                                 

**`Gap.ToFocused.Total`**
: Total gap to the focused car.                                                

**`Gap.ToFocused.OnTrack`**
: Relative on track gap to the focused car.                                    

**`Gap.ToAhead.Overall`**
: Total gap to the car ahead in overall order.                                 

**`Gap.ToAhead.Class`**
: Total gap to the car ahead in class.                                         

**`Gap.ToAhead.Cup`**
: Total gap to the car ahead in cup.                                           

**`Gap.ToAhead.OnTrack`**
: Relative gap to the car ahead on track.                                      

**`Gap.Dynamic.ToFocused`**
: Gap that changes based on the currently selected dynamic leaderboard.

      * `Overall` -> Total gap to the leader.
      * `Class` -> Total gap to the class leader.
      * `Cup` -> Total gap to the cup leader.
      * `(Partial)RelativeOverall`, `(Partial)RelativeClass`, `(Partial)RelativeCup` -> Total gap to the focused car.
      * `RelativeOnTrack` -> Relative on track gap to the focused car.

**`Gap.Dynamic.ToAhead`**
: Gap to the car ahead that changes based on the currently selected dynamic leaderboard.

      * `(Partial)(Relative)Overall` -> Total gap to the car ahead in overall order.
      * `(Partial)(Relative)Class` -> Total gap to the car ahead in class.
      * `(Partial)(Relative)Cup` -> Total gap to the car ahead in cup.
      * `RelativeOnTrack` -> Relative gap to the car ahead on track.

</div>

#### Positions

<div class="props" markdown>

**`Position.Overall`**
: Current overall position.

**`Position.Overall.Start`**
: Overall position at the race start.

**`Position.Class`**
: Current class position.

**`Position.Class.Start`**
: Class position at the race start.

**`Position.Cup`**
: Current cup position.

**`Position.Cup.Start`**
: Cup position at the race start.

</div>

#### Stint info

<div class="props" markdown>

**`Stint.Current.Time`**
: Current stint time.

**`Stint.Current.Laps`**
: Number of laps completed in current stint.

**`Stint.Last.Time`**
: Last stint time.

**`Stint.Last.Laps`**
: Number of laps completed in last stint.

</div>

#### Pit info

<div class="props" markdown>
**`Pit.IsIn`**
: Is the car in pit lane?

**`Pit.Count`**
: Number of pitstops.

**`Pit.Time.Total`**
: Total time spent in pits.

**`Pit.Time.Last`**
: Last pit time.

**`Pit.Time.Current`**
: Current time in pits.

</div>

Note that pit time counts time from the start of pitlane to the end, not just the stationary time in pit box.

#### Misc

<div class="props" markdown>

**`IsFinished`**
: Is the car finished?

**`MaxSpeed`**
: Maximum speed in this session.

**`IsFocused`**
: Is this the focusd car?

**`IsOverallBestLapCar`**
: Is this the car that has overall best lap?

**`IsClassBestLapCar`**
: Is this the car that has class best lap?

**`IsCupBestLapCar`**
: Is this the car that has cup best lap?

**`RelativeOnTrackLapDiff`**
: Show if this car is ahead or behind by one lap (or more) of the focused car in relative on track leaderboard.

      * `1` -> ahead
      * `0` -> same lap
      * `-1` -> behind

</div>

#### For each driver
`DynLeaderboardsPlugin.<leaderboard name>.<position>.Driver.<driver number>.<property name>`, 
for example `DynLeaderboardsPlugin.Dynamic.5.Driver.1.FirstName`.

<div class="props" markdown>

**`FirstName`**
: First name (Abcde)

**`LastName`**
: Last name (Fghij)

**`ShortName`**
: Short name (AFG)

**`FullName`**
: Full name (Abcde Fghij)

**`InitialPlusLastName`**
: Initial + last name (A. Fghij)

**`Nationality`**
: Nationality

**`Category`**
: Driver category. Is one of (`Platinum`, `Gold`, `Silver`, `Bronze`)

**`TotalLaps`**
: Total number of completed laps.

**`TotalDrivingTime`**
: Total driving time.

**`BestLapTime`**
: Best lap time.

**`CategoryColor`**
: Color for driver category.

</div>