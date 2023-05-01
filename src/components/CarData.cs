using KLPlugins.DynLeaderboards.Driver;
using KLPlugins.DynLeaderboards.ksBroadcastingNetwork;
using KLPlugins.DynLeaderboards.ksBroadcastingNetwork.Structs;
using KLPlugins.DynLeaderboards.Realtime;
using KLPlugins.DynLeaderboards.Track;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public string CarClassColor => DynLeaderboardsPlugin.Settings.CarClassColors[CarClass];
        public string TeamCupCategoryColor => DynLeaderboardsPlugin.Settings.TeamCupCategoryColors[TeamCupCategory];
        public string TeamCupCategoryTextColor => DynLeaderboardsPlugin.Settings.TeamCupCategoryTextColors[TeamCupCategory];

        // RealtimeCarUpdates
        public RealtimeCarUpdate NewData { get; private set; } = null;

        public RealtimeCarUpdate OldData { get; private set; } = null;

        public int CurrentDriverIndex;
        public DriverData CurrentDriver => Drivers[CurrentDriverIndex];

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
        public double CurrentDriverTotalDrivingTime => CurrentDriver.GetTotalDrivingTime(true, CurrentStintTime);

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
        private CarClassArray<double?> _splinePositionTime = new CarClassArray<double?>(null);
        private int _lapAtOffsetLapUpdate = -1;

        private bool _isSplinePositionReset = false;
        private bool _isNewLap = false;
        private bool _enteredPitlane = false;
        private bool _exitedPitlane = false;

        ////////////////////////

        internal CarData(CarInfo info, RealtimeCarUpdate update) {
            CarIndex = info.CarIndex;
            CarModelType = info.CarModelType;
            CarClass = info.CarClass;
            TeamName = info.TeamName;
            RaceNumber = info.RaceNumber;
            TeamCupCategory = info.CupCategory;
            _currentDriverIndex = info.CurrentDriverIndex;
            CurrentDriverIndex = _currentDriverIndex;
            foreach (var d in info.Drivers) {
                AddDriver(d);
            }
            TeamNationality = info.Nationality;

            NewData = update;
        }

        /// <summary>
        /// Return current driver always as first driver. Other drivers in order as they are in drivers list.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public DriverData GetDriver(int i) {
            if (i == 0)
                return Drivers.ElementAtOrDefault(CurrentDriverIndex);
            if (i <= CurrentDriverIndex)
                return Drivers.ElementAtOrDefault(i - 1);
            return Drivers.ElementAtOrDefault(i);
        }

        public double? GetDriverTotalDrivingTime(int i) {
            return GetDriver(i)?.GetTotalDrivingTime(i == 0, CurrentStintTime);
        }

        /// <summary>
        /// Updates this cars static info. Should be called when new entry list update for this car is received.
        /// </summary>
        /// <param name="info"></param>
        internal void OnEntryListUpdate(CarInfo info) {
            // Only thing that can change is drivers
            // We need to make sure that the order is as specified by new info
            // But also add new drivers. We keep old drivers but move them to the end of list
            // as they might rejoin and then we need to have the old data. (I'm not sure if ACC keeps those drivers or not, but we make sure to keep the data.)
            CurrentDriverIndex = info.CurrentDriverIndex;
            if (Drivers.Count == info.Drivers.Count
                && Drivers.Zip(info.Drivers, (a, b) => a.Equals(b)).All(x => x)
            )
                return; // All drivers are same

            // Fix drivers list
            for (int i = 0; i < info.Drivers.Count; i++) {
                var currentDriver = Drivers[i];
                var newDriver = info.Drivers[i];
                if (currentDriver.Equals(newDriver))
                    continue;

                var oldIdx = Drivers.FindIndex(x => x.Equals(newDriver));
                if (oldIdx == -1) {
                    // Must be new driver
                    Drivers.Insert(i, new DriverData(newDriver));
                } else {
                    // Driver is present but it's order has changed
                    var old = Drivers[oldIdx];
                    Drivers.RemoveAt(oldIdx);
                    Drivers.Insert(i, old);
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

            OldData = NewData;
            NewData = update;
            if (OldData == null || IsFinished)
                return;

            _isNewLap = OldData.Laps != NewData.Laps;
            _isSplinePositionReset = OldData.SplinePosition > 0.9 && NewData.SplinePosition < 0.1;
            _enteredPitlane = !OldData.IsInPitlane && NewData.IsInPitlane;
            _exitedPitlane = OldData.IsInPitlane && !NewData.IsInPitlane;

            if (realtimeData.IsRace)
                HandleOffsetLapUpdates();
            // Wait for one more update at the beginning of session, so we have all relevant data for calculations below
            if (IsFinalRealtimeCarUpdateAdded)
                return;

            if (NewData?.DriverIndex != null)
                CurrentDriverIndex = NewData.DriverIndex;

            if (realtimeData.IsRace) {
                CheckForCrossingStartLine();
                TotalSplinePosition = NewData.SplinePosition + NewData.Laps;
                if (OffsetLapUpdate == OffsetLapUpdateType.LapBeforeSpline)
                    TotalSplinePosition -= 1;
                else if (OffsetLapUpdate == OffsetLapUpdateType.SplineBeforeLap)
                    TotalSplinePosition += 1;
                UpdatePitInfo();
            }
            HandleRTG();

            if (_isNewLap) {
                CurrentDriver.OnLapFinished(NewData.LastLap);
                IsLastLapOutLap = IsCurrentLapOutLap;
                IsCurrentLapOutLap = false;
                IsLastLapInLap = IsCurrentLapInLap;
                IsCurrentLapInLap = false;
            }

            if (_exitedPitlane) {
                IsCurrentLapOutLap = true;
            }
            if (_enteredPitlane) {
                IsCurrentLapInLap = true;
            }

            UpdateStintInfo();
            UpdateBestLapSectors();

            MaxSpeed = Math.Max(MaxSpeed, NewData.Kmh);
            if (SetFinishedOnNextUpdate && OffsetLapUpdate == OffsetLapUpdateType.None) {
                IsFinalRealtimeCarUpdateAdded = true;
            }

            #region Local functions

            void CheckForCrossingStartLine() {
                // Initial update before the start of the race
                if (realtimeData.IsPreSession
                    && HasCrossedStartLine
                    && NewData.SplinePosition > 0.5
                    && NewData.Laps == 0
                ) {
                    HasCrossedStartLine = false;
                }

                if (!HasCrossedStartLine && (_isSplinePositionReset || _exitedPitlane)) {
                    HasCrossedStartLine = true;
                }
            }

            void HandleRTG() {
                if (realtimeData.IsRace
                    && !SetFinishedOnNextUpdate // It's okay to jump to the pits after finishing
                    && NewData.IsInPitlane
                    && OldData.IsOnTrack
                ) {
                    JumpedToPits = true;
                }

                if (JumpedToPits && !NewData.IsInPitlane) {
                    JumpedToPits = false;
                }
            }

            void HandleOffsetLapUpdates() {
                // Check for offset lap update
                if (OffsetLapUpdate == OffsetLapUpdateType.None
                    && _isNewLap
                    && NewData.SplinePosition > 0.9
                ) {
                    OffsetLapUpdate = OffsetLapUpdateType.LapBeforeSpline;
                    _lapAtOffsetLapUpdate = NewData.Laps;
                } else if (OffsetLapUpdate == OffsetLapUpdateType.None
                                && _isSplinePositionReset
                                && NewData.Laps != _lapAtOffsetLapUpdate // Remove double detection with above
                                && NewData.Laps == OldData.Laps
                                && HasCrossedStartLine
                    ) {
                    OffsetLapUpdate = OffsetLapUpdateType.SplineBeforeLap;
                    _lapAtOffsetLapUpdate = NewData.Laps;
                }

                if (OffsetLapUpdate == OffsetLapUpdateType.LapBeforeSpline) {
                    if (NewData.SplinePosition < 0.9) {
                        OffsetLapUpdate = OffsetLapUpdateType.None;
                    }
                } else if (OffsetLapUpdate == OffsetLapUpdateType.SplineBeforeLap) {
                    if (NewData.Laps != _lapAtOffsetLapUpdate || (NewData.SplinePosition > 0.025 && NewData.SplinePosition < 0.9)) {
                        // Second condition is a fallback in case the lap actually shouldn't have been updated (eg at the start line, jumped to pits and then crossed the line in the pits)
                        OffsetLapUpdate = OffsetLapUpdateType.None;
                    }
                }
            }

            void UpdatePitInfo() {
                // Pit started
                if (_enteredPitlane
                    || (PitEntryTime == null && NewData.IsInPitlane && !realtimeData.IsPreSession) // We join/start SimHub mid session
                ) {
                    PitCount++;
                    PitEntryTime = realtimeData.SessionRunningTime.TotalSeconds;
                }

                // Pit ended
                if (PitEntryTime != null && _exitedPitlane) {
                    IsCurrentLapOutLap = true;
                    LastPitTime = realtimeData.SessionRunningTime.TotalSeconds - PitEntryTime;
                    TotalPitTime += (double)LastPitTime;
                    PitEntryTime = null;
                    CurrentTimeInPits = null;
                }

                if (PitEntryTime != null) {
                    CurrentTimeInPits = realtimeData.SessionRunningTime.TotalSeconds - PitEntryTime;
                }
            }

            void UpdateStintInfo() {
                if (_isNewLap) {
                    CurrentStintLaps++;
                }

                // Stint started
                if (_exitedPitlane // Pitlane exit
                    || (realtimeData.IsRace && realtimeData.IsSessionStart) // Race start
                    || (_stintStartTime == null && NewData.IsOnTrack && !realtimeData.IsPreSession) // We join/start SimHub mid session
                ) {
                    _stintStartTime = realtimeData.SessionRunningTime.TotalSeconds;
                }

                // Stint ended
                if (_enteredPitlane && _stintStartTime != null) {
                    LastStintTime = realtimeData.SessionRunningTime.TotalSeconds - (double)_stintStartTime;
                    CurrentDriver.OnStintEnd((double)LastStintTime);
                    _stintStartTime = null;
                    CurrentStintTime = null;
                    LastStintLaps = CurrentStintLaps;
                    CurrentStintLaps = 0;
                }

                if (_stintStartTime != null) {
                    CurrentStintTime = realtimeData.SessionRunningTime.TotalSeconds - (double)_stintStartTime;
                }
            }

            void UpdateBestLapSectors() {
                // Note that NewData.BestSessionLap doesn't contain the sectors of that best lap but the best sectors.
                if (_isNewLap && NewData.LastLap.IsValidForBest) {
                    NewData.LastLap.Splits.CopyTo(BestLapSectors, 0);
                }
            }

            #endregion Local functions
        }

        internal void OnRealtimeUpdateFirstPass(int focusedCarIndex) {
            _splinePositionTime.Reset();
            IsFocused = CarIndex == focusedCarIndex;
        }

        internal void OnRealtimeUpdateSecondPass(
            RealtimeData realtimeData,
            CarData leaderCar,
            CarData classLeaderCar,
            CarData focusedCar,
            CarData carAhead,
            CarData carAheadInClass,
            CarData carAheadOnTrack,
            CarData overallBestLapCar,
            CarData classBestLapCar,
            int overallPos,
            int classPos,
            float sessionTimeLeft
        ) {
            IsOverallBestLapCar = CarIndex == overallBestLapCar?.CarIndex;
            IsClassBestLapCar = CarIndex == classBestLapCar?.CarIndex;

            InClassPos = classPos;
            OverallPos = overallPos;

            if (realtimeData.SessionRemainingTime == TimeSpan.Zero && realtimeData.IsRace) {
                // We also need to check finished here (after positions update) to detect leaders finish
                CheckIsFinished();

                // If broadcast event was missed we can double check here. Note that we must assume that the session is ended for more than BroadcastDataUpdateRate,
                // otherwise we could falsely detect finish.
                //
                // Say our refresh rate is 5s.Then if you crossed the line inside that 5s then on the next update
                // a) clock has run out and b) you just crossed the line(eg finished lap), this means that you will
                // be falsely counted as finished.
                if (!IsFinished
                    && (realtimeData.SessionRunningTime - realtimeData.SessionTotalTime).TotalMilliseconds > DynLeaderboardsPlugin.Settings.BroadcastDataUpdateRateMs
                    && _isNewLap
                    && (leaderCar.CarIndex == CarIndex || leaderCar.SetFinishedOnNextUpdate)
                ) {
                    SetFinishedOnNextUpdate = true;
                    var timeFromLastRealtimeUpdate = (DateTime.Now - realtimeData.NewData.RecieveTime).TotalSeconds;
                    FinishTime = realtimeData.SessionRunningTime + TimeSpan.FromSeconds(timeFromLastRealtimeUpdate);
                }
            }

            SetGaps();
            SetLapDeltas();

            #region Local functions

            void SetGaps() {
                // Freeze gaps until all is in order again, fixes gap suddenly jumping to larger values as spline positions could be out of sync
                if (OffsetLapUpdate == OffsetLapUpdateType.None) {
                    if (focusedCar?.OffsetLapUpdate == OffsetLapUpdateType.None)
                        GapToFocusedOnTrack = CalculateOnTrackGap(this, focusedCar);
                    if (carAheadOnTrack?.OffsetLapUpdate == OffsetLapUpdateType.None)
                        GapToAheadOnTrack = CalculateOnTrackGap(carAheadOnTrack, this);
                }

                if (realtimeData.IsRace) {
                    // Use time gaps on track
                    // We update the gap only if CalculateGap returns a proper value because we don't want to update the gap if one of the cars has finished.
                    // That would result in wrong gaps. We keep the gaps at the last valid value and update once both cars have finished.

                    // Freeze gaps until all is in order again, fixes gap suddenly jumping to larger values as spline positions could be out of sync
                    if (OffsetLapUpdate == OffsetLapUpdateType.None) {
                        SetGap(this, leaderCar, leaderCar, GapToLeader, x => GapToLeader = x);
                        SetGap(this, classLeaderCar, classLeaderCar, GapToClassLeader, x => GapToClassLeader = x);
                        SetGap(focusedCar, this, focusedCar, GapToFocusedTotal, x => GapToFocusedTotal = x);
                        SetGap(this, carAhead, carAhead, GapToAhead, x => GapToAhead = x);
                        SetGap(this, carAheadInClass, carAheadInClass, GapToAheadInClass, x => GapToAheadInClass = x);

                        void SetGap(CarData from, CarData to, CarData other, double? currentGap, Action<double?> setGap) {
                            if (from == null || to == null)
                                setGap(null);
                            else if (other.OffsetLapUpdate == OffsetLapUpdateType.None) {
                                setGap(CalculateGap(from, to));
                            }
                        }

                        if (focusedCar.OffsetLapUpdate == OffsetLapUpdateType.None) {
                            SetRelLapDiff();
                        }
                    }
                } else {
                    // Use best laps to calculate gaps
                    var thisBestLap = NewData?.BestSessionLap?.Laptime;
                    if (thisBestLap == null) {
                        GapToLeader = null;
                        GapToClassLeader = null;
                        GapToFocusedTotal = null;
                        GapToAheadInClass = null;
                        GapToAhead = null;
                        return;
                    }

                    GapToLeader = CalculateBestLapDelta(leaderCar);
                    GapToClassLeader = CalculateBestLapDelta(classLeaderCar);
                    GapToFocusedTotal = CalculateBestLapDelta(focusedCar);
                    GapToAhead = CalculateBestLapDelta(carAhead);
                    GapToAheadInClass = CalculateBestLapDelta(carAheadInClass);

                    double? CalculateBestLapDelta(CarData to) {
                        var toBest = to?.NewData?.BestSessionLap?.Laptime;
                        return toBest != null ? (double)thisBestLap - (double)toBest : (double?)null;
                    }
                }
            }

            void SetRelLapDiff() {
                if (NewData == null || focusedCar.NewData == null) {
                    return;
                }

                if (GapToFocusedTotal == null) {
                    if (NewData.Laps < focusedCar.NewData.Laps) {
                        RelativeOnTrackLapDiff = -1;
                    } else if (NewData.Laps > focusedCar.NewData.Laps) {
                        RelativeOnTrackLapDiff = 1;
                    } else {
                        if (GapToFocusedOnTrack > 0) {
                            if (OverallPos > focusedCar.OverallPos) {
                                RelativeOnTrackLapDiff = -1;
                            } else {
                                RelativeOnTrackLapDiff = 0;
                            }
                        } else {
                            if (OverallPos < focusedCar.OverallPos) {
                                RelativeOnTrackLapDiff = 1;
                            } else {
                                RelativeOnTrackLapDiff = 0;
                            }
                        }
                    }
                } else if (GapToFocusedTotal > 100_000) {
                    RelativeOnTrackLapDiff = 1;
                } else if (GapToFocusedTotal < 50_000) {
                    RelativeOnTrackLapDiff = 0;
                    if (GapToFocusedOnTrack > 0) {
                        if (OverallPos > focusedCar.OverallPos) {
                            RelativeOnTrackLapDiff = -1;
                        } else {
                            RelativeOnTrackLapDiff = 0;
                        }
                    } else {
                        if (OverallPos < focusedCar.OverallPos) {
                            RelativeOnTrackLapDiff = 1;
                        } else {
                            RelativeOnTrackLapDiff = 0;
                        }
                    }
                } else {
                    RelativeOnTrackLapDiff = -1;
                }
            }

            void SetLapDeltas() {
                var thisBest = NewData?.BestSessionLap?.Laptime;
                var thisLast = NewData?.LastLap?.Laptime;
                if (thisBest == null && thisLast == null)
                    return;

                var overallBest = overallBestLapCar?.NewData?.BestSessionLap?.Laptime;
                var classBest = classBestLapCar?.NewData?.BestSessionLap?.Laptime;
                var leaderBest = leaderCar?.NewData?.BestSessionLap?.Laptime;
                var classLeaderBest = classLeaderCar?.NewData?.BestSessionLap?.Laptime;
                var focusedBest = focusedCar?.NewData?.BestSessionLap?.Laptime;
                var aheadBest = carAhead?.NewData?.BestSessionLap?.Laptime;
                var aheadInClassBest = carAheadInClass?.NewData?.BestSessionLap?.Laptime;

                if (thisBest != null) {
                    if (overallBest != null)
                        BestLapDeltaToOverallBest = (double)thisBest - (double)overallBest;
                    if (classBest != null)
                        BestLapDeltaToClassBest = (double)thisBest - (double)classBest;
                    if (leaderBest != null)
                        BestLapDeltaToLeaderBest = (double)thisBest - (double)leaderBest;
                    if (classLeaderBest != null)
                        BestLapDeltaToClassLeaderBest = (double)thisBest - (double)classLeaderBest;
                    BestLapDeltaToFocusedBest = focusedBest != null ? (double)thisBest - (double)focusedBest : (double?)null;
                    BestLapDeltaToAheadBest = aheadBest != null ? (double)thisBest - (double)aheadBest : (double?)null;
                    BestLapDeltaToAheadInClassBest = aheadInClassBest != null ? (double)thisBest - (double)aheadInClassBest : (double?)null;
                }

                if (thisLast != null) {
                    if (overallBest != null)
                        LastLapDeltaToOverallBest = (double)thisLast - (double)overallBest;
                    if (classBest != null)
                        LastLapDeltaToClassBest = (double)thisLast - (double)classBest;
                    if (leaderBest != null)
                        LastLapDeltaToLeaderBest = (double)thisLast - (double)leaderBest;
                    if (classLeaderBest != null)
                        LastLapDeltaToClassLeaderBest = (double)thisLast - (double)classLeaderBest;
                    LastLapDeltaToFocusedBest = focusedBest != null ? (double)thisLast - (double)focusedBest : (double?)null;
                    LastLapDeltaToAheadBest = aheadBest != null ? (double)thisLast - (double)aheadBest : (double?)null;
                    LastLapDeltaToAheadInClassBest = aheadInClassBest != null ? (double)thisLast - (double)aheadInClassBest : (double?)null;

                    if (thisBest != null)
                        LastLapDeltaToOwnBest = (double)thisLast - (double)thisBest;

                    var leaderLast = leaderCar?.NewData?.LastLap?.Laptime;
                    var classLeaderLast = classLeaderCar?.NewData?.LastLap?.Laptime;
                    var focusedLast = focusedCar?.NewData?.LastLap?.Laptime;
                    var aheadLast = carAhead?.NewData?.LastLap?.Laptime;
                    var aheadInClassLast = carAheadInClass?.NewData?.LastLap?.Laptime;

                    if (leaderLast != null)
                        LastLapDeltaToLeaderLast = (double)thisLast - (double)leaderLast;
                    if (classLeaderLast != null)
                        LastLapDeltaToClassLeaderLast = (double)thisLast - (double)classLeaderLast;
                    LastLapDeltaToFocusedLast = focusedLast != null ? (double)thisLast - (double)focusedLast : (double?)null;
                    LastLapDeltaToAheadLast = aheadLast != null ? (double)thisLast - (double)aheadLast : (double?)null;
                    LastLapDeltaToAheadInClassLast = aheadInClassLast != null ? (double)thisLast - (double)aheadInClassLast : (double?)null;
                }
            }

            #endregion Local functions
        }

        internal void CheckIsFinished() {
            if (!IsFinished && IsFinalRealtimeCarUpdateAdded && OffsetLapUpdate == OffsetLapUpdateType.None) {
                IsFinished = true;
            }
        }

        internal void SetIsFinished(TimeSpan finishTime) {
            SetFinishedOnNextUpdate = true;
            FinishTime = finishTime;
        }

        /// <summary>
        /// Sets starting positions for this car.
        /// </summary>
        /// <param name="overall"></param>
        /// <param name="inclass"></param>
        internal void SetStartingPositions(int overall, int inclass) {
            StartPos = overall;
            StartPosInClass = inclass;
        }

        private void AddDriver(DriverInfo driverInfo) {
            Drivers.Add(new DriverData(driverInfo));
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
        public static double? CalculateGap(CarData from, CarData to) {
            if (from?.NewData == null
                || to?.NewData == null
                || from.OldData == null
                || to.OldData == null
                || from.CarIndex == to.CarIndex
                || !to.HasCrossedStartLine
                || !from.HasCrossedStartLine
                || from.OffsetLapUpdate != OffsetLapUpdateType.None
                || to.OffsetLapUpdate != OffsetLapUpdateType.None
            )
                return null;

            var flaps = from.NewData.Laps;
            var tlaps = to.NewData.Laps;

            // If one of the cars jumped to pits there is no correct way to calculate the gap
            if (flaps == tlaps && (from.JumpedToPits || to.JumpedToPits))
                return null;

            if (from.IsFinished && to.IsFinished) {
                if (flaps == tlaps) {
                    return ((TimeSpan)from.FinishTime).TotalSeconds - ((TimeSpan)to.FinishTime).TotalSeconds;
                } else {
                    return tlaps - flaps + 100_000;
                }
            }

            // Fixes wrong gaps after finish on cars that haven't finished and are in pits.
            // Without this the gap could be off by one lap from the gap calculated from completed laps.
            // This is correct if the session is not finished as you could go out and complete that lap.
            // If session has finished you cannot complete that lap.
            if (tlaps != flaps &&
                (to.IsFinished && !from.IsFinished && from.NewData.IsInPitlane
                || from.IsFinished && !to.IsFinished && to.NewData.IsInPitlane)
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
                    || Values.TrackData == null
                ) {
                    return null;
                }

                if (TrackData.LapInterpolators[to.CarClass] == null && TrackData.LapInterpolators[from.CarClass] == null) {
                    // lap data is not available, use naive distance based calculation
                    return CalculateNaiveGap(distBetween);
                }

                double? gap;
                var cls = TrackData.LapInterpolators[to.CarClass] != null ? to.CarClass : from.CarClass;
                if (distBetween > 0) { // `to` is ahead of `from`
                    gap = CalculateGapBetweenPos(from.GetSplinePosTime(cls), to.GetSplinePosTime(cls), TrackData.LapInterpolators[cls].LapTime);
                } else { // `to` is behind of `from`
                    gap = -CalculateGapBetweenPos(to.GetSplinePosTime(cls), from.GetSplinePosTime(cls), TrackData.LapInterpolators[cls].LapTime);
                }
                return gap;
            }
        }

        public static double? CalculateOnTrackGap(CarData from, CarData to) {
            if (from?.NewData == null
                 || to?.NewData == null
                 || from.OldData == null
                 || to.OldData == null
                 || from.CarIndex == to.CarIndex
                 || from.OffsetLapUpdate != OffsetLapUpdateType.None
                 || to.OffsetLapUpdate != OffsetLapUpdateType.None
                 || Values.TrackData == null
             )
                return null;

            var fromPos = from.NewData.SplinePosition;
            var toPos = to.NewData.SplinePosition;
            var relativeSplinePos = CalculateRelativeSplinePosition(fromPos, toPos);

            if (TrackData.LapInterpolators[to.CarClass] == null && TrackData.LapInterpolators[from.CarClass] == null) {
                // lap data is not available, use naive distance based calculation
                return CalculateNaiveGap(relativeSplinePos);
            }

            double? gap;
            var cls = TrackData.LapInterpolators[to.CarClass] != null ? to.CarClass : from.CarClass;
            if (relativeSplinePos < 0) {
                gap = -CalculateGapBetweenPos(from.GetSplinePosTime(cls), to.GetSplinePosTime(cls), TrackData.LapInterpolators[cls].LapTime);
            } else {
                gap = CalculateGapBetweenPos(to.GetSplinePosTime(cls), from.GetSplinePosTime(cls), TrackData.LapInterpolators[cls].LapTime);
            }
            return gap;
        }

        private static double CalculateNaiveGap(double splineDist) {
            var dist = splineDist * Values.TrackData.TrackMeters;
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
            if (NewData == null || otherCar?.NewData == null)
                return null;
            return CalculateRelativeSplinePosition(NewData.SplinePosition, otherCar.NewData.SplinePosition);
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
        private double GetSplinePosTime(CarClass cls) {
            // Same interpolated value is needed multiple times in one update, thus cache results.
            var pos = _splinePositionTime[cls];
            if (pos != _splinePositionTime.DefaultValue) {
                return (double)pos;
            }

            var interp = TrackData.LapInterpolators[cls];
            if (NewData != null && interp != null) {
                var result = interp.Interpolator.Interpolate(NewData.SplinePosition);
                _splinePositionTime[cls] = result;
                return result;
            } else {
                return -1;
            }
        }
    }
}