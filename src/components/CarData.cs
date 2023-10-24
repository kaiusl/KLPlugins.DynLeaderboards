using System;
using System.Collections.Generic;
using System.Linq;

using KLPlugins.DynLeaderboards.Driver;
using KLPlugins.DynLeaderboards.ksBroadcastingNetwork;
using KLPlugins.DynLeaderboards.Realtime;
using KLPlugins.DynLeaderboards.Track;

namespace KLPlugins.DynLeaderboards.Car {

    internal class CarData {

        internal enum OffsetLapUpdateType {
            None = 0,
            LapBeforeSpline = 1,
            SplineBeforeLap = 2
        }

        // Information from CarInfo
        public ushort CarIndex { get; }

        public CarType CarModelType { get; internal set; }
        public CarClass CarClass { get; internal set; }
        public string TeamName { get; internal set; }
        public int RaceNumber { get; internal set; }
        public TeamCupCategory TeamCupCategory { get; internal set; }
        private int _currentDriverIndex { get; set; }
        public List<DriverData> Drivers { get; internal set; } = new List<DriverData>();
        public NationalityEnum TeamNationality { get; internal set; }
        public string CarClassColor => DynLeaderboardsPlugin.Settings.CarClassColors[this.CarClass];
        public string TeamCupCategoryColor => DynLeaderboardsPlugin.Settings.TeamCupCategoryColors[this.TeamCupCategory];
        public string TeamCupCategoryTextColor => DynLeaderboardsPlugin.Settings.TeamCupCategoryTextColors[this.TeamCupCategory];

        // RealtimeCarUpdates
        public RealtimeCarUpdate? NewData { get; private set; } = null;

        public RealtimeCarUpdate? OldData { get; private set; } = null;

        public int CurrentDriverIndex;
        public DriverData CurrentDriver => this.Drivers[this.CurrentDriverIndex];

        // ..BySplinePosition
        public double TotalSplinePosition { get; private set; } = 0.0;

        // Gaps
        public double? GapToLeader { get; private set; } = null;

        public double? GapToClassLeader { get; private set; } = null;
        public double? GapToFocusedTotal { get; private set; } = null;
        public double? GapToFocusedOnTrack { get; private set; } = null;

        public double? GapToAhead { get; private set; } = null;
        public double? GapToAheadInClass { get; internal set; } = null;
        public double? GapToAheadOnTrack { get; internal set; } = null;

        // Positions
        public int InClassPos { get; private set; } = -1;

        public int OverallPos { get; private set; } = -1;
        public int StartPos { get; private set; } = -1;
        public int StartPosInClass { get; private set; } = -1;

        // Pit info
        public int PitCount { get; private set; } = 0;

        public double? PitEntryTime { get; private set; } = null;
        public double TotalPitTime { get; private set; } = 0;
        public double? LastPitTime { get; private set; } = null;
        public double? CurrentTimeInPits { get; private set; } = null;
        public bool IsCurrentLapOutLap { get; private set; } = false;
        public bool? IsLastLapOutLap { get; private set; } = null;
        public bool IsCurrentLapInLap { get; private set; } = false;
        public bool? IsLastLapInLap { get; private set; } = null;

        // Stint info
        public double? LastStintTime { get; private set; } = null;

        public double? CurrentStintTime { get; private set; } = null;
        public int LastStintLaps { get; private set; } = 0;
        public int CurrentStintLaps { get; private set; } = 0;
        public double CurrentDriverTotalDrivingTime => this.CurrentDriver.GetTotalDrivingTime(true, this.CurrentStintTime);

        // Lap deltas
        public double? BestLapDeltaToOverallBest { get; private set; } = null;

        public double? BestLapDeltaToClassBest { get; private set; } = null;
        public double? BestLapDeltaToLeaderBest { get; private set; } = null;
        public double? BestLapDeltaToClassLeaderBest { get; private set; } = null;
        public double? BestLapDeltaToFocusedBest { get; private set; } = null;
        public double? BestLapDeltaToAheadBest { get; private set; } = null;
        public double? BestLapDeltaToAheadInClassBest { get; private set; } = null;

        public double? LastLapDeltaToOverallBest { get; private set; } = null;
        public double? LastLapDeltaToClassBest { get; private set; } = null;
        public double? LastLapDeltaToLeaderBest { get; private set; } = null;
        public double? LastLapDeltaToClassLeaderBest { get; private set; } = null;
        public double? LastLapDeltaToFocusedBest { get; private set; } = null;
        public double? LastLapDeltaToAheadBest { get; private set; } = null;
        public double? LastLapDeltaToAheadInClassBest { get; private set; } = null;
        public double? LastLapDeltaToOwnBest { get; private set; } = null;

        public double? LastLapDeltaToLeaderLast { get; private set; } = null;
        public double? LastLapDeltaToClassLeaderLast { get; private set; } = null;
        public double? LastLapDeltaToFocusedLast { get; private set; } = null;
        public double? LastLapDeltaToAheadLast { get; private set; } = null;
        public double? LastLapDeltaToAheadInClassLast { get; private set; } = null;

        // Else
        public bool IsFinished { get; private set; } = false;

        public TimeSpan? FinishTime { get; private set; } = null;
        public double?[] BestLapSectors { get; private set; } = new double?[] { null, null, null };
        public double MaxSpeed { get; private set; } = 0.0;
        public bool IsFocused { get; internal set; } = false;
        public bool IsOverallBestLapCar { get; private set; } = false;
        public bool IsClassBestLapCar { get; private set; } = false;
        public int RelativeOnTrackLapDiff { get; private set; } = 0;

        public bool JumpedToPits { get; private set; } = false;
        public bool HasCrossedStartLine { get; private set; } = true;

        internal bool IsFirstUpdate { get; private set; } = true;
        internal bool SetFinishedOnNextUpdate { get; private set; } = false;
        internal bool IsFinalRealtimeCarUpdateAdded { get; private set; } = false;
        internal OffsetLapUpdateType OffsetLapUpdate { get; private set; } = OffsetLapUpdateType.None;
        internal int MissedRealtimeUpdates { get; set; } = 0;

        private double? _stintStartTime = null;
        private readonly CarClassArray<double?> _splinePositionTime = new(null);
        private int _lapAtOffsetLapUpdate = -1;

        private bool _isSplinePositionReset = false;
        private bool _isNewLap = false;
        private bool _enteredPitlane = false;
        private bool _exitedPitlane = false;

        ////////////////////////

        internal CarData(in CarInfo info, RealtimeCarUpdate? update) {
            this.CarIndex = info.Id;
            this.CarModelType = info.ModelType;
            this.CarClass = info.Class;
            this.TeamName = info.TeamName;
            this.RaceNumber = info.RaceNumber;
            this.TeamCupCategory = info.CupCategory;
            this._currentDriverIndex = info.CurrentDriverIndex;
            this.CurrentDriverIndex = this._currentDriverIndex;
            foreach (var d in info.Drivers) {
                this.AddDriver(d);
            }
            this.TeamNationality = info.TeamNationality;

            this.NewData = update;
        }

        /// <summary>
        /// Return current driver always as first driver. Other drivers in order as they are in drivers list.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public DriverData GetDriver(int i) {
            if (i == 0) {
                return this.Drivers.ElementAtOrDefault(this.CurrentDriverIndex);
            }

            if (i <= this.CurrentDriverIndex) {
                return this.Drivers.ElementAtOrDefault(i - 1);
            }

            return this.Drivers.ElementAtOrDefault(i);
        }

        public double? GetDriverTotalDrivingTime(int i) {
            return this.GetDriver(i)?.GetTotalDrivingTime(i == 0, this.CurrentStintTime);
        }

        /// <summary>
        /// Updates this cars static info. Should be called when new entry list update for this car is received.
        /// </summary>
        /// <param name="info"></param>
        internal void OnEntryListUpdate(in CarInfo info) {
            // Only thing that can change is drivers
            // We need to make sure that the order is as specified by new info
            // But also add new drivers. We keep old drivers but move them to the end of list
            // as they might rejoin and then we need to have the old data. (I'm not sure if ACC keeps those drivers or not, but we make sure to keep the data.)
            this.CurrentDriverIndex = info.CurrentDriverIndex;
            if (this.Drivers.Count == info.Drivers.Length
                && this.Drivers.Zip(info.Drivers, (a, b) => a.Equals(b)).All(x => x)
            ) {
                return; // All drivers are same
            }

            // Fix drivers list
            for (int i = 0; i < info.Drivers.Length; i++) {
                var currentDriver = this.Drivers[i];
                var newDriver = info.Drivers[i];
                if (currentDriver.Equals(newDriver)) {
                    continue;
                }

                var oldIdx = this.Drivers.FindIndex(x => x.Equals(newDriver));
                if (oldIdx == -1) {
                    // Must be new driver
                    this.Drivers.Insert(i, new DriverData(newDriver));
                } else {
                    // Driver is present but it's order has changed
                    var old = this.Drivers[oldIdx];
                    this.Drivers.RemoveAt(oldIdx);
                    this.Drivers.Insert(i, old);
                }
            }
        }

        /// <summary>
        /// Updates this cars data. Should be called when RealtimeCarUpdate for this car is received.
        /// </summary>
        /// <param name="update"></param>
        /// <param name="realtimeData"></param>
        internal void OnRealtimeCarUpdate(RealtimeCarUpdate update, RealtimeData realtimeData) {
            // If the race is finished we don't care about any of the realtime updates anymore.
            // We have set finished positions in ´OnRealtimeUpdate´ and that's really all that matters
            //if (IsFinished) return;

            this.OldData = this.NewData;
            this.NewData = update;
            if (this.OldData == null || this.IsFinished) {
                return;
            }

            this._isNewLap = this.OldData.Laps != this.NewData.Laps;
            this._isSplinePositionReset = this.OldData.SplinePosition > 0.9 && this.NewData.SplinePosition < 0.1;
            this._enteredPitlane = !this.OldData.IsInPitlane && this.NewData.IsInPitlane;
            this._exitedPitlane = this.OldData.IsInPitlane && !this.NewData.IsInPitlane;

            if (realtimeData.IsRace) {
                HandleOffsetLapUpdates();
            }
            // Wait for one more update at the beginning of session, so we have all relevant data for calculations below
            if (this.IsFinalRealtimeCarUpdateAdded) {
                return;
            }

            this.CurrentDriverIndex = this.NewData.DriverIndex;

            if (realtimeData.IsRace) {
                CheckForCrossingStartLine();
                this.TotalSplinePosition = this.NewData.SplinePosition + this.NewData.Laps;
                if (this.OffsetLapUpdate == OffsetLapUpdateType.LapBeforeSpline) {
                    this.TotalSplinePosition -= 1;
                } else if (this.OffsetLapUpdate == OffsetLapUpdateType.SplineBeforeLap) {
                    this.TotalSplinePosition += 1;
                }

                UpdatePitInfo();
            }
            HandleRTG();

            if (this._isNewLap) {
                this.CurrentDriver.OnLapFinished(this.NewData.LastLap);
                this.IsLastLapOutLap = this.IsCurrentLapOutLap;
                this.IsCurrentLapOutLap = false;
                this.IsLastLapInLap = this.IsCurrentLapInLap;
                this.IsCurrentLapInLap = false;
            }

            if (this._exitedPitlane) {
                this.IsCurrentLapOutLap = true;
            }
            if (this._enteredPitlane) {
                this.IsCurrentLapInLap = true;
            }

            UpdateStintInfo();
            UpdateBestLapSectors();

            this.MaxSpeed = Math.Max(this.MaxSpeed, this.NewData.Kmh);
            if (this.SetFinishedOnNextUpdate && this.OffsetLapUpdate == OffsetLapUpdateType.None) {
                this.IsFinalRealtimeCarUpdateAdded = true;
            }

            #region Local functions

            void CheckForCrossingStartLine() {
                // Initial update before the start of the race
                if (realtimeData.IsPreSession
                    && this.HasCrossedStartLine
                    && this.NewData.SplinePosition > 0.5
                    && this.NewData.Laps == 0
                ) {
                    this.HasCrossedStartLine = false;
                }

                if (!this.HasCrossedStartLine && (this._isSplinePositionReset || this._exitedPitlane)) {
                    this.HasCrossedStartLine = true;
                }
            }

            void HandleRTG() {
                if (realtimeData.IsRace
                    && !this.SetFinishedOnNextUpdate // It's okay to jump to the pits after finishing
                    && this.NewData.IsInPitlane
                    && this.OldData.IsOnTrack
                ) {
                    this.JumpedToPits = true;
                }

                if (this.JumpedToPits && !this.NewData.IsInPitlane) {
                    this.JumpedToPits = false;
                }
            }

            void HandleOffsetLapUpdates() {
                // Check for offset lap update
                if (this.OffsetLapUpdate == OffsetLapUpdateType.None
                    && this._isNewLap
                    && this.NewData.SplinePosition > 0.9
                ) {
                    this.OffsetLapUpdate = OffsetLapUpdateType.LapBeforeSpline;
                    this._lapAtOffsetLapUpdate = this.NewData.Laps;
                } else if (this.OffsetLapUpdate == OffsetLapUpdateType.None
                                && this._isSplinePositionReset
                                && this.NewData.Laps != this._lapAtOffsetLapUpdate // Remove double detection with above
                                && this.NewData.Laps == this.OldData.Laps
                                && this.HasCrossedStartLine
                    ) {
                    this.OffsetLapUpdate = OffsetLapUpdateType.SplineBeforeLap;
                    this._lapAtOffsetLapUpdate = this.NewData.Laps;
                }

                if (this.OffsetLapUpdate == OffsetLapUpdateType.LapBeforeSpline) {
                    if (this.NewData.SplinePosition < 0.9) {
                        this.OffsetLapUpdate = OffsetLapUpdateType.None;
                    }
                } else if (this.OffsetLapUpdate == OffsetLapUpdateType.SplineBeforeLap) {
                    if (this.NewData.Laps != this._lapAtOffsetLapUpdate || (this.NewData.SplinePosition > 0.025 && this.NewData.SplinePosition < 0.9)) {
                        // Second condition is a fallback in case the lap actually shouldn't have been updated (eg at the start line, jumped to pits and then crossed the line in the pits)
                        this.OffsetLapUpdate = OffsetLapUpdateType.None;
                    }
                }
            }

            void UpdatePitInfo() {
                // Pit started
                if (this._enteredPitlane
                    || (this.PitEntryTime == null && this.NewData.IsInPitlane && !realtimeData.IsPreSession) // We join/start SimHub mid session
                ) {
                    this.PitCount++;
                    this.PitEntryTime = realtimeData.NewData.SessionRunningTime.TotalSeconds;
                }

                // Pit ended
                if (this.PitEntryTime != null && this._exitedPitlane) {
                    this.IsCurrentLapOutLap = true;
                    this.LastPitTime = realtimeData.NewData.SessionRunningTime.TotalSeconds - this.PitEntryTime;
                    this.TotalPitTime += (double)this.LastPitTime;
                    this.PitEntryTime = null;
                    this.CurrentTimeInPits = null;
                }

                if (this.PitEntryTime != null) {
                    this.CurrentTimeInPits = realtimeData.NewData.SessionRunningTime.TotalSeconds - this.PitEntryTime;
                }
            }

            void UpdateStintInfo() {
                if (this._isNewLap) {
                    this.CurrentStintLaps++;
                }

                // Stint started
                if (this._exitedPitlane // Pitlane exit
                    || (realtimeData.IsRace && realtimeData.IsSessionStart) // Race start
                    || (this._stintStartTime == null && this.NewData.IsOnTrack && !realtimeData.IsPreSession) // We join/start SimHub mid session
                ) {
                    this._stintStartTime = realtimeData.NewData.SessionRunningTime.TotalSeconds;
                }

                // Stint ended
                if (this._enteredPitlane && this._stintStartTime != null) {
                    this.LastStintTime = realtimeData.NewData.SessionRunningTime.TotalSeconds - (double)this._stintStartTime;
                    this.CurrentDriver.OnStintEnd((double)this.LastStintTime);
                    this._stintStartTime = null;
                    this.CurrentStintTime = null;
                    this.LastStintLaps = this.CurrentStintLaps;
                    this.CurrentStintLaps = 0;
                }

                if (this._stintStartTime != null) {
                    this.CurrentStintTime = realtimeData.NewData.SessionRunningTime.TotalSeconds - (double)this._stintStartTime;
                }
            }

            void UpdateBestLapSectors() {
                // Note that NewData.BestSessionLap doesn't contain the sectors of that best lap but the best sectors.
                if (this._isNewLap
                    && this.NewData.LastLap.IsValidForBest
                    && this.NewData.LastLap.Laptime == this.NewData.BestSessionLap.Laptime
                ) {
                    this.NewData.LastLap.Splits.CopyTo(this.BestLapSectors, 0);
                }
            }

            #endregion Local functions
        }

        internal void OnRealtimeUpdateFirstPass(int focusedCarIndex) {
            this._splinePositionTime.Reset();
            this.IsFocused = this.CarIndex == focusedCarIndex;
        }

        internal void OnRealtimeUpdateSecondPass(
            RealtimeData realtimeData,
            TrackData trackData,
            CarData leaderCar,
            CarData classLeaderCar,
            CarData focusedCar,
            CarData? carAhead,
            CarData? carAheadInClass,
            CarData? carAheadOnTrack,
            CarData? overallBestLapCar,
            CarData? classBestLapCar,
            int overallPos,
            int classPos
        ) {
            this.IsOverallBestLapCar = this.CarIndex == overallBestLapCar?.CarIndex;
            this.IsClassBestLapCar = this.CarIndex == classBestLapCar?.CarIndex;

            this.InClassPos = classPos;
            this.OverallPos = overallPos;

            if (realtimeData.NewData.SessionRemainingTime == TimeSpan.Zero && realtimeData.IsRace) {
                // We also need to check finished here (after positions update) to detect leaders finish
                this.CheckIsFinished();

                // If broadcast event was missed we can double check here. Note that we must assume that the session is ended for more than BroadcastDataUpdateRate,
                // otherwise we could falsely detect finish.
                //
                // Say our refresh rate is 5s.Then if you crossed the line inside that 5s then on the next update
                // a) clock has run out and b) you just crossed the line(eg finished lap), this means that you will
                // be falsely counted as finished.
                if (!this.IsFinished
                    && (realtimeData.NewData.SessionRunningTime - realtimeData.SessionTotalTime).TotalMilliseconds > DynLeaderboardsPlugin.Settings.BroadcastDataUpdateRateMs
                    && this._isNewLap
                    && (leaderCar.CarIndex == this.CarIndex || leaderCar.SetFinishedOnNextUpdate)
                ) {
                    this.SetFinishedOnNextUpdate = true;
                    var timeFromLastRealtimeUpdate = (DateTime.Now - realtimeData.NewData.RecieveTime).TotalSeconds;
                    this.FinishTime = realtimeData.NewData.SessionRunningTime + TimeSpan.FromSeconds(timeFromLastRealtimeUpdate);
                }
            }

            SetGaps();
            SetLapDeltas();

            #region Local functions

            void SetGaps() {
                // Freeze gaps until all is in order again, fixes gap suddenly jumping to larger values as spline positions could be out of sync
                if (this.OffsetLapUpdate == OffsetLapUpdateType.None) {
                    if (focusedCar.OffsetLapUpdate == OffsetLapUpdateType.None) {
                        this.GapToFocusedOnTrack = CalculateOnTrackGap(this, focusedCar, trackData);
                    }

                    if (carAheadOnTrack?.OffsetLapUpdate == OffsetLapUpdateType.None) {
                        this.GapToAheadOnTrack = CalculateOnTrackGap(carAheadOnTrack, this, trackData);
                    }
                }

                if (realtimeData.IsRace) {
                    // Use time gaps on track
                    // We update the gap only if CalculateGap returns a proper value because we don't want to update the gap if one of the cars has finished.
                    // That would result in wrong gaps. We keep the gaps at the last valid value and update once both cars have finished.

                    // Freeze gaps until all is in order again, fixes gap suddenly jumping to larger values as spline positions could be out of sync
                    if (this.OffsetLapUpdate == OffsetLapUpdateType.None) {
                        SetGap(this, leaderCar, leaderCar, this.GapToLeader, x => this.GapToLeader = x);
                        SetGap(this, classLeaderCar, classLeaderCar, this.GapToClassLeader, x => this.GapToClassLeader = x);
                        SetGap(focusedCar, this, focusedCar, this.GapToFocusedTotal, x => this.GapToFocusedTotal = x);
                        SetGap(this, carAhead, carAhead, this.GapToAhead, x => this.GapToAhead = x);
                        SetGap(this, carAheadInClass, carAheadInClass, this.GapToAheadInClass, x => this.GapToAheadInClass = x);

                        void SetGap(CarData? from, CarData? to, CarData? other, double? currentGap, Action<double?> setGap) {
                            if (from == null || to == null) {
                                setGap(null);
                            } else if (other?.OffsetLapUpdate == OffsetLapUpdateType.None) {
                                setGap(CalculateGap(from, to, trackData));
                            }
                        }

                        if (focusedCar.OffsetLapUpdate == OffsetLapUpdateType.None) {
                            SetRelLapDiff();
                        }
                    }
                } else {
                    // Use best laps to calculate gaps
                    var thisBestLap = this.NewData?.BestSessionLap.Laptime;
                    if (thisBestLap == null) {
                        this.GapToLeader = null;
                        this.GapToClassLeader = null;
                        this.GapToFocusedTotal = null;
                        this.GapToAheadInClass = null;
                        this.GapToAhead = null;
                        return;
                    }

                    this.GapToLeader = CalculateBestLapDelta(leaderCar);
                    this.GapToClassLeader = CalculateBestLapDelta(classLeaderCar);
                    this.GapToFocusedTotal = CalculateBestLapDelta(focusedCar);
                    this.GapToAhead = CalculateBestLapDelta(carAhead);
                    this.GapToAheadInClass = CalculateBestLapDelta(carAheadInClass);

                    double? CalculateBestLapDelta(CarData? to) {
                        var toBest = to?.NewData?.BestSessionLap.Laptime;
                        return toBest != null ? (double)thisBestLap - (double)toBest : (double?)null;
                    }
                }
            }

            void SetRelLapDiff() {
                if (this.NewData == null || focusedCar.NewData == null) {
                    return;
                }

                if (this.GapToFocusedTotal == null) {
                    if (this.NewData.Laps < focusedCar.NewData.Laps) {
                        this.RelativeOnTrackLapDiff = -1;
                    } else if (this.NewData.Laps > focusedCar.NewData.Laps) {
                        this.RelativeOnTrackLapDiff = 1;
                    } else {
                        if (this.GapToFocusedOnTrack > 0) {
                            if (this.OverallPos > focusedCar.OverallPos) {
                                this.RelativeOnTrackLapDiff = -1;
                            } else {
                                this.RelativeOnTrackLapDiff = 0;
                            }
                        } else {
                            if (this.OverallPos < focusedCar.OverallPos) {
                                this.RelativeOnTrackLapDiff = 1;
                            } else {
                                this.RelativeOnTrackLapDiff = 0;
                            }
                        }
                    }
                } else if (this.GapToFocusedTotal > 100_000) {
                    this.RelativeOnTrackLapDiff = 1;
                } else if (this.GapToFocusedTotal < 50_000) {
                    this.RelativeOnTrackLapDiff = 0;
                    if (this.GapToFocusedOnTrack > 0) {
                        if (this.OverallPos > focusedCar.OverallPos) {
                            this.RelativeOnTrackLapDiff = -1;
                        } else {
                            this.RelativeOnTrackLapDiff = 0;
                        }
                    } else {
                        if (this.OverallPos < focusedCar.OverallPos) {
                            this.RelativeOnTrackLapDiff = 1;
                        } else {
                            this.RelativeOnTrackLapDiff = 0;
                        }
                    }
                } else {
                    this.RelativeOnTrackLapDiff = -1;
                }
            }

            void SetLapDeltas() {
                var thisBest = this.NewData?.BestSessionLap.Laptime;
                var thisLast = this.NewData?.LastLap.Laptime;
                if (thisBest == null && thisLast == null) {
                    return;
                }

                var overallBest = overallBestLapCar?.NewData?.BestSessionLap.Laptime;
                var classBest = classBestLapCar?.NewData?.BestSessionLap.Laptime;
                var leaderBest = leaderCar?.NewData?.BestSessionLap.Laptime;
                var classLeaderBest = classLeaderCar?.NewData?.BestSessionLap.Laptime;
                var focusedBest = focusedCar?.NewData?.BestSessionLap.Laptime;
                var aheadBest = carAhead?.NewData?.BestSessionLap.Laptime;
                var aheadInClassBest = carAheadInClass?.NewData?.BestSessionLap.Laptime;

                if (thisBest != null) {
                    if (overallBest != null) {
                        this.BestLapDeltaToOverallBest = (double)thisBest - (double)overallBest;
                    }

                    if (classBest != null) {
                        this.BestLapDeltaToClassBest = (double)thisBest - (double)classBest;
                    }

                    if (leaderBest != null) {
                        this.BestLapDeltaToLeaderBest = (double)thisBest - (double)leaderBest;
                    }

                    if (classLeaderBest != null) {
                        this.BestLapDeltaToClassLeaderBest = (double)thisBest - (double)classLeaderBest;
                    }

                    this.BestLapDeltaToFocusedBest = focusedBest != null ? (double)thisBest - (double)focusedBest : (double?)null;
                    this.BestLapDeltaToAheadBest = aheadBest != null ? (double)thisBest - (double)aheadBest : (double?)null;
                    this.BestLapDeltaToAheadInClassBest = aheadInClassBest != null ? (double)thisBest - (double)aheadInClassBest : (double?)null;
                }

                if (thisLast != null) {
                    if (overallBest != null) {
                        this.LastLapDeltaToOverallBest = (double)thisLast - (double)overallBest;
                    }

                    if (classBest != null) {
                        this.LastLapDeltaToClassBest = (double)thisLast - (double)classBest;
                    }

                    if (leaderBest != null) {
                        this.LastLapDeltaToLeaderBest = (double)thisLast - (double)leaderBest;
                    }

                    if (classLeaderBest != null) {
                        this.LastLapDeltaToClassLeaderBest = (double)thisLast - (double)classLeaderBest;
                    }

                    this.LastLapDeltaToFocusedBest = focusedBest != null ? (double)thisLast - (double)focusedBest : (double?)null;
                    this.LastLapDeltaToAheadBest = aheadBest != null ? (double)thisLast - (double)aheadBest : (double?)null;
                    this.LastLapDeltaToAheadInClassBest = aheadInClassBest != null ? (double)thisLast - (double)aheadInClassBest : (double?)null;

                    if (thisBest != null) {
                        this.LastLapDeltaToOwnBest = (double)thisLast - (double)thisBest;
                    }

                    var leaderLast = leaderCar?.NewData?.LastLap.Laptime;
                    var classLeaderLast = classLeaderCar?.NewData?.LastLap.Laptime;
                    var focusedLast = focusedCar?.NewData?.LastLap.Laptime;
                    var aheadLast = carAhead?.NewData?.LastLap.Laptime;
                    var aheadInClassLast = carAheadInClass?.NewData?.LastLap.Laptime;

                    if (leaderLast != null) {
                        this.LastLapDeltaToLeaderLast = (double)thisLast - (double)leaderLast;
                    }

                    if (classLeaderLast != null) {
                        this.LastLapDeltaToClassLeaderLast = (double)thisLast - (double)classLeaderLast;
                    }

                    this.LastLapDeltaToFocusedLast = focusedLast != null ? (double)thisLast - (double)focusedLast : (double?)null;
                    this.LastLapDeltaToAheadLast = aheadLast != null ? (double)thisLast - (double)aheadLast : (double?)null;
                    this.LastLapDeltaToAheadInClassLast = aheadInClassLast != null ? (double)thisLast - (double)aheadInClassLast : (double?)null;
                }
            }

            #endregion Local functions
        }

        internal void CheckIsFinished() {
            if (!this.IsFinished && this.IsFinalRealtimeCarUpdateAdded && this.OffsetLapUpdate == OffsetLapUpdateType.None) {
                this.IsFinished = true;
            }
        }

        internal void SetIsFinished(in TimeSpan finishTime) {
            this.SetFinishedOnNextUpdate = true;
            this.FinishTime = finishTime;
        }

        /// <summary>
        /// Sets starting positions for this car.
        /// </summary>
        /// <param name="overall"></param>
        /// <param name="inclass"></param>
        internal void SetStartingPositions(int overall, int inclass) {
            this.StartPos = overall;
            this.StartPosInClass = inclass;
        }

        private void AddDriver(in DriverInfo driverInfo) {
            this.Drivers.Add(new DriverData(driverInfo));
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
        public static double? CalculateGap(CarData from, CarData to, TrackData trackData) {
            if (from?.NewData == null
                || to?.NewData == null
                || from.OldData == null
                || to.OldData == null
                || from.CarIndex == to.CarIndex
                || !to.HasCrossedStartLine
                || !from.HasCrossedStartLine
                || from.OffsetLapUpdate != OffsetLapUpdateType.None
                || to.OffsetLapUpdate != OffsetLapUpdateType.None
            ) {
                return null;
            }

            var flaps = from.NewData.Laps;
            var tlaps = to.NewData.Laps;

            // If one of the cars jumped to pits there is no correct way to calculate the gap
            if (flaps == tlaps && (from.JumpedToPits || to.JumpedToPits)) {
                return null;
            }

            if (from.IsFinished && to.IsFinished) {
                if (flaps == tlaps) {
                    // If there IsFinished is set, FinishTime must also be set
                    return ((TimeSpan)from.FinishTime!).TotalSeconds - ((TimeSpan)to.FinishTime!).TotalSeconds;
                } else {
                    return tlaps - flaps + 100_000;
                }
            }

            // Fixes wrong gaps after finish on cars that haven't finished and are in pits.
            // Without this the gap could be off by one lap from the gap calculated from completed laps.
            // This is correct if the session is not finished as you could go out and complete that lap.
            // If session has finished you cannot complete that lap.
            if (tlaps != flaps &&
                ((to.IsFinished && !from.IsFinished && from.NewData.IsInPitlane)
                || (from.IsFinished && !to.IsFinished && to.NewData.IsInPitlane))
            ) {
                return tlaps - flaps + 100_000;
            }

            var distBetween = to.TotalSplinePosition - from.TotalSplinePosition; // Negative if 'to' is behind
            if (distBetween <= -1) { // 'to' is more than a lap behind of 'from'
                return Math.Ceiling(distBetween) + 100_000;
            } else if (distBetween >= 1) { // 'to' is more than a lap ahead of 'from'
                return Math.Floor(distBetween) + 100_000;
            } else {
                if (from.IsFinished
                    || to.IsFinished
                    || trackData == null
                ) {
                    return null;
                }

                // TrackData is passed from Values, Values never stores TrackData without LapInterpolators
                var toInterp = trackData.LapInterpolators![to.CarClass];
                var fromInterp = trackData.LapInterpolators![from.CarClass];
                if (toInterp == null && fromInterp == null) {
                    // lap data is not available, use naive distance based calculation
                    return CalculateNaiveGap(distBetween, trackData);
                }

                double? gap;
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

        public static double? CalculateOnTrackGap(CarData from, CarData to, TrackData trackData) {
            if (from?.NewData == null
                 || to?.NewData == null
                 || from.OldData == null
                 || to.OldData == null
                 || from.CarIndex == to.CarIndex
                 || from.OffsetLapUpdate != OffsetLapUpdateType.None
                 || to.OffsetLapUpdate != OffsetLapUpdateType.None
                 || trackData == null
             ) {
                return null;
            }

            var fromPos = from.NewData.SplinePosition;
            var toPos = to.NewData.SplinePosition;
            var relativeSplinePos = CalculateRelativeSplinePosition(fromPos, toPos);

            // TrackData is passed from Values, Values never stores TrackData without LapInterpolators
            var toInterp = trackData.LapInterpolators![to.CarClass];
            var fromInterp = trackData.LapInterpolators![from.CarClass];
            if (toInterp == null && fromInterp == null) {
                // lap data is not available, use naive distance based calculation
                return CalculateNaiveGap(relativeSplinePos, trackData);
            }

            double? gap;
            // At least one toInterp or fromInterp must be not null, because of the above check
            (LapInterpolator interp, var cls) = toInterp != null ? (toInterp!, to.CarClass) : (fromInterp!, from.CarClass);
            if (relativeSplinePos < 0) {
                gap = -CalculateGapBetweenPos(from.GetSplinePosTime(cls, trackData), to.GetSplinePosTime(cls, trackData), interp.LapTime);
            } else {
                gap = CalculateGapBetweenPos(to.GetSplinePosTime(cls, trackData), from.GetSplinePosTime(cls, trackData), interp.LapTime);
            }
            return gap;
        }

        private static double CalculateNaiveGap(double splineDist, TrackData trackData) {
            var dist = splineDist * trackData.LengthMeters;
            // use avg speed of 50m/s (180km/h)
            // we could use actual speeds of the cars
            // but the gap will fluctuate either way so I don't think it makes things better.
            // This also avoid the question of which speed to use (faster, slower, average)
            // and what happens if either car is standing (eg speed is 0 and we would divide by 0).
            // It's an just in case backup anyway, so most of the times it should never even be reached.7654
            return dist / 50;
        }

        /// <summary>
        /// Calculates the gap in seconds from <paramref name="start"/> to <paramref name="end"/>.
        /// </summary>
        /// <returns>Non-negative value</returns>
        public static double CalculateGapBetweenPos(double start, double end, double lapTime) {
            if (end < start) { // Ahead is on another lap, gap is time from `start` to end of the lap, and then to `end`
                return lapTime - start + end;
            } else { // We must be on the same lap, gap is time from `start` to reach `end`
                return end - start;
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
        public double? CalculateRelativeSplinePosition(CarData otherCar) {
            if (this.NewData == null || otherCar?.NewData == null) {
                return null;
            }

            return CalculateRelativeSplinePosition(this.NewData.SplinePosition, otherCar.NewData.SplinePosition);
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

        /// <summary>
        /// Calculates expected lap time for <paramref name="cls"> class car at the position of <c>this</c> car.
        /// </summary>
        /// <returns>
        /// Lap time in seconds or <c>-1.0</c> if it cannot be calculated.
        /// </returns>>
        /// <param name="cls"></param>
        /// <returns></returns>
        private double GetSplinePosTime(CarClass cls, TrackData trackData) {
            // Same interpolated value is needed multiple times in one update, thus cache results.
            var pos = this._splinePositionTime[cls];
            if (pos != this._splinePositionTime.DefaultValue && pos != null) {
                return (double)pos;
            }

            // TrackData is passed from Values, Values never stores TrackData without LapInterpolators
            var interp = trackData.LapInterpolators![cls];
            if (this.NewData != null && interp != null) {
                var result = interp.Interpolate(this.NewData.SplinePosition);
                this._splinePositionTime[cls] = result;
                return result;
            } else {
                return -1;
            }
        }
    }
}