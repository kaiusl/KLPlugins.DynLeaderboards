using KLPlugins.DynLeaderboards.Enums;
using KLPlugins.DynLeaderboards.src.ksBroadcastingNetwork.Structs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KLPlugins.DynLeaderboards.ksBroadcastingNetwork.Structs {
    public class CarData {
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
        public double PitEntryTime { get; private set; } = double.NaN;
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

        internal int MissedRealtimeUpdates { get; set; } = 0;

        private double? _stintStartTime = null;
        private CarClassArray<double> _splinePositionTime = new CarClassArray<double>(-1);
        private bool _isRaceFinishPosSet = false;
        public bool IsFirstUpdate = true;

        public bool SetFinishedOnNextUpdate = false;
        public bool IsFinalRealtimeCarUpdateAdded = false;

        public bool HasCrossedStartLine = true;

        ////////////////////////

        public CarData(CarInfo info, RealtimeCarUpdate update) {
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

        private void AddDriver(DriverInfo driverInfo) {
            Drivers.Add(new DriverData(driverInfo));
        }

        public double? GetDriverTotalDrivingTime(int i) {
            return GetDriver(i)?.GetTotalDrivingTime(i == 0, CurrentStintTime);
        }

        #region Entry list update

        /// <summary>
        /// Updates this cars static info. Should be called when new entry list update for this car is received.
        /// </summary>
        /// <param name="info"></param>
        public void UpdateCarInfo(CarInfo info) {
            // Only thing that can change is drivers
            // We need to make sure that the order is as specified by new info
            // But also add new drivers. We keep old drivers but move them to the end of list
            // as they might rejoin and then we need to have the old data. (I'm not sure if ACC keeps those drivers or not, but we make sure to keep the data.)
            CurrentDriverIndex = info.CurrentDriverIndex;
            if (Drivers.Count == info.Drivers.Count && Drivers.Zip(info.Drivers, (a, b) => a.Equals(b)).All(x => x)) {
                DynLeaderboardsPlugin.LogInfo($"All drivers same.");
                return;
            } // All drivers are same

            var currentDrivers = "";
            foreach (var d in Drivers) {
                currentDrivers += $"{d.FirstName} {d.LastName};";
            }

            for (int i = 0; i < info.Drivers.Count; i++) {
                DynLeaderboardsPlugin.LogInfo($"Current drivers are: {currentDrivers}");

                var currentDriver = Drivers[i];
                var newDriver = info.Drivers[i];
                DynLeaderboardsPlugin.LogInfo($"Comparing drivers #{i}: current:{currentDriver.FirstName} {currentDriver.LastName}, new:{newDriver.FirstName} {newDriver.LastName}");

                if (currentDriver.Equals(newDriver)) {
                    DynLeaderboardsPlugin.LogInfo($"Result: Drivers are same.");
                    continue; // this driver is same
                }

                var oldIdx = Drivers.FindIndex(x => x.Equals(newDriver));
                if (oldIdx == -1) {
                    // Must be new driver
                    DynLeaderboardsPlugin.LogInfo($"Found new driver. Inserting.");
                    Drivers.Insert(i, new DriverData(newDriver));
                } else {
                    // Driver is present but it's order has changed
                    DynLeaderboardsPlugin.LogInfo($"Drivers are reordered.");

                    var old = Drivers[oldIdx];
                    Drivers.RemoveAt(oldIdx);
                    Drivers.Insert(i, old);
                }
            }
        }

        #endregion

        #region On realtime car update

        /// <summary>
        /// Sets starting positions for this car. 
        /// </summary>
        /// <param name="overall"></param>
        /// <param name="inclass"></param>
        public void SetStartingPositions(int overall, int inclass) {
            StartPos = overall;
            StartPosInClass = inclass;
        }

        public bool OffsetLapUpdate = false;
        private bool _lapUpdatedAfterOffsetLapUpdate = true;
        private int _lapAtOffsetLapUpdate = 0;

        /// <summary>
        /// Updates this cars data. Should be called when RealtimeCarUpdate for this car is received.
        /// </summary>
        /// <param name="update"></param>
        /// <param name="realtimeData"></param>
        public void OnRealtimeCarUpdate(RealtimeCarUpdate update, RealtimeData realtimeData) {
            // If the race is finished we don't care about any of the realtime updates anymore.
            // We have set finished positions in ´OnRealtimeUpdate´ and that's really all that matters
            //if (IsFinished) return;

            OldData = NewData;
            NewData = update;

            // Wait for one more update at the beginning of session, so we have all relevant data for calculations below
            if (IsFinalRealtimeCarUpdateAdded || OldData == null) return;
            CheckForOffsetLapUpdate();
            SetCurrentDriverIndex();

            if (realtimeData.IsRace) {
                CheckForCrossingStartLine(realtimeData);
                TotalSplinePosition = NewData.SplinePosition + NewData.Laps;
                UpdatePitInfo(realtimeData);
            }
            CheckForRTG(realtimeData);

            if (NewData.Laps != OldData.Laps) {
                CurrentDriver.OnLapFinished(NewData.LastLap);
            }

            UpdateStintInfo(realtimeData);
            UpdateBestLapSectors();

            MaxSpeed = Math.Max(MaxSpeed, NewData.Kmh);
            if (SetFinishedOnNextUpdate) {
                DynLeaderboardsPlugin.LogInfo($"Car #{RaceNumber} will finish on next update. Step 2");

                IsFinalRealtimeCarUpdateAdded = true;
            }
        }

        private void CheckForCrossingStartLine(RealtimeData realtimeData) {
            // On certain tracks (Spa) first half of the grid is ahead of the start/finish line,
            // need to add the line crossing lap, otherwise they will be shown lap behind
            if (HasCrossedStartLine && realtimeData.Phase == SessionPhase.PreFormation
                && NewData.SplinePosition > 0.5
                && NewData.Laps == 0
            ) {
                DynLeaderboardsPlugin.LogInfo($"#{RaceNumber}: set HasCrossedStartLine = false");
                HasCrossedStartLine = false;
            }

            if (!HasCrossedStartLine && NewData.SplinePosition < 0.1 && OldData.SplinePosition > 0.9) {
                HasCrossedStartLine = true;
                DynLeaderboardsPlugin.LogInfo($"#{RaceNumber}: set HasCrossedStartLine = true");
            }
        }

        private void CheckForRTG(RealtimeData realtimeData) {
            if (realtimeData.IsRace && !SetFinishedOnNextUpdate && NewData.CarLocation == CarLocationEnum.Pitlane && OldData.CarLocation == CarLocationEnum.Track) {
                // It's okay to jump to the pits after finishing
                JumpedToPits = true;
            }

            if (JumpedToPits && NewData.CarLocation != CarLocationEnum.Pitlane) {
                JumpedToPits = false;
            }
        }

        private void SetCurrentDriverIndex() {
            if (NewData?.DriverIndex != null) CurrentDriverIndex = NewData.DriverIndex;
        }

        private void CheckForOffsetLapUpdate() {
            if (!OffsetLapUpdate && NewData.Laps != OldData.Laps && (NewData.SplinePosition > 0.9 || OldData.SplinePosition < 0.1)) {
                OffsetLapUpdate = true;
                DynLeaderboardsPlugin.LogError($"#{RaceNumber}: laps updated before spline position was reset. NewData: laps={NewData.Laps}, sp={NewData.SplinePosition}, OldData: laps={OldData.Laps}, sp={OldData.SplinePosition}");
            } else if (!OffsetLapUpdate && NewData.SplinePosition < 0.1 && OldData.SplinePosition > 0.9 && NewData.Laps == OldData.Laps && NewData.Laps != 0) {
                OffsetLapUpdate = true;
                _lapUpdatedAfterOffsetLapUpdate = false;
                _lapAtOffsetLapUpdate = NewData.Laps;
                DynLeaderboardsPlugin.LogError($"#{RaceNumber}: spline position was reset before laps were updated. NewData: laps={NewData.Laps}, sp={NewData.SplinePosition}, OldData: laps={OldData.Laps}, sp={OldData.SplinePosition}");
            }

            if (!_lapUpdatedAfterOffsetLapUpdate && NewData.Laps != _lapAtOffsetLapUpdate) {
                _lapUpdatedAfterOffsetLapUpdate = true;
            }

            if (OffsetLapUpdate && NewData.SplinePosition < 0.9 && _lapUpdatedAfterOffsetLapUpdate) {
                DynLeaderboardsPlugin.LogError($"#{RaceNumber}: offset lap update removed, all ok now again.");
                OffsetLapUpdate = false;
            }
        }

        private void UpdatePitInfo(RealtimeData realtimeData) {
            if (OldData.CarLocation != CarLocationEnum.Pitlane && NewData.CarLocation == CarLocationEnum.Pitlane // Entered pitlane
                || (double.IsNaN(PitEntryTime)
                    && NewData.CarLocation == CarLocationEnum.Pitlane
                    && (realtimeData.IsSession
                    || realtimeData.IsPostSession)
                    ) // We join/start SimHub mid session
                ) {
                PitCount++;
                PitEntryTime = realtimeData.SessionRunningTime.TotalSeconds;
                DynLeaderboardsPlugin.LogInfo($"#{RaceNumber} entered pitlane at {PitEntryTime}.");
            }

            if (!double.IsNaN(PitEntryTime) && NewData.CarLocation != CarLocationEnum.Pitlane) {
                // Left the pitlane
                LastPitTime = (realtimeData.SessionRunningTime).TotalSeconds - PitEntryTime;
                TotalPitTime += (double)LastPitTime;
                PitEntryTime = double.NaN;
                CurrentTimeInPits = null;
                DynLeaderboardsPlugin.LogInfo($"#{RaceNumber} exited pitlane. Time in pits (Total,Last) = ({TotalPitTime:00.0}s,{LastPitTime:00.0}s)");
            }

            if (!double.IsNaN(PitEntryTime)) {
                CurrentTimeInPits = realtimeData.SessionRunningTime.TotalSeconds - PitEntryTime;
            }

        }

        private void UpdateStintInfo(RealtimeData realtimeData) {
            // Lap finished
            if (NewData.Laps != OldData.Laps) {
                CurrentStintLaps++;
            }

            // Stint started
            if ((OldData.CarLocation == CarLocationEnum.Pitlane && NewData.CarLocation != CarLocationEnum.Pitlane) // Pitlane exit
                || (realtimeData.IsRace && realtimeData.IsSessionStart) // Race start
                || (_stintStartTime == null && NewData.CarLocation == CarLocationEnum.Track && (realtimeData.IsSession || realtimeData.IsPostSession)) // We join/start SimHub mid session
            ) {
                _stintStartTime = realtimeData.SessionRunningTime.TotalSeconds;
                DynLeaderboardsPlugin.LogInfo($"#{RaceNumber} started stint at {_stintStartTime}");
            }

            // Stint ended
            if (OldData.CarLocation != CarLocationEnum.Pitlane && NewData.CarLocation == CarLocationEnum.Pitlane) { // Pitlane entry
                if (_stintStartTime != null) {
                    LastStintTime = realtimeData.SessionRunningTime.TotalSeconds - (double)_stintStartTime;
                    CurrentDriver.OnStintEnd((double)LastStintTime);
                    _stintStartTime = null;
                    CurrentStintTime = null;
                }
                LastStintLaps = CurrentStintLaps;
                CurrentStintLaps = 0;

                DynLeaderboardsPlugin.LogInfo($"#{RaceNumber} stint ended: {LastStintLaps} laps in {LastStintTime / 60.0:00.0}min");
            }

            if (_stintStartTime != null) {
                CurrentStintTime = realtimeData.SessionRunningTime.TotalSeconds - (double)_stintStartTime;
            }
        }

        private void UpdateBestLapSectors() {
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

        #region On realtime update

        public void OnRealtimeUpdate(
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

            //if (IsFinished && _isRaceFinishPosSet) return;
            InClassPos = classPos;
            OverallPos = overallPos;
            _splinePositionTime.Reset();

            if (realtimeData.OldData.Phase == SessionPhase.SessionOver && realtimeData.IsRace) {
                // We also need to check finished here (after positions update) to detect leaders finish
                CheckIsFinished();
            }

            if (OffsetLapUpdate) return; // Gaps could be offset by a lap, wait for the next update
            SetGaps(realtimeData, leaderCar, classLeaderCar, focusedCar, carAhead, carAheadInClass, carAheadOnTrack);
            SetLapDeltas(leaderCar, classLeaderCar, focusedCar, carAhead, carAheadInClass, overallBestLapCar, classBestLapCar);

            if (IsFinished) _isRaceFinishPosSet = true;
        }

        public void CheckIsFinished() {
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


        private void SetLapDeltas(
            CarData leaderCar,
            CarData classLeaderCar,
            CarData focusedCar,
            CarData carAhead,
            CarData carAheadInClass,
            CarData overallBestLapCar,
            CarData classBestLapCar
        ) {
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
                if (classBest != null) BestLapDeltaToClassBest = (double)thisBest - (double)(classBest);
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

        #region Gap calculations

        private void SetGaps(
            RealtimeData realtimeData,
            CarData leader,
            CarData classLeader,
            CarData focused,
            CarData carAhead,
            CarData carAheadInClass,
            CarData carAheadOnTrack) {
            if (realtimeData.IsRace) {
                CalculateRaceGaps(leader, classLeader, focused, carAhead, carAheadInClass);
            } else {
                CalculateNonRaceGaps(leader, classLeader, focused, carAhead, carAheadInClass);
            }
            CalculateRelativeOnTrackGaps(focused, carAheadOnTrack);
        }

        private void CalculateRelativeOnTrackGaps(CarData focused, CarData carAheadOnTrack) {
            var gap = CalculateOnTrackGap(this, focused);
            if (gap != null) GapToFocusedOnTrack = gap;
            if (carAheadOnTrack == null) {
                GapToAheadOnTrack = null;
            } else {
                var gapToAheadOnTrack = CalculateOnTrackGap(carAheadOnTrack, this);
                if (gapToAheadOnTrack != null) GapToAheadOnTrack = gapToAheadOnTrack;
            }
        }

        private void CalculateNonRaceGaps(CarData leader, CarData classLeader, CarData focused, CarData carAhead, CarData carAheadInClass) {
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

            var leaderBestLap = leader?.NewData?.BestSessionLap?.Laptime;
            GapToLeader = leaderBestLap != null ? ((double)thisBestLap - (double)leaderBestLap) : (double?)null;

            var classLeaderBestLap = classLeader?.NewData?.BestSessionLap?.Laptime;
            GapToClassLeader = classLeaderBestLap != null ? ((double)thisBestLap - (double)classLeaderBestLap) : (double?)null;

            var focusedBestLap = focused?.NewData?.BestSessionLap?.Laptime;
            GapToFocusedTotal = focusedBestLap != null ? ((double)thisBestLap - (double)focusedBestLap) : (double?)null;

            var aheadBestLap = carAhead?.NewData?.BestSessionLap?.Laptime;
            GapToAhead = aheadBestLap != null ? ((double)thisBestLap - (double)aheadBestLap) : (double?)null;

            var aheadInClassBestLap = carAheadInClass?.NewData?.BestSessionLap?.Laptime;
            GapToAheadInClass = aheadInClassBestLap != null ? ((double)thisBestLap - (double)aheadInClassBestLap) : (double?)null;
        }

        private void CalculateRaceGaps(CarData leader, CarData classLeader, CarData focused, CarData carAhead, CarData carAheadInClass) {
            // Use time gaps on track
            // We update the gap only if CalculateGap returns a proper value because we don't want to update the gap if one of the cars has finished. 
            // That would result in wrong gaps. We keep the gaps at the last valid value and update once both cars have finished.

            var gapToLeader = CalculateGap(this, leader);
            //if (gapToLeader != null) {
            GapToLeader = gapToLeader;
            //} else if (NewData?.CarLocation != CarLocationEnum.Track) {
            //    GapToLeader = null;
            //}

            if (classLeader.CarIndex == CarIndex) {
                GapToClassLeader = 0.0;
            } else {
                var gapToClassLeader = CalculateGap(this, classLeader);
                //if (gapToClassLeader != null) {
                GapToClassLeader = gapToClassLeader;
                //} else if (NewData?.CarLocation != CarLocationEnum.Track) {
                //    GapToClassLeader = null;
                //}
            }

            if (focused.CarIndex == CarIndex) {
                GapToFocusedTotal = 0.0;
            } else {
                var gapToFocusedTotal = CalculateGap(focused, this);
                //if (gapToFocusedTotal != null) 
                GapToFocusedTotal = gapToFocusedTotal;
                //else if (NewData?.CarLocation != CarLocationEnum.Track) {
                //    GapToFocusedTotal = null;
                //}
            }

            if (carAhead == null) {
                GapToAhead = null;
            } else {
                var gapToAhead = CalculateGap(this, carAhead);
                //if (gapToAhead != null)
                GapToAhead = gapToAhead;
                //else if (NewData?.CarLocation != CarLocationEnum.Track) {
                //    GapToAhead = null;
                //}
            }

            if (carAheadInClass == null) {
                GapToAheadInClass = null;
            } else {
                var gapToAheadInClass = CalculateGap(this, carAheadInClass);
                //if (gapToAheadInClass != null) 
                GapToAheadInClass = gapToAheadInClass;
                //else if (NewData?.CarLocation != CarLocationEnum.Track) {
                //    GapToAheadInClass = null;
                //}
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
        public static double? CalculateGap(CarData from, CarData to) {
            if (from.CarIndex == to.CarIndex) return 0;

            if (from.NewData == null || to.NewData == null || from.OldData == null || to.OldData == null || !to.HasCrossedStartLine || !from.HasCrossedStartLine) return null;
            if (from.NewData.Laps == to.NewData.Laps && (from.JumpedToPits || to.JumpedToPits)) return null;

            var flaps = from.NewData.Laps;
            var tlaps = to.NewData.Laps;

            if (from.IsFinished && to.IsFinished) {
                if (flaps == tlaps) {
                    return ((TimeSpan)from.FinishTime).TotalSeconds - ((TimeSpan)to.FinishTime).TotalSeconds;
                } else {
                    return tlaps - flaps + 100_000;
                }
            }

            if (tlaps != flaps &&
                ((to.IsFinished && !from.IsFinished && from.NewData.CarLocation == CarLocationEnum.Pitlane)
                || (from.IsFinished && !to.IsFinished && to.NewData.CarLocation == CarLocationEnum.Pitlane))
            ) {
                return tlaps - flaps + 100_000;
            }

            var distBetween = to.TotalSplinePosition - from.TotalSplinePosition; // Negative if 'To' is behind

            if (distBetween <= -1) {
                // 'To' is more than a lap behind of 'from'
                return -Math.Floor(Math.Abs(distBetween)) + 100_000;
            } else if (distBetween >= 1) {
                // 'To' is more than a lap ahead of 'from'
                return Math.Floor(Math.Abs(distBetween)) + 100_000;
            } else {
                if (from.IsFinished || to.IsFinished) {
                    return null;
                }

                // We don't have lap interpolators available, use naive method to calculate the gap
                if (Values.TrackData == null
                    || (TrackData.LapInterpolators[to.CarClass] == null && TrackData.LapInterpolators[from.CarClass] == null)
                ) {
                    //LeaderboardPlugin.LogInfo("Used naive gap calculator");
                    return distBetween * Values.TrackData.TrackMeters / (175.0 / 3.6);
                }

                var fromPos = from.NewData.SplinePosition;
                var toPos = to.NewData.SplinePosition;
                double? gap;
                var cls = TrackData.LapInterpolators[to.CarClass] != null ? to.CarClass : from.CarClass;
                if (distBetween > 0) {
                    // To car is ahead of from, gap should be the time it takes 'from' car to reach 'to' car's position
                    // That is use 'from' lap data to calculate the gap
                    gap = CalculateGapBetweenPos((float)fromPos, (float)toPos, from.GetSplinePosTime(cls), to.GetSplinePosTime(cls), TrackData.LapInterpolators[cls].LapTime);
                } else {
                    // 'to' car is behind of 'from', gap should be the time it takes 'to' to reach 'from'
                    // That is use 'to' cars lap data to calculate the gap
                    //var cls = TrackData.LapInterpolators[to.CarClass] != null ? to.CarClass : from.CarClass;
                    gap = -CalculateGapBetweenPos((float)toPos, (float)fromPos, to.GetSplinePosTime(cls), from.GetSplinePosTime(cls), TrackData.LapInterpolators[cls].LapTime);
                }
                return gap;
            }
        }

        public static double? CalculateOnTrackGap(CarData from, CarData to) {
            if (from.CarIndex == to.CarIndex) return 0;

            var fromPos = from.NewData?.SplinePosition;
            var toPos = to.NewData?.SplinePosition;
            if (fromPos == null || toPos == null) return null;

            var relativeSplinePos = CalculateRelativeSplinePosition((float)fromPos, (float)toPos);

            // We don't have lap interpolators available, use naive method to calculate the gap
            if (Values.TrackData == null
                || (TrackData.LapInterpolators[to.CarClass] == null && TrackData.LapInterpolators[from.CarClass] == null)
            ) {
                //LeaderboardPlugin.LogInfo("Used naive gap calculator");
                return relativeSplinePos * Values.TrackData.TrackMeters / (175.0 / 3.6);
            }

            double? gap;
            var cls = TrackData.LapInterpolators[to.CarClass] != null ? to.CarClass : from.CarClass;
            if (relativeSplinePos < 0) {
                // To car is ahead of from, gap should be the time it takes 'from' car to reach 'to' car's position
                // That is use 'from' lap data to calculate the gap
                gap = -CalculateGapBetweenPos((float)fromPos, (float)toPos, from.GetSplinePosTime(cls), to.GetSplinePosTime(cls), TrackData.LapInterpolators[cls].LapTime);
            } else {
                // 'to' car is behind of 'from', gap should be the time it takes 'to' to reach 'from'
                // That is use 'to' cars lap data to calculate the gap
                //var cls = TrackData.LapInterpolators[to.CarClass] != null ? to.CarClass : from.CarClass;
                gap = CalculateGapBetweenPos((float)toPos, (float)fromPos, to.GetSplinePosTime(cls), from.GetSplinePosTime(cls), TrackData.LapInterpolators[cls].LapTime);
            }
            return gap;
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

        /// <summary>
        /// Calculates the gap in seconds from <paramref name="behindPos"/> to <paramref name="aheadPos"/> using lap data from <paramref name="lapInterp"/>.
        /// </summary>
        /// <returns>
        /// Positive value or <typeparamref name="double"><c>NaN</c> if gap cannot be calculated.
        /// </returns>
        /// <param name="behindPos"></param>
        /// <param name="aheadPos"></param>
        /// <param name="lapInterp"></param>
        /// <returns></returns>
        public static double CalculateGapBetweenPos(float behindPos, float aheadPos, double start, double end, double lapTime) {
            if (aheadPos < behindPos) {
                // Ahead is on another lap, gap is time for `behindpos` to end lap, and then reach aheadpos
                return lapTime - start + end;
            } else {
                // We must be on the same lap, gap is time for behindpos to reach aheadpos
                return end - start;
            }
        }

        /// <summary>
        /// Calculates relative spline position to <paramref name="otherCar"/>.
        /// 
        /// Car will be shown ahead if it's ahead by less than half a lap, otherwise it's behind.
        /// If result is positive then this car is ahead of other, if negative it's behind.
        /// </summary>
        /// <returns>
        /// Value between (-0.5, 0.5) or <typeparamref name="float"/><c>.NaN</c> if the result cannot be calculated.
        /// </returns>
        /// <param name="otherCar"></param>
        /// <returns></returns>
        public double CalculateRelativeSplinePosition(CarData otherCar) {
            if (NewData == null || otherCar.NewData == null) return double.NaN;
            return CalculateRelativeSplinePosition(NewData.SplinePosition, otherCar.NewData.SplinePosition);
        }

        /// <summary>
        /// Calculates relative spline position of <paramref name="thisPos"> to <paramref name="posRelativeTo"/>.
        /// 
        /// Position will be shown ahead if it's ahead by less than half a lap, otherwise it's behind.
        /// If result is positive then this position is ahead of other, if negative it's behind.
        /// </summary>
        /// <param name="thisPos"></param>
        /// <param name="posRelativeTo"></param>
        /// <returns></returns>
        public static double CalculateRelativeSplinePosition(double thisPos, double posRelativeTo) {
            var relSplinePos = thisPos - posRelativeTo;
            if (relSplinePos > 0.5) { // Pos is more than half a lap ahead, so technically it's closer from behind. Take one lap away to show it behind us.
                relSplinePos -= 1;
            } else if (relSplinePos < -0.5) { // Pos is more than half a lap behind, so it's in front. Add one lap to show it in front of us.
                relSplinePos += 1;
            }
            return relSplinePos;
        }

        #endregion

    }
}
