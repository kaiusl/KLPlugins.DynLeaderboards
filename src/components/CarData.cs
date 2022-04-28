using KLPlugins.DynLeaderboards.Driver;
using KLPlugins.DynLeaderboards.ksBroadcastingNetwork;
using KLPlugins.DynLeaderboards.ksBroadcastingNetwork.Structs;
using KLPlugins.DynLeaderboards.Realtime;
using KLPlugins.DynLeaderboards.Track;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KLPlugins.DynLeaderboards.Car {
    class CarData {
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

        public bool JumpedToPits { get; private set; } = false;
        public bool HasCrossedStartLine { get; private set; } = true;

        internal bool IsFirstUpdate { get; private set; } = true;
        internal bool SetFinishedOnNextUpdate { get; private set; } = false;
        internal bool IsFinalRealtimeCarUpdateAdded { get; private set; } = false;
        internal bool OffsetLapUpdate { get; private set; } = false;
        internal int MissedRealtimeUpdates { get; set; } = 0;

        private double? _stintStartTime = null;
        private CarClassArray<double?> _splinePositionTime = new CarClassArray<double?>(null);
        private bool _lapUpdatedAfterOffsetLapUpdate = true;
        private int _lapAtOffsetLapUpdate = 0;

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
            if (i == 0) { return Drivers.ElementAtOrDefault(CurrentDriverIndex); }
            if (i <= CurrentDriverIndex) { return Drivers.ElementAtOrDefault(i - 1); }
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
            ) return; // All drivers are same

            // Fix drivers list
            for (int i = 0; i < info.Drivers.Count; i++) {
                var currentDriver = Drivers[i];
                var newDriver = info.Drivers[i];
                if (currentDriver.Equals(newDriver)) continue;

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

            // Wait for one more update at the beginning of session, so we have all relevant data for calculations below
            if (IsFinalRealtimeCarUpdateAdded || OldData == null) return;

            HandleOffsetLapUpdates();
            if (NewData?.DriverIndex != null) CurrentDriverIndex = NewData.DriverIndex;

            if (realtimeData.IsRace) {
                CheckForCrossingStartLine();
                TotalSplinePosition = NewData.SplinePosition + NewData.Laps;
                UpdatePitInfo();
            }
            CheckForRTG();

            if (NewData.Laps != OldData.Laps) {
                CurrentDriver.OnLapFinished(NewData.LastLap);
            }

            UpdateStintInfo();
            UpdateBestLapSectors();

            MaxSpeed = Math.Max(MaxSpeed, NewData.Kmh);
            if (SetFinishedOnNextUpdate) {
                DynLeaderboardsPlugin.LogInfo($"Car #{RaceNumber} will finish on next update. Step 2");
                IsFinalRealtimeCarUpdateAdded = true;
            }

            #region Local functions

            void CheckForCrossingStartLine() {
                // Initial update before the start of the race
                if (realtimeData.Phase == SessionPhase.PreFormation
                    && HasCrossedStartLine
                    && NewData.SplinePosition > 0.5
                    && NewData.Laps == 0
                ) {
                    DynLeaderboardsPlugin.LogInfo($"#{RaceNumber}: set HasCrossedStartLine = false");
                    HasCrossedStartLine = false;
                }

                if (!HasCrossedStartLine && NewData.SplinePosition < 0.1 && OldData.SplinePosition > 0.9
                ) {
                    HasCrossedStartLine = true;
                    DynLeaderboardsPlugin.LogInfo($"#{RaceNumber}: set HasCrossedStartLine = true");
                }
            }

            void CheckForRTG() {
                if (realtimeData.IsRace
                    && !SetFinishedOnNextUpdate // It's okay to jump to the pits after finishing
                    && NewData.CarLocation == CarLocationEnum.Pitlane
                    && OldData.CarLocation == CarLocationEnum.Track
                ) {
                    JumpedToPits = true;
                }

                if (JumpedToPits && NewData.CarLocation != CarLocationEnum.Pitlane) {
                    JumpedToPits = false;
                }
            }

            void HandleOffsetLapUpdates() {
                // Check for offset lap update
                if (!OffsetLapUpdate
                    && NewData.Laps != OldData.Laps
                    && (NewData.SplinePosition > 0.9 || OldData.SplinePosition < 0.1)
                ) {
                    OffsetLapUpdate = true;
                    DynLeaderboardsPlugin.LogError($"#{RaceNumber}: laps updated before spline position was reset. NewData: laps={NewData.Laps}, sp={NewData.SplinePosition}, OldData: laps={OldData.Laps}, sp={OldData.SplinePosition}");
                } else if (!OffsetLapUpdate
                    && NewData.SplinePosition < 0.1
                    && OldData.SplinePosition > 0.9
                    && NewData.Laps == OldData.Laps
                    && NewData.Laps != 0
                ) {
                    OffsetLapUpdate = true;
                    _lapUpdatedAfterOffsetLapUpdate = false;
                    _lapAtOffsetLapUpdate = NewData.Laps;
                    DynLeaderboardsPlugin.LogError($"#{RaceNumber}: spline position was reset before laps were updated. NewData: laps={NewData.Laps}, sp={NewData.SplinePosition}, OldData: laps={OldData.Laps}, sp={OldData.SplinePosition}");
                }

                // Check if it's fixed
                if (!_lapUpdatedAfterOffsetLapUpdate && NewData.Laps != _lapAtOffsetLapUpdate) {
                    _lapUpdatedAfterOffsetLapUpdate = true;
                }

                if (OffsetLapUpdate
                    && ((NewData.SplinePosition < 0.9 && _lapUpdatedAfterOffsetLapUpdate) || NewData.SplinePosition > 0.01)
                    ) {
                    DynLeaderboardsPlugin.LogError($"#{RaceNumber}: offset lap update removed, all ok now again.");
                    OffsetLapUpdate = false;
                }
            }


            void UpdatePitInfo() {
                // Pit started
                if ((!OldData.IsInPitlane && NewData.IsInPitlane) // Entered pitlane
                    || (PitEntryTime == null && NewData.IsInPitlane && !realtimeData.IsPreSession) // We join/start SimHub mid session
                ) {
                    PitCount++;
                    PitEntryTime = realtimeData.SessionRunningTime.TotalSeconds;
                    DynLeaderboardsPlugin.LogInfo($"#{RaceNumber} entered pitlane at {PitEntryTime}.");
                }

                // Pit ended
                if (PitEntryTime != null && !NewData.IsInPitlane) {
                    // Left the pitlane
                    LastPitTime = realtimeData.SessionRunningTime.TotalSeconds - PitEntryTime;
                    TotalPitTime += (double)LastPitTime;
                    PitEntryTime = null;
                    CurrentTimeInPits = null;
                    DynLeaderboardsPlugin.LogInfo($"#{RaceNumber} exited pitlane. Time in pits (Total,Last) = ({TotalPitTime:00.0}s,{LastPitTime:00.0}s)");
                }

                if (PitEntryTime != null) {
                    CurrentTimeInPits = realtimeData.SessionRunningTime.TotalSeconds - PitEntryTime;
                }
            }

            void UpdateStintInfo() {
                if (NewData.Laps != OldData.Laps) {
                    CurrentStintLaps++;
                }

                // Stint started
                if ((OldData.IsInPitlane && !NewData.IsInPitlane) // Pitlane exit
                    || (realtimeData.IsRace && realtimeData.IsSessionStart) // Race start
                    || (_stintStartTime == null && NewData.IsOnTrack && !realtimeData.IsPreSession) // We join/start SimHub mid session
                ) {
                    _stintStartTime = realtimeData.SessionRunningTime.TotalSeconds;
                    DynLeaderboardsPlugin.LogInfo($"#{RaceNumber} started stint at {_stintStartTime}");
                }

                // Stint ended
                if (!OldData.IsInPitlane && NewData.IsInPitlane && _stintStartTime != null) {
                    LastStintTime = realtimeData.SessionRunningTime.TotalSeconds - (double)_stintStartTime;
                    CurrentDriver.OnStintEnd((double)LastStintTime);
                    _stintStartTime = null;
                    CurrentStintTime = null;
                    LastStintLaps = CurrentStintLaps;
                    CurrentStintLaps = 0;
                    DynLeaderboardsPlugin.LogInfo($"#{RaceNumber} stint ended: {LastStintLaps} laps in {LastStintTime / 60.0:00.0}min");
                }

                if (_stintStartTime != null) {
                    CurrentStintTime = realtimeData.SessionRunningTime.TotalSeconds - (double)_stintStartTime;
                }
            }

            void UpdateBestLapSectors() {
                // Note that NewData.BestSessionLap doesn't contain the sectors of that best lap but the best sectors.
                if (OldData.Laps != NewData.Laps
                    && NewData.LastLap.IsValidForBest
                    && NewData.LastLap.Laptime == NewData.BestSessionLap.Laptime
                ) {
                    for (int i = 0; i < 3; i++) {
                        BestLapSectors[i] = NewData.LastLap.Splits[i];
                    }
                }
            }
            #endregion
        }

        internal void OnRealtimeUpdateFirstPass() {
            _splinePositionTime.Reset();
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
            int classPos
        ) {
            IsOverallBestLapCar = CarIndex == overallBestLapCar?.CarIndex;
            IsClassBestLapCar = CarIndex == classBestLapCar?.CarIndex;

            InClassPos = classPos;
            OverallPos = overallPos;

            if (realtimeData.OldData.Phase == SessionPhase.SessionOver && realtimeData.IsRace) {
                // We also need to check finished here (after positions update) to detect leaders finish
                CheckIsFinished();
            }

            if (OffsetLapUpdate) return; // Gaps could be offset by a lap, wait for the next update
            SetGaps();
            SetLapDeltas();

            #region Local functions

            void SetGaps() {
                if (realtimeData.IsRace) {
                    // Use time gaps on track
                    // We update the gap only if CalculateGap returns a proper value because we don't want to update the gap if one of the cars has finished. 
                    // That would result in wrong gaps. We keep the gaps at the last valid value and update once both cars have finished.

                    GapToLeader = CalculateGap(this, leaderCar);
                    GapToClassLeader = CalculateGap(this, classLeaderCar);
                    GapToFocusedTotal = CalculateGap(focusedCar, this);
                    GapToAhead = CalculateGap(this, carAhead);
                    GapToAheadInClass = CalculateGap(this, carAheadInClass);
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

                GapToFocusedOnTrack = CalculateOnTrackGap(this, focusedCar);
                GapToAheadOnTrack = CalculateOnTrackGap(carAheadOnTrack, this);
            }

            void SetLapDeltas() {
                var thisBest = NewData?.BestSessionLap?.Laptime;
                var thisLast = NewData?.LastLap?.Laptime;
                if (thisBest == null && thisLast == null) return;

                var overallBest = overallBestLapCar?.NewData?.BestSessionLap?.Laptime;
                var classBest = classBestLapCar?.NewData?.BestSessionLap?.Laptime;
                var leaderBest = leaderCar?.NewData?.BestSessionLap?.Laptime;
                var classLeaderBest = classLeaderCar?.NewData?.BestSessionLap?.Laptime;
                var focusedBest = focusedCar?.NewData?.BestSessionLap?.Laptime;
                var aheadBest = carAhead?.NewData?.BestSessionLap?.Laptime;
                var aheadInClassBest = carAheadInClass?.NewData?.BestSessionLap?.Laptime;

                if (thisBest != null) {
                    if (overallBest != null) BestLapDeltaToOverallBest = (double)thisBest - (double)overallBest;
                    if (classBest != null) BestLapDeltaToClassBest = (double)thisBest - (double)classBest;
                    if (leaderBest != null) BestLapDeltaToLeaderBest = (double)thisBest - (double)leaderBest;
                    if (classLeaderBest != null) BestLapDeltaToClassLeaderBest = (double)thisBest - (double)classLeaderBest;
                    BestLapDeltaToFocusedBest = focusedBest != null ? (double)thisBest - (double)focusedBest : (double?)null;
                    BestLapDeltaToAheadBest = aheadBest != null ? (double)thisBest - (double)aheadBest : (double?)null;
                    BestLapDeltaToAheadInClassBest = aheadInClassBest != null ? (double)thisBest - (double)aheadInClassBest : (double?)null;
                }

                if (thisLast != null) {
                    if (overallBest != null) LastLapDeltaToOverallBest = (double)thisLast - (double)overallBest;
                    if (classBest != null) LastLapDeltaToClassBest = (double)thisLast - (double)classBest;
                    if (leaderBest != null) LastLapDeltaToLeaderBest = (double)thisLast - (double)leaderBest;
                    if (classLeaderBest != null) LastLapDeltaToClassLeaderBest = (double)thisLast - (double)classLeaderBest;
                    LastLapDeltaToFocusedBest = focusedBest != null ? (double)thisLast - (double)focusedBest : (double?)null;
                    LastLapDeltaToAheadBest = aheadBest != null ? (double)thisLast - (double)aheadBest : (double?)null;
                    LastLapDeltaToAheadInClassBest = aheadInClassBest != null ? (double)thisLast - (double)aheadInClassBest : (double?)null;

                    if (thisBest != null) LastLapDeltaToOwnBest = (double)thisLast - (double)thisBest;

                    var leaderLast = leaderCar?.NewData?.LastLap?.Laptime;
                    var classLeaderLast = classLeaderCar?.NewData?.LastLap?.Laptime;
                    var focusedLast = focusedCar?.NewData?.LastLap?.Laptime;
                    var aheadLast = carAhead?.NewData?.LastLap?.Laptime;
                    var aheadInClassLast = carAheadInClass?.NewData?.LastLap?.Laptime;

                    if (leaderLast != null) LastLapDeltaToLeaderLast = (double)thisLast - (double)leaderLast;
                    if (classLeaderLast != null) LastLapDeltaToClassLeaderLast = (double)thisLast - (double)classLeaderLast;
                    LastLapDeltaToFocusedLast = focusedLast != null ? (double)thisLast - (double)focusedLast : (double?)null;
                    LastLapDeltaToAheadLast = aheadLast != null ? (double)thisLast - (double)aheadLast : (double?)null;
                    LastLapDeltaToAheadInClassLast = aheadInClassLast != null ? (double)thisLast - (double)aheadInClassLast : (double?)null;
                }
            }

            #endregion


        }

        internal void CheckIsFinished() {
            if (!IsFinished && IsFinalRealtimeCarUpdateAdded && !OffsetLapUpdate) {
                IsFinished = true;
                DynLeaderboardsPlugin.LogInfo($"Car #{RaceNumber} finished at {FinishTime}");
            }
        }

        internal void SetIsFinished(TimeSpan finishTime) {
            SetFinishedOnNextUpdate = true;
            FinishTime = finishTime;
            DynLeaderboardsPlugin.LogInfo($"Car #{RaceNumber} will finish on next update. Step 1");
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
            ) return null;

            var flaps = from.NewData.Laps;
            var tlaps = to.NewData.Laps;

            // If one of the cars jumped to pits there is no correct way to calculate the gap
            if (flaps == tlaps && (from.JumpedToPits || to.JumpedToPits)) return null;

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
                    || (TrackData.LapInterpolators[to.CarClass] == null && TrackData.LapInterpolators[from.CarClass] == null)
                ) return null;

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
             ) return null;

            var fromPos = from.NewData.SplinePosition;
            var toPos = to.NewData.SplinePosition;
            var relativeSplinePos = CalculateRelativeSplinePosition(fromPos, toPos);

            // We don't have lap interpolators available
            if (Values.TrackData == null
                || (TrackData.LapInterpolators[to.CarClass] == null && TrackData.LapInterpolators[from.CarClass] == null)
            ) return null;

            double? gap;
            var cls = TrackData.LapInterpolators[to.CarClass] != null ? to.CarClass : from.CarClass;
            if (relativeSplinePos < 0) {
                gap = -CalculateGapBetweenPos(from.GetSplinePosTime(cls), to.GetSplinePosTime(cls), TrackData.LapInterpolators[cls].LapTime);
            } else {
                gap = CalculateGapBetweenPos(to.GetSplinePosTime(cls), from.GetSplinePosTime(cls), TrackData.LapInterpolators[cls].LapTime);
            }
            return gap;
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
            if (NewData == null || otherCar?.NewData == null) return null;
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
                return pos;
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
