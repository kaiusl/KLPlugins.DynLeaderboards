using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

using GameReaderCommon;

using KLPlugins.DynLeaderboards.Helpers;
using KLPlugins.DynLeaderboards.Track;

using ksBroadcastingNetwork;

using Newtonsoft.Json;

namespace KLPlugins.DynLeaderboards.Car {
    public class CarData {
        public CarClass CarClass { get; private set; }
        public TextBoxColor CarClassColor { get; private set; }

        public string CarNumber { get; private set; } // string because 001 and 1 could be different numbers in some games
        public string CarModel { get; private set; }
        public string CarManufacturer { get; private set; }
        public string? TeamName { get; private set; }
        public TeamCupCategory TeamCupCategory { get; private set; }
        public TextBoxColor TeamCupCategoryColor { get; private set; }

        public NewOld<CarLocation> Location { get; } = new(CarLocation.NONE);

        public NewOld<int> Laps { get; } = new(0);
        public bool IsNewLap { get; private set; } = false;
        public TimeSpan CurrentLapTime { get; private set; }
        public bool IsCurrentLapOutLap { get; private set; }
        public bool IsCurrentLapInLap { get; private set; }
        public bool IsCurrentLapValid { get; private set; }
        public Lap? LastLap { get; private set; }
        public Lap? BestLap { get; private set; }
        public SectorSplits BestSectors => this.RawDataNew.BestSectorSplits;

        public bool IsBestLapCarOverall { get; private set; }
        public bool IsBestLapCarInClass { get; private set; }

        public bool IsFocused => this.RawDataNew.IsPlayer;

        /// <summary>
        /// List of all drivers. Current driver is always the first.
        /// </summary>
        public List<Driver> Drivers { get; } = new();
        public Driver? CurrentDriver => this.Drivers.FirstOrDefault();

        public int PositionOverall { get; private set; }
        public int PositionInClass { get; private set; }
        public int? PositionOverallStart { get; private set; }
        public int? PositionInClassStart { get; private set; }
        public int IndexOverall { get; private set; }
        public int IndexClass { get; private set; }

        public bool IsInPitLane { get; private set; }
        public bool ExitedPitLane { get; private set; }
        public bool EnteredPitLane { get; private set; }
        public int PitCount { get; private set; }
        public TimeSpan? PitTimeLast { get; private set; }
        public TimeSpan TotalPitTime { get; private set; }
        public TimeSpan? PitTimeCurrent { get; private set; }


        public TimeSpan? GapToLeader { get; private set; }
        public TimeSpan? GapToClassLeader { get; private set; }
        public TimeSpan? GapToFocusedTotal { get; private set; }
        public TimeSpan? GapToFocusedOnTrack { get; private set; }
        public TimeSpan? GapToAheadOnTrack { get; private set; }
        public TimeSpan? GapToCupLeader { get; private set; }
        public TimeSpan? GapToAhead { get; private set; }
        public TimeSpan? GapToAheadInClass { get; private set; }
        public TimeSpan? GapToAheadInCup { get; private set; }
        public RelativeLapDiff RelativeOnTrackLapDiff { get; private set; }
        public double SplinePosition { get; private set; }

        /// <summary>
        /// > 0 if ahead, < 0 if behind. Is in range [-0.5, 0.5].
        /// </summary>
        public double RelativeSplinePositionToFocusedCar { get; private set; }
        public double TotalSplinePosition { get; private set; } = 0.0;

        public bool JumpedToPits { get; private set; } = false;
        public bool HasCrossedStartLine { get; private set; } = true;
        private bool _isHasCrossedStartLineSet = false;
        public bool IsFinished { get; private set; } = false;
        public DateTime? FinishTime { get; private set; } = null;

        public int? CurrentStintLaps { get; private set; }
        public TimeSpan? LastStintTime { get; private set; }
        public TimeSpan? CurrentStintTime { get; private set; }
        public int? LastStintLaps { get; private set; }
        private DateTime? _stintStartTime;

        public double MaxSpeed { get; private set; } = 0.0;

        internal string Id => this.RawDataNew.Id;
        internal bool IsUpdated { get; set; }

        internal Opponent RawDataNew;
        internal Opponent RawDataOld;

        // In some games the spline position and the lap counter reset at different locations.
        // Since we use total spline position to order the cars on track, we need them to be in sync
        internal enum OffsetLapUpdateType {
            None = 0,
            LapBeforeSpline = 1,
            SplineBeforeLap = 2
        }
        internal OffsetLapUpdateType OffsetLapUpdate { get; private set; } = OffsetLapUpdateType.None;


        private int _lapAtOffsetLapUpdate = -1;
        private bool _isSplinePositionReset = false;

        private readonly static TimeSpan _LAP_GAP_VALUE = TimeSpan.FromSeconds(100_000);
        private readonly static TimeSpan _HALF_LAP_GAP_VALUE = TimeSpan.FromSeconds(_LAP_GAP_VALUE.TotalSeconds / 2);
        private Dictionary<CarClass, TimeSpan?> _splinePositionTimes = [];


        public CarData(Values values, Opponent rawData) {
            this.RawDataNew = rawData;

            var carInfo = values.GetCarInfo(this.RawDataNew.CarName);
            if (carInfo == null) {
                DynLeaderboardsPlugin.LogWarn($"Car info not found for {this.RawDataNew.CarName}. Static car info (like class, manufacturer etc) may be missing or incorrect.");
            }
            this.CarClass = carInfo?.Class ?? CarClass.TryNew(this.RawDataNew.CarClass) ?? CarClass.Default;
            this.CarModel = carInfo?.Name ?? this.RawDataNew.CarName ?? "Unknown";
            this.CarManufacturer = carInfo?.Manufacturer ?? GetCarManufacturer(this.CarModel);

            this.CarClassColor = values.GetCarClassColor(this.CarClass)
                ?? TextBoxColor.TryNew(bg: this.RawDataNew.CarClassColor, fg: this.RawDataNew.CarClassTextColor)
                ?? new TextBoxColor(bg: "#FFFFFF", fg: "#000000");

            this.CarNumber = this.RawDataNew.CarNumber ?? "-1";

            this.TeamName = this.RawDataNew.TeamName;
            this.PositionOverall = this.RawDataNew!.Position;
            this.PositionInClass = this.RawDataNew.PositionInClass;

            if (DynLeaderboardsPlugin.Game.IsAcc) {
                var accRawData = (ACSharedMemory.Models.ACCOpponent)rawData;
                this.TeamCupCategory = ACCTeamCupCategoryToString(accRawData.ExtraData.CarEntry.CupCategory);
            } else {
                this.TeamCupCategory = TeamCupCategory.Default;
            }

            this.CarClassColor = values.GetTeamCupCategoryColor(this.TeamCupCategory)
                ?? new TextBoxColor(bg: "#FFFFFF", fg: "#000000");

            this.UpdateIndependent(values, rawData);
        }

        static TeamCupCategory ACCTeamCupCategoryToString(byte cupCategory) {
            return cupCategory switch {
                0 => new TeamCupCategory("Overall"),
                1 => new TeamCupCategory("ProAm"),
                2 => new TeamCupCategory("Am"),
                3 => new TeamCupCategory("Silver"),
                4 => new TeamCupCategory("National"),
                _ => TeamCupCategory.Default
            };
        }

        static string GetCarManufacturer(string carModel) {
            // TODO: read from LUTs
            return carModel.Split(' ')[0];
        }

        /// <summary>
        /// Update data that is independent of other cars data.
        /// </summary>
        /// <param name="rawData"></param>
        public void UpdateIndependent(Values values, Opponent rawData) {
            this.RawDataOld = this.RawDataNew;
            this.RawDataNew = rawData;

            this.IsBestLapCarOverall = false;
            this.IsBestLapCarInClass = false;

            this.Laps.Update((this.RawDataNew.CurrentLap ?? 1) - 1);
            this.IsNewLap = this.Laps.New > this.Laps.Old;

            if (this.RawDataNew.IsCarInPit) {
                this.Location.Update(CarLocation.PitBox);
            } else if (this.RawDataNew.IsCarInPitLane) {
                this.Location.Update(CarLocation.Pitlane);
            } else {
                this.Location.Update(CarLocation.Track);
            }

            this.IsInPitLane = this.Location.New == CarLocation.Pitlane || this.Location.New == CarLocation.PitBox;
            this.ExitedPitLane = this.Location.New == CarLocation.Track && this.Location.Old == CarLocation.Pitlane;
            this.EnteredPitLane = this.Location.New == CarLocation.Pitlane && this.Location.Old == CarLocation.Track;
            this.PitCount = this.RawDataNew.PitCount ?? 0;
            this.PitTimeLast = this.RawDataNew.PitLastDuration ?? TimeSpan.Zero;
            this.IsCurrentLapOutLap = (this.RawDataNew.PitOutAtLap ?? -1) == this.Laps.New + 1;
            this.IsCurrentLapInLap = (this.RawDataNew.PitEnterAtLap ?? -1) == this.Laps.New + 1;

            this.PositionInClassStart = this.RawDataNew.StartPositionClass;
            this.PositionOverallStart = this.RawDataNew.StartPosition;

            var newSplinePos = this.RawDataNew.TrackPositionPercent ?? throw new System.Exception("TrackPositionPercent is null");
            newSplinePos += values.TrackData!.SplinePosOffset;
            if (newSplinePos > 1) {
                newSplinePos -= 1;
            }
            this._isSplinePositionReset = newSplinePos < 0.1 && this.SplinePosition > 0.9;
            this.SplinePosition = newSplinePos;
            this.TotalSplinePosition = this.Laps.New + this.SplinePosition;

            this.CurrentLapTime = this.RawDataNew.CurrentLapTime ?? TimeSpan.Zero;
            this.MaxSpeed = Math.Max(this.MaxSpeed, this.RawDataNew.Speed ?? 0.0);

            this.UpdateDrivers(values, rawData);

            if (this.IsNewLap) {
                Debug.Assert(this.CurrentDriver != null, "Current driver shouldn't be null since someone had to finish this lap.");
                var currentDriver = this.CurrentDriver!;
                currentDriver.TotalLaps += 1;
            }

            if (this.RawDataNew.LastLapTime != this.RawDataOld.LastLapTime) {
                // Lap time end position may be offset with lap or spline position reset point.
                this.LastLap = new Lap(this.RawDataNew.LastLapSectorTimes, this.Laps.New, this.CurrentDriver!) {
                    IsValid = this.IsCurrentLapValid,
                    IsOutLap = this.IsCurrentLapOutLap,
                    IsInLap = this.IsCurrentLapInLap,
                };
                DynLeaderboardsPlugin.LogInfo($"[{this.Id}] new last lap: {this.LastLap.Time}");

                var maybeBestLap = this.RawDataNew.BestLapSectorTimes;
                if (maybeBestLap != null) {
                    Debug.Assert(this.CurrentDriver != null, "Current driver shouldn't be null since someone had to finish this lap.");
                    var currentDriver = this.CurrentDriver!;
                    var maybeBestLapTime = maybeBestLap.GetLapTime();
                    if (this.BestLap?.Time == null || (maybeBestLapTime != null && maybeBestLapTime < this.BestLap.Time)) {
                        this.BestLap = new Lap(maybeBestLap!, this.Laps.New, this.CurrentDriver!);
                        currentDriver.BestLap = this.BestLap; // If it's car's best lap, it must also be the drivers
                        DynLeaderboardsPlugin.LogInfo($"[{this.Id}] best lap: {this.BestLap.Time}");
                    } else if (currentDriver!.BestLap == null || (maybeBestLapTime != null && maybeBestLapTime < currentDriver.BestLap.Time)) {
                        currentDriver!.BestLap = new Lap(maybeBestLap!, this.Laps.New, currentDriver!);
                        DynLeaderboardsPlugin.LogInfo($"[{this.Id}] best lap for driver '{currentDriver.FullName}': {this.BestLap.Time}");
                    }
                }
            }

            if (values.Session.IsRace) {
                this.HandleJumpToPits(values.Session.SessionType);
                this.CheckForCrossingStartLine(values.Session.SessionPhase);
            }

            this.UpdatePitInfo();
            this.UpdateStintInfo(values.Session);

            this.HandleOffsetLapUpdates();
        }

        void UpdateDrivers(Values values, Opponent rawData) {
            if (DynLeaderboardsPlugin.Game.IsAcc) {
                // ACC has more driver info than generic SimHub interface
                var accOpponent = (ACSharedMemory.Models.ACCOpponent)rawData;
                var realtimeCarUpdate = accOpponent.ExtraData;
                if (this.Drivers.Count == 0) {
                    foreach (var driver in realtimeCarUpdate.CarEntry.Drivers) {
                        this.Drivers.Add(new Driver(values, driver));
                    }
                } else {
                    // ACC driver name could be different from SimHub's full name
                    var currentRawDriver = realtimeCarUpdate.CarEntry.Drivers[realtimeCarUpdate.DriverIndex];
                    var currentDriverIndex = this.Drivers.FindIndex(d => d.FirstName == currentRawDriver.FirstName && d.LastName == currentRawDriver.LastName);
                    if (currentDriverIndex == 0) {
                        // OK, current driver is already first in list
                    } else if (currentDriverIndex == -1) {
                        this.Drivers.Insert(0, new Driver(values, currentRawDriver));
                    } else {
                        // move current driver to the front
                        this.Drivers.MoveElementAt(currentDriverIndex, 0);
                    }
                }
            } else {
                var currentDriverIndex = this.Drivers.FindIndex(d => d.FullName == this.RawDataNew.Name);
                if (currentDriverIndex == 0) {
                    // OK, current driver is already first in list
                } else if (currentDriverIndex == -1) {
                    this.Drivers.Insert(0, new Driver(values, this.RawDataNew));
                } else {
                    // move current driver to the front
                    this.Drivers.MoveElementAt(currentDriverIndex, 0);
                }
            }
        }

        void HandleJumpToPits(SessionType sessionType) {
            Debug.Assert(sessionType == SessionType.Race);
            if (!this.IsFinished // It's okay to jump to the pits after finishing
                && this.Location.Old == CarLocation.Track
                && this.IsInPitLane
            ) {
                DynLeaderboardsPlugin.LogInfo($"[{this.Id}] jumped to pits");
                this.JumpedToPits = true;
            }

            if (this.JumpedToPits && !this.IsInPitLane) {
                DynLeaderboardsPlugin.LogInfo($"[{this.Id}] jumped to pits cleared.");
                this.JumpedToPits = false;
            }
        }

        void CheckForCrossingStartLine(SessionPhase sessionPhase) {
            // Initial update before the start of the race
            if (sessionPhase == SessionPhase.PreSession
                && !this._isHasCrossedStartLineSet
                && this.HasCrossedStartLine
                && this.SplinePosition > 0.5
                && this.Laps.New == 0
            ) {
                DynLeaderboardsPlugin.LogInfo($"[{this.Id}] has not crossed the start line");
                this.HasCrossedStartLine = false;
                this._isHasCrossedStartLineSet = true;
            }

            if (!this.HasCrossedStartLine && (this._isSplinePositionReset || this.ExitedPitLane)) {
                DynLeaderboardsPlugin.LogInfo($"[{this.Id}] crossed the start line");
                this.HasCrossedStartLine = true;
            }
        }

        void HandleOffsetLapUpdates() {
            // Check for offset lap update
            if (this.OffsetLapUpdate == OffsetLapUpdateType.None
                && this.IsNewLap
                && this.SplinePosition > 0.9
            ) {
                this.OffsetLapUpdate = OffsetLapUpdateType.LapBeforeSpline;
                this._lapAtOffsetLapUpdate = this.Laps.New;
                DynLeaderboardsPlugin.LogInfo($"Offset lap update [{this.Id}]: {this.OffsetLapUpdate}: sp={this.SplinePosition}, oldLap={this.Laps.Old}, newLap={this.Laps.New}");
            } else if (this.OffsetLapUpdate == OffsetLapUpdateType.None
                            && this._isSplinePositionReset
                            && this.Laps.New != this._lapAtOffsetLapUpdate // Remove double detection with above
                            && this.Laps.New == this.Laps.Old
                            && this.HasCrossedStartLine
                ) {
                this.OffsetLapUpdate = OffsetLapUpdateType.SplineBeforeLap;
                this._lapAtOffsetLapUpdate = this.Laps.New;
                DynLeaderboardsPlugin.LogInfo($"Offset lap update [{this.Id}]: {this.OffsetLapUpdate}: sp={this.SplinePosition}, oldLap={this.Laps.Old}, newLap={this.Laps.New}");
            }

            if (this.OffsetLapUpdate == OffsetLapUpdateType.LapBeforeSpline) {
                if (this.SplinePosition < 0.9) {
                    this.OffsetLapUpdate = OffsetLapUpdateType.None;
                    this._lapAtOffsetLapUpdate = -1;
                    DynLeaderboardsPlugin.LogInfo($"Offset lap update fixed [{this.Id}]: {this.OffsetLapUpdate}: sp={this.SplinePosition}, oldLap={this.Laps.Old}, newLap={this.Laps.New}");
                }
            } else if (this.OffsetLapUpdate == OffsetLapUpdateType.SplineBeforeLap) {
                if (this.Laps.New != this._lapAtOffsetLapUpdate || (this.SplinePosition > 0.025 && this.SplinePosition < 0.9)) {
                    // Second condition is a fallback in case the lap actually shouldn't have been updated (eg at the start line, jumped to pits and then crossed the line in the pits)
                    this.OffsetLapUpdate = OffsetLapUpdateType.None;
                    this._lapAtOffsetLapUpdate = -1;
                    DynLeaderboardsPlugin.LogInfo($"Offset lap update fixed [{this.Id}]: {this.OffsetLapUpdate}: sp={this.SplinePosition}, oldLap={this.Laps.Old}, newLap={this.Laps.New}");
                }
            }
        }

        void UpdatePitInfo() {
            var pitEntryTime = this.RawDataNew.PitEnterAtTime;
            // Pit ended
            if (pitEntryTime != null && this.ExitedPitLane) {
                this.IsCurrentLapOutLap = true;
                this.TotalPitTime += this.PitTimeLast ?? TimeSpan.Zero;
                this.PitTimeCurrent = null;
            }

            if (pitEntryTime != null) {
                var time = DateTime.Now - pitEntryTime;
                this.PitTimeCurrent = time;
            }
        }

        void UpdateStintInfo(Session session) {
            if (this.IsNewLap && this.CurrentStintLaps != null) {
                this.CurrentStintLaps++;
            }

            // Stint started
            if (this.ExitedPitLane // Pitlane exit
                || (session.IsRace && session.IsSessionStart) // Race start
                || (this._stintStartTime == null && this.Location.New == CarLocation.Track && session.SessionPhase != SessionPhase.PreSession) // We join/start SimHub mid session
            ) {
                this._stintStartTime = DateTime.Now;
                this.CurrentStintLaps = 0;
            }

            // Stint ended
            if (this.EnteredPitLane && this._stintStartTime != null) {
                this.LastStintTime = DateTime.Now - this._stintStartTime;
                this.CurrentDriver!.OnStintEnd(this.LastStintTime.Value);
                this.LastStintLaps = this.CurrentStintLaps;
                this._stintStartTime = null;
                this.CurrentStintTime = null;
                this.CurrentStintLaps = null;
            }

            if (this._stintStartTime != null) {
                this.CurrentStintTime = DateTime.Now - this._stintStartTime;
            }
        }

        /// <summary>
        /// Update data that requires that other cars have already received the basic update.
        /// 
        /// This includes for example relative spline positions, gaps and lap time deltas.
        /// </summary>
        /// <param name="focusedCar"></param>
        public void UpdateDependsOnOthers(
            Values values,
            CarData? focusedCar,
            CarData? overallBestLapCar,
            CarData? classBestLapCar,
            CarData? cupBestLapCar,
            CarData leaderCar,
            CarData classLeaderCar,
            CarData cupLeaderCar,
            CarData? carAhead,
            CarData? carAheadInClass,
            CarData? carAheadInCup
        ) {
            if (overallBestLapCar == this) {
                this.IsBestLapCarOverall = true;
                this.IsBestLapCarInClass = true;
            } else if (classBestLapCar == this) {
                this.IsBestLapCarInClass = true;
            }

            if (this.IsFocused) {
                this.RelativeSplinePositionToFocusedCar = 0;
                this.GapToFocusedTotal = TimeSpan.Zero;
                this.RelativeOnTrackLapDiff = RelativeLapDiff.SAME_LAP;
            } else if (focusedCar != null) {
                this.RelativeSplinePositionToFocusedCar = this.CalculateRelativeSplinePosition(focusedCar);
            }

            if (values.IsFirstFinished && this.IsNewLap) {
                this.IsFinished = true;
                this.FinishTime = DateTime.Now;
            }

            this.BestLap?.CalculateDeltas(
                thisCar: this,
                overallBestLapCar: overallBestLapCar,
                classBestLapCar: classBestLapCar,
                cupBestLapCar: cupBestLapCar,
                leaderCar: leaderCar,
                classLeaderCar: classLeaderCar,
                cupLeaderCar: cupLeaderCar,
                focusedCar: focusedCar,
                carAhead: carAhead,
                carAheadInClass: carAheadInClass,
                carAheadInCup: carAheadInCup
            );

            this.LastLap?.CalculateDeltas(
                thisCar: this,
                overallBestLapCar: overallBestLapCar,
                classBestLapCar: classBestLapCar,
                cupBestLapCar: cupBestLapCar,
                leaderCar: leaderCar,
                classLeaderCar: classLeaderCar,
                cupLeaderCar: cupLeaderCar,
                focusedCar: focusedCar,
                carAhead: carAhead,
                carAheadInClass: carAheadInClass,
                carAheadInCup: carAheadInCup
            );

            this.SetGaps(
                focusedCar: focusedCar,
                leaderCar: leaderCar,
                classLeaderCar: classLeaderCar,
                cupLeaderCar: cupLeaderCar,
                carAhead: carAhead,
                carAheadInClass: carAheadInClass,
                carAheadInCup: carAheadInCup,
                carAheadOnTrack: carAhead,
                trackData: values.TrackData,
                session: values.Session
            );

        }

        public void SetOverallPosition(int overall) {
            Debug.Assert(overall > 0);
            this.PositionOverall = overall;
            this.IndexOverall = overall - 1;
        }


        public void SetClassPosition(int cls) {
            Debug.Assert(cls > 0);
            this.PositionInClass = cls;
            this.IndexClass = cls - 1;
        }

        void SetRelLapDiff(CarData focusedCar) {
            if (this.GapToFocusedTotal == null) {
                if (this.Laps.New < focusedCar.Laps.New) {
                    this.RelativeOnTrackLapDiff = RelativeLapDiff.BEHIND;
                } else if (this.Laps.New > focusedCar.Laps.New) {
                    this.RelativeOnTrackLapDiff = RelativeLapDiff.AHEAD;
                } else {
                    if (this.GapToFocusedOnTrack > TimeSpan.Zero) {
                        if (this.PositionOverall > focusedCar.PositionOverall) {
                            this.RelativeOnTrackLapDiff = RelativeLapDiff.BEHIND;
                        } else {
                            this.RelativeOnTrackLapDiff = RelativeLapDiff.SAME_LAP;
                        }
                    } else {
                        if (this.PositionOverall < focusedCar.PositionOverall) {
                            this.RelativeOnTrackLapDiff = RelativeLapDiff.AHEAD;
                        } else {
                            this.RelativeOnTrackLapDiff = RelativeLapDiff.SAME_LAP;
                        }
                    }
                }
            } else if (this.GapToFocusedTotal > _LAP_GAP_VALUE) {
                this.RelativeOnTrackLapDiff = RelativeLapDiff.AHEAD;
            } else if (this.GapToFocusedTotal < _HALF_LAP_GAP_VALUE) {
                this.RelativeOnTrackLapDiff = RelativeLapDiff.SAME_LAP;
                if (this.GapToFocusedOnTrack > TimeSpan.Zero) {
                    if (this.PositionOverall > focusedCar.PositionOverall) {
                        this.RelativeOnTrackLapDiff = RelativeLapDiff.BEHIND;
                    } else {
                        this.RelativeOnTrackLapDiff = RelativeLapDiff.SAME_LAP;
                    }
                } else {
                    if (this.PositionOverall < focusedCar.PositionOverall) {
                        this.RelativeOnTrackLapDiff = RelativeLapDiff.AHEAD;
                    } else {
                        this.RelativeOnTrackLapDiff = RelativeLapDiff.SAME_LAP;
                    }
                }
            } else {
                this.RelativeOnTrackLapDiff = RelativeLapDiff.BEHIND;
            }
        }

        /// <summary>
        /// Calculates relative spline position from `this` to <paramref name="otherCar"/>.
        ///
        /// Car will be shown ahead if it's ahead by less than half a lap, otherwise it's behind.
        /// If result is positive then `this` is ahead of <paramref name="otherCar"/>, if negative it's behind.
        /// </summary>
        /// <returns>
        /// Value in [-0.5, 0.5] or `null` if the result cannot be calculated.
        /// </returns>
        /// <param name="otherCar"></param>
        /// <returns></returns>
        public double CalculateRelativeSplinePosition(CarData otherCar) {
            return CalculateRelativeSplinePosition(this.SplinePosition, otherCar.SplinePosition);
        }

        /// <summary>
        /// Calculates relative spline position of from <paramref name="fromPos"/> to <paramref name="toPos"/>.
        ///
        /// Position will be shown ahead if it's ahead by less than half a lap, otherwise it's behind.
        /// If result is positive then `to` is ahead of `from`, if negative it's behind.
        /// </summary>
        /// <param name="toPos"></param>
        /// <param name="fromPos"></param>
        /// <returns>
        /// Value in [-0.5, 0.5].
        /// </returns>
        public static double CalculateRelativeSplinePosition(double toPos, double fromPos) {
            var relSplinePos = toPos - fromPos;
            if (relSplinePos > 0.5) {
                // `to` is more than half a lap ahead, so technically it's closer from behind.
                // Take one lap away to show it behind `from`.
                relSplinePos -= 1.0;
            } else if (relSplinePos < -0.5) {
                // `to` is more than half a lap behind, so it's in front.
                // Add one lap to show it in front of us.
                relSplinePos += 1.0;
            }
            return relSplinePos;
        }

        void SetGaps(
            CarData? focusedCar,
            CarData leaderCar,
            CarData classLeaderCar,
            CarData cupLeaderCar,
            CarData? carAhead,
            CarData? carAheadInClass,
            CarData? carAheadInCup,
            CarData? carAheadOnTrack,
            TrackData? trackData,
            Session session
        ) {

            this._splinePositionTimes.Clear();

            // Freeze gaps until all is in order again, fixes gap suddenly jumping to larger values as spline positions could be out of sync
            if (trackData != null && this.OffsetLapUpdate == OffsetLapUpdateType.None) {
                if (focusedCar != null && focusedCar.OffsetLapUpdate == OffsetLapUpdateType.None) {
                    this.GapToFocusedOnTrack = CalculateOnTrackGap(this, focusedCar, trackData);
                }

                if (carAheadOnTrack?.OffsetLapUpdate == OffsetLapUpdateType.None) {
                    this.GapToAheadOnTrack = CalculateOnTrackGap(carAheadOnTrack, this, trackData);
                }
            }

            if (session.IsRace) {
                // Use time gaps on track
                // We update the gap only if CalculateGap returns a proper value because we don't want to update the gap if one of the cars has finished.
                // That would result in wrong gaps. We keep the gaps at the last valid value and update once both cars have finished.

                // Freeze gaps until all is in order again, fixes gap suddenly jumping to larger values as spline positions could be out of sync
                if (trackData != null && this.OffsetLapUpdate == OffsetLapUpdateType.None) {
                    SetGap(this, leaderCar, leaderCar, this.GapToLeader, x => this.GapToLeader = x);
                    SetGap(this, classLeaderCar, classLeaderCar, this.GapToClassLeader, x => this.GapToClassLeader = x);
                    SetGap(this, cupLeaderCar, cupLeaderCar, this.GapToCupLeader, x => this.GapToCupLeader = x);
                    SetGap(focusedCar, this, focusedCar, this.GapToFocusedTotal, x => this.GapToFocusedTotal = x);
                    SetGap(this, carAhead, carAhead, this.GapToAhead, x => this.GapToAhead = x);
                    SetGap(this, carAheadInClass, carAheadInClass, this.GapToAheadInClass, x => this.GapToAheadInClass = x);
                    SetGap(this, carAheadInCup, carAheadInCup, this.GapToAheadInCup, x => this.GapToAheadInCup = x);

                    void SetGap(CarData? from, CarData? to, CarData? other, TimeSpan? currentGap, Action<TimeSpan?> setGap) {
                        if (from == null || to == null) {
                            setGap(null);
                        } else if (other?.OffsetLapUpdate == OffsetLapUpdateType.None) {
                            setGap(CalculateGap(from, to, trackData));
                        }
                    }

                    if (focusedCar != null && focusedCar.OffsetLapUpdate == OffsetLapUpdateType.None) {
                        this.SetRelLapDiff(focusedCar);
                    }
                }
            } else {
                // Use best laps to calculate gaps
                var thisBestLap = this.BestLap?.Time;
                if (thisBestLap == null) {
                    this.GapToLeader = null;
                    this.GapToClassLeader = null;
                    this.GapToCupLeader = null;
                    this.GapToFocusedTotal = null;
                    this.GapToAheadInClass = null;
                    this.GapToAheadInCup = null;
                    this.GapToAhead = null;
                    return;
                }

                this.GapToLeader = CalculateBestLapDelta(leaderCar);
                this.GapToClassLeader = CalculateBestLapDelta(classLeaderCar);
                this.GapToCupLeader = CalculateBestLapDelta(cupLeaderCar);
                this.GapToFocusedTotal = CalculateBestLapDelta(focusedCar);
                this.GapToAhead = CalculateBestLapDelta(carAhead);
                this.GapToAheadInClass = CalculateBestLapDelta(carAheadInClass);
                this.GapToAheadInCup = CalculateBestLapDelta(carAheadInCup);

                TimeSpan? CalculateBestLapDelta(CarData? to) {
                    var toBest = to?.BestLap?.Time;
                    return toBest != null ? thisBestLap - toBest : null;
                }
            }
        }

        /// <summary>
        /// Calculates gap between two cars.
        /// </summary>
        /// <returns>
        /// The gap in seconds or laps with respect to the <paramref name="from">.
        /// It is positive if <paramref name="to"> is ahead of <paramref name="from"> and negative if behind.
        /// If the gap is larger than a lap we only return the lap part (1lap, 2laps) and add 100_000 to the value to differentiate it from gap on the same lap.
        /// For example 100_002 means that <paramref name="to"> is 2 laps ahead whereas result 99_998 means it's 2 laps behind.
        /// If the result couldn't be calculated it returns <c>double.NaN</c>.
        /// </returns>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static TimeSpan? CalculateGap(CarData from, CarData to, TrackData trackData) {
            if (from.Id == to.Id
                || !to.HasCrossedStartLine
                || !from.HasCrossedStartLine
                || from.OffsetLapUpdate != OffsetLapUpdateType.None
                || to.OffsetLapUpdate != OffsetLapUpdateType.None
            ) {
                return null;
            }

            var flaps = from.Laps.New;
            var tlaps = to.Laps.New;

            // If one of the cars jumped to pits there is no correct way to calculate the gap
            if (flaps == tlaps && (from.JumpedToPits || to.JumpedToPits)) {
                return null;
            }

            if (from.IsFinished && to.IsFinished) {
                if (flaps == tlaps) {
                    // If there IsFinished is set, FinishTime must also be set
                    return from.FinishTime - to.FinishTime;
                } else {
                    return TimeSpan.FromSeconds(tlaps - flaps) + _LAP_GAP_VALUE;
                }
            }

            // Fixes wrong gaps after finish on cars that haven't finished and are in pits.
            // Without this the gap could be off by one lap from the gap calculated from completed laps.
            // This is correct if the session is not finished as you could go out and complete that lap.
            // If session has finished you cannot complete that lap.
            if (tlaps != flaps &&
                ((to.IsFinished && !from.IsFinished && from.IsInPitLane)
                || (from.IsFinished && !to.IsFinished && to.IsInPitLane))
            ) {
                return TimeSpan.FromSeconds(tlaps - flaps) + _LAP_GAP_VALUE;
            }

            var distBetween = to.TotalSplinePosition - from.TotalSplinePosition; // Negative if 'to' is behind
            if (distBetween <= -1) { // 'to' is more than a lap behind of 'from'
                return TimeSpan.FromSeconds(Math.Ceiling(distBetween)) + _LAP_GAP_VALUE;
            } else if (distBetween >= 1) { // 'to' is more than a lap ahead of 'from'
                return TimeSpan.FromSeconds(Math.Floor(distBetween)) + _LAP_GAP_VALUE;
            } else {
                if (from.IsFinished
                    || to.IsFinished
                    || trackData == null
                ) {
                    return null;
                }

                // TrackData is passed from Values, Values never stores TrackData without LapInterpolators
                var toInterp = trackData.LapInterpolators?.GetValueOr(to.CarClass, null);
                var fromInterp = trackData.LapInterpolators?.GetValueOr(from.CarClass, null);
                if (toInterp == null && fromInterp == null) {
                    // lap data is not available, use naive distance based calculation
                    return CalculateNaiveGap(distBetween, trackData);
                }

                TimeSpan? gap;
                // At least one toInterp or fromInterp must be not null, because of the above check
                (LapInterpolator interp, var cls) = fromInterp != null ? (toInterp!, to.CarClass) : (fromInterp!, from.CarClass);
                if (distBetween > 0) { // `to` is ahead of `from`
                    gap = CalculateGapBetweenPos(from.GetSplinePosTime(cls, trackData), to.GetSplinePosTime(cls, trackData), interp.LapTime);
                } else { // `to` is behind of `from`
                    gap = -CalculateGapBetweenPos(to.GetSplinePosTime(cls, trackData), from.GetSplinePosTime(cls, trackData), interp.LapTime);
                }
                return gap;
            }
        }

        public static TimeSpan? CalculateOnTrackGap(CarData from, CarData to, TrackData trackData) {
            if (from.Id == to.Id
                 || from.OffsetLapUpdate != OffsetLapUpdateType.None
                 || to.OffsetLapUpdate != OffsetLapUpdateType.None
                 || trackData == null
             ) {
                return null;
            }

            var fromPos = from.SplinePosition;
            var toPos = to.SplinePosition;
            var relativeSplinePos = CalculateRelativeSplinePosition(fromPos, toPos);

            // TrackData is passed from Values, Values never stores TrackData without LapInterpolators
            var toInterp = trackData.LapInterpolators?.GetValueOr(to.CarClass, null);
            var fromInterp = trackData.LapInterpolators?.GetValueOr(from.CarClass, null);
            if (toInterp == null && fromInterp == null) {
                // lap data is not available, use naive distance based calculation
                return CalculateNaiveGap(relativeSplinePos, trackData);
            }

            TimeSpan? gap;
            // At least one toInterp or fromInterp must be not null, because of the above check
            (LapInterpolator interp, var cls) = toInterp != null ? (toInterp!, to.CarClass) : (fromInterp!, from.CarClass);
            if (relativeSplinePos < 0) {
                gap = -CalculateGapBetweenPos(from.GetSplinePosTime(cls, trackData), to.GetSplinePosTime(cls, trackData), interp.LapTime);
            } else {
                gap = CalculateGapBetweenPos(to.GetSplinePosTime(cls, trackData), from.GetSplinePosTime(cls, trackData), interp.LapTime);
            }
            return gap;
        }

        private static TimeSpan CalculateNaiveGap(double splineDist, TrackData trackData) {
            var dist = splineDist * trackData.LengthMeters;
            // use avg speed of 50m/s (180km/h)
            // we could use actual speeds of the cars
            // but the gap will fluctuate either way so I don't think it makes things better.
            // This also avoid the question of which speed to use (faster, slower, average)
            // and what happens if either car is standing (eg speed is 0 and we would divide by 0).
            // It's an just in case backup anyway, so most of the times it should never even be reached.7654
            return TimeSpan.FromSeconds(dist / 50);
        }

        /// <summary>
        /// Calculates the gap in seconds from <paramref name="start"/> to <paramref name="end"/>.
        /// </summary>
        /// <returns>Non-negative value</returns>
        public static TimeSpan CalculateGapBetweenPos(TimeSpan start, TimeSpan end, TimeSpan lapTime) {
            if (end < start) { // Ahead is on another lap, gap is time from `start` to end of the lap, and then to `end`
                return lapTime - start + end;
            } else { // We must be on the same lap, gap is time from `start` to reach `end`
                return end - start;
            }
        }

        /// <summary>
        /// Calculates expected lap time for <paramref name="cls"> class car at the position of <c>this</c> car.
        /// </summary>
        /// <returns>
        /// Lap time in seconds or <c>-1.0</c> if it cannot be calculated.
        /// </returns>>
        /// <param name="cls"></param>
        /// <returns></returns>
        private TimeSpan GetSplinePosTime(CarClass cls, TrackData trackData) {
            // Same interpolated value is needed multiple times in one update, thus cache results.
            var pos = this._splinePositionTimes.GetValueOr(cls, null);
            if (pos != null) {
                return pos.Value;
            }

            // TrackData is passed from Values, Values never stores TrackData without LapInterpolators
            var interp = trackData.LapInterpolators.GetValueOr(cls, null);
            if (interp != null) {
                var result = interp.Interpolate(this.SplinePosition);
                this._splinePositionTimes[cls] = result;
                return result;
            } else {
                return TimeSpan.FromSeconds(-1.0);
            }
        }
    }

    public class Driver {
        public string? FirstName { get; private set; }
        public string? LastName { get; private set; }
        public string ShortName { get; private set; }
        public string FullName { get; private set; }
        public string InitialPlusLastName { get; private set; }
        public string? Initials { get; private set; }

        public DriverCategory Category { get; private set; } = DriverCategory.Default;
        public string Nationality { get; private set; } = "Unknown";
        public int TotalLaps { get; internal set; } = 0;
        public Lap? BestLap { get; internal set; } = null;
        public TextBoxColor CategoryColor { get; internal set; }

        private TimeSpan _totalDrivingTime;

        public Driver(Values v, Opponent o) {
            this.FullName = o.Name;
            this.ShortName = o.Initials;
            this.InitialPlusLastName = o.ShortName;

            this.CategoryColor = v.GetDriverCategoryColor(this.Category) ?? DefCategoryColor();
        }

        public Driver(Values v, ksBroadcastingNetwork.Structs.DriverInfo driver) {
            this.FirstName = driver.FirstName;
            this.LastName = driver.LastName;
            this.ShortName = driver.ShortName;
            this.Category = ACCDriverCategoryToPrettyString(driver.Category);
            this.Nationality = ACCNationalityToPrettyString(driver.Nationality);

            this.FullName = this.FirstName + " " + this.LastName;
            this.InitialPlusLastName = this.CreateInitialPlusLastNameACC();
            this.Initials = this.CreateInitialsACC();

            this.CategoryColor = v.GetDriverCategoryColor(this.Category) ?? DefCategoryColor();
        }

        private static TextBoxColor DefCategoryColor() {
            return new TextBoxColor(fg: "#FFFFFF", bg: "#000000");
        }

        internal void OnStintEnd(TimeSpan lastStintTime) {
            this._totalDrivingTime += lastStintTime;
        }

        internal TimeSpan GetTotalDrivingTime(bool isDriving = false, TimeSpan? currentStintTime = null) {
            if (isDriving && currentStintTime != null) {
                return this._totalDrivingTime + currentStintTime.Value;
            }

            return this._totalDrivingTime;
        }

        private string CreateInitialPlusLastNameACC() {
            if (this.FirstName == "") {
                return $"{this.LastName}";
            }
            return $"{this.FirstName![0]}. {this.LastName}";
        }

        private string CreateInitialsACC() {
            if (this.FirstName != "" && this.LastName != "") {
                return $"{this.FirstName![0]}{this.LastName![0]}";
            } else if (this.FirstName == "" && this.LastName != "") {
                return $"{this.LastName![0]}";
            } else if (this.FirstName != "" && this.LastName == "") {
                return $"{this.FirstName![0]}";
            } else {
                return "";
            }
        }

        private static DriverCategory ACCDriverCategoryToPrettyString(ksBroadcastingNetwork.DriverCategory category) {
            return category switch {
                ksBroadcastingNetwork.DriverCategory.Platinum => new DriverCategory("Platinum"),
                ksBroadcastingNetwork.DriverCategory.Gold => new DriverCategory("Gold"),
                ksBroadcastingNetwork.DriverCategory.Silver => new DriverCategory("Silver"),
                ksBroadcastingNetwork.DriverCategory.Bronze => new DriverCategory("Bronze"),
                _ => DriverCategory.Default,
            };
        }

        private static string ACCNationalityToPrettyString(NationalityEnum nationality) {
            return nationality switch {
                NationalityEnum.Italy => "Italy",
                NationalityEnum.Germany => "Germany",
                NationalityEnum.France => "France",
                NationalityEnum.Spain => "Spain",
                NationalityEnum.GreatBritain => "Great Britain",
                NationalityEnum.Hungary => "Hungary",
                NationalityEnum.Belgium => "Belgium",
                NationalityEnum.Switzerland => "Switzerland",
                NationalityEnum.Austria => "Austria",
                NationalityEnum.Russia => "Russia",
                NationalityEnum.Thailand => "Thailand",
                NationalityEnum.Netherlands => "Netherlands",
                NationalityEnum.Poland => "Poland",
                NationalityEnum.Argentina => "Argentina",
                NationalityEnum.Monaco => "Monaco",
                NationalityEnum.Ireland => "Ireland",
                NationalityEnum.Brazil => "Brazil",
                NationalityEnum.SouthAfrica => "South Africa",
                NationalityEnum.PuertoRico => "Puerto Rico",
                NationalityEnum.Slovakia => "Slovakia",
                NationalityEnum.Oman => "Oman",
                NationalityEnum.Greece => "Greece",
                NationalityEnum.SaudiArabia => "Saudi Arabia",
                NationalityEnum.Norway => "Norway",
                NationalityEnum.Turkey => "Turkey",
                NationalityEnum.SouthKorea => "South Korea",
                NationalityEnum.Lebanon => "Lebanon",
                NationalityEnum.Armenia => "Armenia",
                NationalityEnum.Mexico => "Mexico",
                NationalityEnum.Sweden => "Sweden",
                NationalityEnum.Finland => "Finland",
                NationalityEnum.Denmark => "Denmark",
                NationalityEnum.Croatia => "Croatia",
                NationalityEnum.Canada => "Canada",
                NationalityEnum.China => "China",
                NationalityEnum.Portugal => "Portugal",
                NationalityEnum.Singapore => "Singapore",
                NationalityEnum.Indonesia => "Indonesia",
                NationalityEnum.USA => "USA",
                NationalityEnum.NewZealand => "New Zealand",
                NationalityEnum.Australia => "Australia",
                NationalityEnum.SanMarino => "San Marino",
                NationalityEnum.UAE => "UAE",
                NationalityEnum.Luxembourg => "Luxembourg",
                NationalityEnum.Kuwait => "Kuwait",
                NationalityEnum.HongKong => "Hong Kong",
                NationalityEnum.Colombia => "Colombia",
                NationalityEnum.Japan => "Japan",
                NationalityEnum.Andorra => "Andorra",
                NationalityEnum.Azerbaijan => "Azerbaijan",
                NationalityEnum.Bulgaria => "Bulgaria",
                NationalityEnum.Cuba => "Cuba",
                NationalityEnum.CzechRepublic => "Czech Republic",
                NationalityEnum.Estonia => "Estonia",
                NationalityEnum.Georgia => "Georgia",
                NationalityEnum.India => "India",
                NationalityEnum.Israel => "Israel",
                NationalityEnum.Jamaica => "Jamaica",
                NationalityEnum.Latvia => "Latvia",
                NationalityEnum.Lithuania => "Lithuania",
                NationalityEnum.Macau => "Macau",
                NationalityEnum.Malaysia => "Malaysia",
                NationalityEnum.Nepal => "Nepal",
                NationalityEnum.NewCaledonia => "New Caledonia",
                NationalityEnum.Nigeria => "Nigeria",
                NationalityEnum.NorthernIreland => "Northern Ireland",
                NationalityEnum.PapuaNewGuinea => "Papua New Guinea",
                NationalityEnum.Philippines => "Philippines",
                NationalityEnum.Qatar => "Qatar",
                NationalityEnum.Romania => "Romania",
                NationalityEnum.Scotland => "Scotland",
                NationalityEnum.Serbia => "Serbia",
                NationalityEnum.Slovenia => "Slovenia",
                NationalityEnum.Taiwan => "Taiwan",
                NationalityEnum.Ukraine => "Ukraine",
                NationalityEnum.Venezuela => "Venezuela",
                NationalityEnum.Wales => "Wales",
                _ => "Unknown"
            };
        }
    }

    public class Lap {
        public TimeSpan? Time { get; private set; }
        public TimeSpan? S1Time { get; private set; }
        public TimeSpan? S2Time { get; private set; }
        public TimeSpan? S3Time { get; private set; }

        public bool IsOutLap { get; internal set; } = false;
        public bool IsInLap { get; internal set; } = false;
        public bool IsValid { get; internal set; } = true;

        public int LapNumber { get; private set; }
        public Driver Driver { get; private set; }


        public TimeSpan? DeltaToOwnBest { get; private set; }

        public TimeSpan? DeltaToOverallBest { get; private set; }
        public TimeSpan? DeltaToClassBest { get; private set; }
        public TimeSpan? DeltaToCupBest { get; private set; }
        public TimeSpan? DeltaToLeaderBest { get; private set; }
        public TimeSpan? DeltaToClassLeaderBest { get; private set; }
        public TimeSpan? DeltaToCupLeaderBest { get; private set; }
        public TimeSpan? DeltaToFocusedBest { get; private set; }
        public TimeSpan? DeltaToAheadBest { get; private set; }
        public TimeSpan? DeltaToAheadInClassBest { get; private set; }
        public TimeSpan? DeltaToAheadInCupBest { get; private set; }

        public TimeSpan? DeltaToLeaderLast { get; private set; }
        public TimeSpan? DeltaToClassLeaderLast { get; private set; }
        public TimeSpan? DeltaToCupLeaderLast { get; private set; }
        public TimeSpan? DeltaToFocusedLast { get; private set; }
        public TimeSpan? DeltaToAheadLast { get; private set; }
        public TimeSpan? DeltaToAheadInClassLast { get; private set; }
        public TimeSpan? DeltaToAheadInCupLast { get; private set; }

        public Lap(SectorTimes? sectorTimes, int lapNumber, Driver driver) {
            this.Time = sectorTimes?.GetLapTime();
            this.S1Time = sectorTimes?.GetSectorSplit(1);
            this.S2Time = sectorTimes?.GetSectorSplit(2);
            this.S3Time = sectorTimes?.GetSectorSplit(3);

            this.LapNumber = lapNumber;
            this.Driver = driver;
        }

        internal void CalculateDeltas(
            CarData thisCar,
            CarData? overallBestLapCar,
            CarData? classBestLapCar,
            CarData? cupBestLapCar,
            CarData leaderCar,
            CarData classLeaderCar,
            CarData cupLeaderCar,
            CarData? focusedCar,
            CarData? carAhead,
            CarData? carAheadInClass,
            CarData? carAheadInCup
        ) {
            if (this.Time == null) {
                return;
            }

            var overallBest = overallBestLapCar?.BestLap?.Time;
            var classBest = classBestLapCar?.BestLap?.Time;
            var cupBest = cupBestLapCar?.BestLap?.Time;
            var leaderBest = leaderCar?.BestLap?.Time;
            var classLeaderBest = classLeaderCar?.BestLap?.Time;
            var cupLeaderBest = cupLeaderCar?.BestLap?.Time;
            var focusedBest = focusedCar?.BestLap?.Time;
            var aheadBest = carAhead?.BestLap?.Time;
            var aheadInClassBest = carAheadInClass?.BestLap?.Time;
            var aheadInCupBest = carAheadInCup?.BestLap?.Time;

            if (overallBest != null) {
                this.DeltaToOverallBest = this.Time - overallBest;
            }

            if (classBest != null) {
                this.DeltaToClassBest = this.Time - classBest;
            }

            if (cupBest != null) {
                this.DeltaToCupBest = this.Time - cupBest;
            }

            if (leaderBest != null) {
                this.DeltaToLeaderBest = this.Time - leaderBest;
            }

            if (classLeaderBest != null) {
                this.DeltaToClassLeaderBest = this.Time - classLeaderBest;
            }

            if (cupLeaderBest != null) {
                this.DeltaToCupLeaderBest = this.Time - cupLeaderBest;
            }

            this.DeltaToFocusedBest = focusedBest != null ? this.Time - focusedBest : null;
            this.DeltaToAheadBest = aheadBest != null ? this.Time - aheadBest : null;
            this.DeltaToAheadInClassBest = aheadInClassBest != null ? this.Time - aheadInClassBest : null;
            this.DeltaToAheadInCupBest = aheadInCupBest != null ? this.Time - aheadInCupBest : null;

            var thisBest = thisCar.BestLap?.Time;
            if (thisBest != null) {
                this.DeltaToOwnBest = this.Time - thisBest;
            }

            var leaderLast = leaderCar?.LastLap?.Time;
            var classLeaderLast = classLeaderCar?.LastLap?.Time;
            var cupLeaderLast = cupLeaderCar?.LastLap?.Time;
            var focusedLast = focusedCar?.LastLap?.Time;
            var aheadLast = carAhead?.LastLap?.Time;
            var aheadInClassLast = carAheadInClass?.LastLap?.Time;
            var aheadInCupLast = carAheadInCup?.LastLap?.Time;

            if (leaderLast != null) {
                this.DeltaToLeaderLast = this.Time - leaderLast;
            }

            if (classLeaderLast != null) {
                this.DeltaToClassLeaderLast = this.Time - classLeaderLast;
            }

            if (cupLeaderLast != null) {
                this.DeltaToCupLeaderLast = this.Time - cupLeaderLast;
            }

            this.DeltaToFocusedLast = focusedLast != null ? this.Time - focusedLast : null;
            this.DeltaToAheadLast = aheadLast != null ? this.Time - aheadLast : null;
            this.DeltaToAheadInClassLast = aheadInClassLast != null ? this.Time - aheadInClassLast : null;
            this.DeltaToAheadInCupLast = aheadInCupLast != null ? this.Time - aheadInCupLast : null;
        }
    }

    public enum CarLocation {
        NONE = 0,
        Track = 1,
        Pitlane = 2,
        PitBox = 3,
    }

    public enum RelativeLapDiff {
        AHEAD = 1,
        SAME_LAP = 0,
        BEHIND = -1
    }


    public class NewOld<T> {
        public T New { get; private set; }
        public T Old { get; private set; }

        internal NewOld(T data) {
            this.New = data;
            this.Old = data;
        }

        internal void Update(T data) {
            this.Old = this.New;
            this.New = data;
        }
    }

    class CarInfo {
        public string? Name { get; private set; }
        public string? Manufacturer { get; private set; }
        public CarClass? Class { get; private set; }

        [JsonConstructor]
        public CarInfo(string? name, string? manufacturer, CarClass? @class) {
            this.Name = name;
            this.Manufacturer = manufacturer;
            this.Class = @class;
        }

        public void Merge(CarInfo other) {
            if (other.Name != null) {
                this.Name = other.Name;
            }

            if (other.Manufacturer != null) {
                this.Manufacturer = other.Manufacturer;
            }

            if (other.Class != null) {
                this.Class = other.Class;
            }
        }
    }

    [TypeConverter(typeof(CarClassTypeConverter))]
    public readonly struct CarClass(string cls) {
        private string _cls { get; } = cls;

        public static CarClass Default = new("");

        public static CarClass? TryNew(string? cls) {
            if (cls == null) {
                return null;
            }
            return new(cls);
        }

        public string AsString() {
            return this._cls;
        }

        public static bool operator ==(CarClass obj1, CarClass obj2) {
            return obj1._cls == obj2._cls;
        }

        public static bool operator !=(CarClass obj1, CarClass obj2) {
            return !(obj1 == obj2);
        }

        public override bool Equals(object obj) {
            return obj is CarClass other && this._cls == other._cls;
        }

        public override int GetHashCode() {
            return this._cls.GetHashCode();
        }

        public override string ToString() {
            return this._cls.ToString();
        }
    }

    class CarClassTypeConverter : TypeConverter {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) {
            return new CarClass((string)value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return destinationType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) {
            return ((CarClass)value).AsString();
        }
    }

    [TypeConverter(typeof(TeamCupCategoryTypeConverter))]
    public readonly struct TeamCupCategory(string cls) {
        private string _cls { get; } = cls;

        public static TeamCupCategory Default = new("Overall");

        public static TeamCupCategory? TryNew(string? cls) {
            if (cls == null) {
                return null;
            }
            return new(cls);
        }

        public string AsString() {
            return this._cls;
        }

        public static bool operator ==(TeamCupCategory obj1, TeamCupCategory obj2) {
            return obj1._cls == obj2._cls;
        }

        public static bool operator !=(TeamCupCategory obj1, TeamCupCategory obj2) {
            return !(obj1 == obj2);
        }

        public override bool Equals(object obj) {
            return obj is TeamCupCategory other && this._cls == other._cls;
        }

        public override int GetHashCode() {
            return this._cls.GetHashCode();
        }

        public override string ToString() {
            return this._cls.ToString();
        }
    }

    class TeamCupCategoryTypeConverter : TypeConverter {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) {
            return new TeamCupCategory((string)value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return destinationType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) {
            return ((TeamCupCategory)value).AsString();
        }
    }

    [TypeConverter(typeof(DriverCategoryTypeConverter))]
    public readonly struct DriverCategory(string cls) {
        private string _cls { get; } = cls;

        public static DriverCategory Default = new("Platinum");

        public static DriverCategory? TryNew(string? cls) {
            if (cls == null) {
                return null;
            }
            return new(cls);
        }

        public string AsString() {
            return this._cls;
        }

        public static bool operator ==(DriverCategory obj1, DriverCategory obj2) {
            return obj1._cls == obj2._cls;
        }

        public static bool operator !=(DriverCategory obj1, DriverCategory obj2) {
            return !(obj1 == obj2);
        }

        public override bool Equals(object obj) {
            return obj is DriverCategory other && this._cls == other._cls;
        }

        public override int GetHashCode() {
            return this._cls.GetHashCode();
        }

        public override string ToString() {
            return this._cls.ToString();
        }
    }

    class DriverCategoryTypeConverter : TypeConverter {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) {
            return new DriverCategory((string)value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return destinationType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) {
            return ((DriverCategory)value).AsString();
        }
    }
}
