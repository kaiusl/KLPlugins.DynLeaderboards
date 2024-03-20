using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using GameReaderCommon;

using KLPlugins.DynLeaderboards.Helpers;

namespace KLPlugins.DynLeaderboards.Car {

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

    public class CarData {

        public string CarClass { get; private set; }
        public string CarClassColor { get; private set; }
        public string CarClassTextColor { get; private set; }

        public string CarNumber { get; private set; }
        public string CarModel { get; private set; }
        public string? TeamName { get; private set; }

        public NewOld<CarLocation> Location { get; } = new(CarLocation.NONE);

        public NewOld<int> Laps { get; } = new(0);
        public double CurrentLapTime { get; private set; }
        public bool IsCurrentLapOutLap { get; private set; }
        public bool IsCurrentLapInLap { get; private set; }
        public bool IsCurrentLapValid { get; private set; }
        public Lap? LastLap { get; private set; }
        public Lap? BestLap { get; private set; }
        public SectorSplits BestSectors => this.RawDataNew.BestSectorSplits;

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
        public int PitCount { get; private set; }
        public double PitTimeLast { get; private set; }


        public double GapToLeader { get; private set; }
        public double GapToClassLeader { get; private set; }
        public double GapToFocusedTotal { get; private set; }
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
        public long? FinishTime { get; private set; } = null;

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


        public bool IsNewLap { get; private set; } = false;

        public CarData(Values values, Opponent rawData) {
            this.RawDataNew = rawData;
            this.UpdateIndependent(values, rawData);
            this.CarClass = this.RawDataNew.CarClass ?? "";
            this.CarClassColor = this.RawDataNew.CarClassColor ?? "#FFFFFF";
            this.CarClassTextColor = this.RawDataNew.CarClassTextColor ?? "#000000";
            this.CarNumber = this.RawDataNew.CarNumber ?? "-1";
            this.CarModel = this.RawDataNew.CarName ?? "Unknown";
            this.TeamName = this.RawDataNew.TeamName;
            this.PositionOverall = this.RawDataNew!.Position;
            this.PositionInClass = this.RawDataNew.PositionInClass;
        }

        /// <summary>
        /// Update data that is independent of other cars data.
        /// </summary>
        /// <param name="rawData"></param>
        public void UpdateIndependent(Values values, Opponent rawData) {
            this.RawDataOld = this.RawDataNew;
            this.RawDataNew = rawData;

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
            this.PitCount = this.RawDataNew.PitCount ?? 0;
            this.PitTimeLast = this.RawDataNew.PitLastDuration?.TotalSeconds ?? 0.0;
            this.IsCurrentLapOutLap = (this.RawDataNew.PitOutAtLap ?? -1) == this.Laps.New + 1;
            this.IsCurrentLapInLap = (this.RawDataNew.PitEnterAtLap ?? -1) == this.Laps.New + 1;

            this.PositionInClassStart = this.RawDataNew.StartPositionClass;
            this.PositionOverallStart = this.RawDataNew.StartPosition;

            var newSplinePos = this.RawDataNew.TrackPositionPercent ?? throw new System.Exception("TrackPositionPercent is null");
            this._isSplinePositionReset = newSplinePos < 0.1 && this.SplinePosition > 0.9;
            this.SplinePosition = newSplinePos;
            this.TotalSplinePosition = this.Laps.New + this.SplinePosition;

            this.CurrentLapTime = this.RawDataNew.CurrentLapTime?.TotalSeconds ?? 0.0;

            var currentDriverIndex = this.Drivers.FindIndex(d => d.FullName == this.RawDataNew.Name);
            if (currentDriverIndex == -1) {
                this.Drivers.Insert(0, new Driver(this.RawDataNew));
            } else if (currentDriverIndex == 0) {
                // OK, current driver is already first in list
            } else {
                // move current driver to the front
                this.Drivers.MoveElementAt(currentDriverIndex, 0);
            }

            if (this.IsNewLap) {
                Debug.Assert(this.CurrentDriver != null, "Current driver shouldn't be null since someone had to finish this lap.");
                this.LastLap = new Lap(this.RawDataNew.LastLapSectorTimes, this.Laps.New, this.CurrentDriver!) {
                    IsValid = this.IsCurrentLapValid,
                    IsOutLap = this.IsCurrentLapOutLap,
                    IsInLap = this.IsCurrentLapInLap,
                };

                var maybeBestLap = this.RawDataNew.BestLapSectorTimes;
                if (maybeBestLap != null) {
                    var maybeBestLapTime = maybeBestLap.GetLapTime()?.TotalSeconds;
                    if (this.BestLap?.Time == null || (maybeBestLapTime != null && maybeBestLapTime < this.BestLap.Time)) {
                        this.BestLap = new Lap(maybeBestLap!, this.Laps.New, this.CurrentDriver!);
                        DynLeaderboardsPlugin.LogInfo($"[{this.Id}] best lap: {this.BestLap.Time}");
                    }
                }
            }

            if (values.Session.IsRace) {
                this.HandleJumpToPits(values.Session.SessionType);
                this.CheckForCrossingStartLine(values.Session.SessionPhase);
            }

            this.HandleOffsetLapUpdates();
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
            CarData? classLeaderCar, // TODO: remove nullable
            CarData? cupLeaderCar, // TODO: remove nullable
            CarData? carAhead,
            CarData? carAheadInClass,
            CarData? carAheadInCup
        ) {
            if (this.IsFocused) {
                this.RelativeSplinePositionToFocusedCar = 0;
                this.GapToFocusedTotal = 0;
            } else if (focusedCar != null) {
                this.RelativeSplinePositionToFocusedCar = this.CalculateRelativeSplinePosition(focusedCar);
                // TODO: fix it to proper one
                this.GapToFocusedTotal = (this.RawDataNew.LapsToPlayer ?? 0) * 10000 + this.RawDataNew.GaptoPlayer ?? 0;
            }

            if (values.IsFirstFinished && this.IsNewLap) {
                this.IsFinished = true;
                this.FinishTime = DateTime.Now.Ticks;
            }

            this.GapToLeader = (this.RawDataNew.LapsToLeader ?? 0) * 10000 + this.RawDataNew.GaptoLeader ?? 0;
            this.GapToClassLeader = (this.RawDataNew.LapsToClassLeader ?? 0) * 10000 + this.RawDataNew.GaptoClassLeader ?? 0;

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
    }

    public class Driver {
        public string FullName { get; private set; }
        public string ShortName { get; private set; }
        public string InitialPlusLastName { get; private set; }

        public Driver(Opponent o) {
            this.FullName = o.Name;
            this.ShortName = o.Initials;
            this.InitialPlusLastName = o.ShortName;
        }
    }

    public class Lap {
        public double? Time { get; private set; }
        public double? S1Time { get; private set; }
        public double? S2Time { get; private set; }
        public double? S3Time { get; private set; }

        public bool IsOutLap { get; internal set; } = false;
        public bool IsInLap { get; internal set; } = false;
        public bool IsValid { get; internal set; } = true;

        public int LapNumber { get; private set; }
        public Driver Driver { get; private set; }


        public double? DeltaToOwnBest { get; private set; }

        public double? DeltaToOverallBest { get; private set; }
        public double? DeltaToClassBest { get; private set; }
        public double? DeltaToCupBest { get; private set; }
        public double? DeltaToLeaderBest { get; private set; }
        public double? DeltaToClassLeaderBest { get; private set; }
        public double? DeltaToCupLeaderBest { get; private set; }
        public double? DeltaToFocusedBest { get; private set; }
        public double? DeltaToAheadBest { get; private set; }
        public double? DeltaToAheadInClassBest { get; private set; }
        public double? DeltaToAheadInCupBest { get; private set; }

        public double? DeltaToLeaderLast { get; private set; }
        public double? DeltaToClassLeaderLast { get; private set; }
        public double? DeltaToCupLeaderLast { get; private set; }
        public double? DeltaToFocusedLast { get; private set; }
        public double? DeltaToAheadLast { get; private set; }
        public double? DeltaToAheadInClassLast { get; private set; }
        public double? DeltaToAheadInCupLast { get; private set; }

        public Lap(SectorTimes? sectorTimes, int lapNumber, Driver driver) {
            this.Time = sectorTimes?.GetLapTime()?.TotalSeconds;
            this.S1Time = sectorTimes?.GetSectorSplit(1)?.TotalSeconds;
            this.S2Time = sectorTimes?.GetSectorSplit(2)?.TotalSeconds;
            this.S3Time = sectorTimes?.GetSectorSplit(3)?.TotalSeconds;

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
}