using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KLPlugins.Leaderboard.Enums;
using KLPlugins.Leaderboard.src.ksBroadcastingNetwork.Structs;
using MathNet.Numerics.Interpolation;

namespace KLPlugins.Leaderboard.ksBroadcastingNetwork.Structs {
    public class CarData {
        // Information from CarInfo
        public ushort CarIndex { get; }
        public CarType CarModelType { get; internal set; }
        public CarClass CarClass { get; internal set; }
        public string TeamName { get; internal set; }
        public int RaceNumber { get; internal set; }
        public CupCategory CupCategory { get; internal set; }
        public int CurrentDriverIndex { get; internal set; }
        public List<DriverData> Drivers { get; internal set; } = new List<DriverData>();
        public NationalityEnum TeamNationality { get; internal set; }

        // RealtimeCarUpdates
        public RealtimeCarUpdate NewData { get; private set; } = null;
        public RealtimeCarUpdate OldData { get; private set; } = null;

        public DriverData CurrentDriver => Drivers[NewData?.DriverIndex ?? CurrentDriverIndex];

        // ..BySplinePosition
        public float TotalSplinePosition { get; private set; } = 0.0f;
        public int LapsBySplinePosition { get; private set; } = 0;

        // Distances
        public float OnTrackDistanceToFocused { get; private set; } = float.NaN;
        public float DistanceToLeader { get; private set; } = float.NaN;
        public float DistanceToClassLeader { get; private set; } = float.NaN;
        public float TotalDistanceToFocused { get; private set; } = float.NaN;

        // Gaps
        public double GapToLeader { get; private set; } = double.NaN;
        public double GapToClassLeader { get; private set; } = double.NaN;
        public double GapToFocusedTotal { get; private set; } = double.NaN;
        public double GapToFocusedOnTrack { get; private set; } = double.NaN;
        public double GapToAheadInClass { get; internal set; } = double.NaN;
        public double GapToAhead { get; private set; } = double.NaN;

        // Positions
        public int InClassPos { get; private set; } = -1;
        public int OverallPos { get; private set; } = -1;
        public int StartPos { get; private set; } = -1;
        public int StartPosInClass { get; private set; } = -1;

        // Pit info
        public int PitCount { get; private set; } = 0;
        public double PitEntryTime { get; private set; } = double.NaN;
        public double TotalPitTime { get; private set; } = 0;
        public double LastPitTime { get; private set; } = 0;
        public double CurrentTimeInPits { get; private set; } = double.NaN;

        // Stint info
        public double LastStintTime { get; private set; } = double.NaN;
        public double CurrentStintTime { get; private set; } = double.NaN;
        public int LastStintLaps { get; private set; } = 0;
        public int CurrentStintLaps { get; private set; } = 0;
        public double CurrentDriverTotalDrivingTime => CurrentDriver.GetTotalDrivingTime(true, CurrentStintTime);

        // Else
        public bool IsFinished { get; private set; } = false;
        public TimeSpan? FinishTime { get; private set; } = null;
        public int?[] BestLapSectors { get; private set; } = new int?[] { null, null, null };

        internal int MissedRealtimeUpdates { get; set; } = 0;

        private double? _stintStartTime = null;
        private CarClassArray<double> _splinePositionTime = new CarClassArray<double>(-1);
        private bool _isLapFinished = false;
        private bool _isRaceFinishPosSet = false;
        private bool _isFirstUpdate = true;
        ////////////////////////

        public CarData(CarInfo info, RealtimeCarUpdate update) {
            CarIndex = info.CarIndex;
            CarModelType = info.CarModelType;
            CarClass = info.CarClass;
            TeamName = info.TeamName;
            RaceNumber = info.RaceNumber;
            CupCategory = info.CupCategory;
            CurrentDriverIndex = info.CurrentDriverIndex;
            foreach (var d in info.Drivers) { 
                AddDriver(d);
            }
            TeamNationality = info.Nationality;

            NewData = update;
        }

        private void AddDriver(DriverInfo driverInfo) {
            Drivers.Add(new DriverData(driverInfo));
        }

        public double GetDriverTotalDrivingTime(int i) {
            return Drivers[i].GetTotalDrivingTime(i == NewData.DriverIndex, CurrentStintTime);
        }

        #region Entry list update

        /// <summary>
        /// Updates this cars static info. Should be called when new entry list update for this car is recieved.
        /// </summary>
        /// <param name="info"></param>
        public void UpdateCarInfo(CarInfo info) {
            // Only thing that can change is drivers
            // We need to make sure that the order is as specified by new info
            // But also add new drivers. We keep old drivers but move them to the end of list
            // as they might rejoin and then we need to have the old data. (I'm not sure if ACC keeps those drivers or not, but we make sure to keep the data.)
            LeaderboardPlugin.LogInfo($"CarInfo update for car #{RaceNumber}.");
            CurrentDriverIndex = info.CurrentDriverIndex;
            if (Drivers.Count == info.Drivers.Count && Drivers.Zip(info.Drivers, (a, b) => a.Equals(b)).All(x => x)) {
                LeaderboardPlugin.LogInfo($"All drivers same.");
                return;
            } // All drivers are same

            var currentDrivers = "";
            foreach (var d in Drivers) {
                currentDrivers += $"{d.FirstName} {d.LastName};";
            }

            for (int i = 0; i < info.Drivers.Count; i++) {
                LeaderboardPlugin.LogInfo($"Current drivers are: {currentDrivers}");

                var currentDriver = Drivers[i];
                var newDriver = info.Drivers[i];
                LeaderboardPlugin.LogInfo($"Comparing drivers #{i}: current:{currentDriver.FirstName} {currentDriver.LastName}, new:{newDriver.FirstName} {newDriver.LastName}");

                if (currentDriver.Equals(newDriver)) {
                    LeaderboardPlugin.LogInfo($"Result: Drivers are same.");
                    continue; // this driver is same
                }

                var oldIdx = Drivers.FindIndex(x => x.Equals(newDriver));
                if (oldIdx == -1) {
                    // Must be new driver
                    LeaderboardPlugin.LogInfo($"Found new driver. Inserting.");
                    Drivers.Insert(i, new DriverData(newDriver));
                } else {
                    // Driver is present but it's order has changed
                    LeaderboardPlugin.LogInfo($"Drivers are reordered.");

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

        /// <summary>
        /// Updates this cars data. Should be called when RealtimeCarUpdate for this car is recieved.
        /// </summary>
        /// <param name="update"></param>
        /// <param name="realtimeData"></param>
        public void OnRealtimeCarUpdate(RealtimeCarUpdate update, RealtimeData realtimeData) {
            // If the race is finished we don't care about any of the realtime updates anymore.
            // We have set finished positions in ´OnRealtimeUpdate´ and that's really all that matters
            if (IsFinished) return;

            OldData = NewData;
            NewData = update;

            // Wait for one more update at the beginning of session, so we have all relevant data for calculations below
            if (OldData == null) return;

            if (realtimeData.IsRace) {
                if (!AddInitialLaps(realtimeData)) return; // If we didn't succeed, we don't want to continue with calculation but just wait for another update.
                UpdateLapsBySplinePosition(realtimeData);
                TotalSplinePosition = NewData.SplinePosition + LapsBySplinePosition;
                UpdatePitInfo(realtimeData);
            }

            if (NewData.Laps != OldData.Laps) {
                CurrentDriver.OnLapFinished(NewData.LastLap);
            }

            UpdateStintInfo(realtimeData);
            UpdateBestLapSectors();
        }

        private void UpdateLapsBySplinePosition(RealtimeData realtimeData) {
            // RealtimeCarUpdate.Laps and SplinePosition updates are not always in sync.
            // This results in some weirdness on lap finish. Count laps myself based on spline position.
            if (OldData.SplinePosition > 0.9
                && NewData.SplinePosition < 0.1
                && OldData.CarLocation == NewData.CarLocation // Removes false laps when using RTG
            ) {
                LapsBySplinePosition++;
            }

            // On certain tracks (Spa) first half of the grid is ahead of the start/finish line,
            // need to add the line crossing lap, otherwise they will be shown lap behind
            if (realtimeData.Phase == SessionPhase.PreFormation
                && NewData.SplinePosition < 0.1
                && LapsBySplinePosition == 0
            ) {
                LapsBySplinePosition++;
            }
        }


        /// <summary>
        /// Add inital laps to the car. It's important when starting SimHub mid session.
        /// </summary>
        /// <returns>
        /// <c>true</c> if succeeded in adding the laps, <c>false</c> otherwise.
        /// </returns>
        /// <param name="realtimeData"></param>
        /// <returns></returns>
        private bool AddInitialLaps(RealtimeData realtimeData) {
            // If we start SimHub in the middle of session and cars are on the different laps, the car behind will gain a lap over
            // For example: P1 has just crossed the line and has completed 3 laps, P2 has 2 laps
            // But LapsBySplinePosition is 0 for both, if now P2 crosses the line,
            // it's LapsBySplinePosition is increased and it would be shown lap ahead of tha actual leader
            // Thus we add current laps to the LapsBySplinePosition
            if (_isFirstUpdate && realtimeData.IsSession) {
                if (Values.TrackData == null || NewData.SplinePosition == 1 || NewData.SplinePosition == 0
                    || (Values.TrackData.TrackId == TrackType.Silverstone && 0.9789979 < NewData.SplinePosition && NewData.SplinePosition < 0.9791052) // Silverstone
                    || (Values.TrackData.TrackId == TrackType.Spa && 0.9961125 < NewData.SplinePosition && NewData.SplinePosition < 0.9962250) // Spa
                ) {
                    LeaderboardPlugin.LogInfo($"Ignored car #{RaceNumber} at start up.");
                    // This is critical point when the lap changes, we don't know yet if it's the old lap or new
                    // Wait for the next update where we know that laps counter has been increased
                    return false;
                } else {
                    if (_isFirstUpdate && LapsBySplinePosition == 0) {
                        LapsBySplinePosition = NewData.Laps;

                        if ((Values.TrackData.TrackId == TrackType.Silverstone && NewData.SplinePosition >= 0.9791052)
                            || (Values.TrackData.TrackId == TrackType.Spa && NewData.SplinePosition >= 0.9962250)
                        ) {
                            // This is the position of finish line, position where lap count is increased.
                            // This means that in above we added one extra lap as by SplinePosition it's not new lap yet.
                            LapsBySplinePosition -= 1;
                            //LeaderboardPlugin.LogInfo($"Remove lap from #{Info.RaceNumber}");
                        }

                        LeaderboardPlugin.LogInfo($"Set initial laps of #{RaceNumber} to {LapsBySplinePosition}");
                    }
                    _isFirstUpdate = false;
                }
            }
            _isFirstUpdate = false;
            return true;
        }

        private void UpdatePitInfo(RealtimeData realtimeData) {
            if (OldData.CarLocation != CarLocationEnum.Pitlane && NewData.CarLocation == CarLocationEnum.Pitlane
                || (double.IsNaN(PitEntryTime) && NewData.CarLocation == CarLocationEnum.Pitlane && (realtimeData.IsSession || realtimeData.IsPostSession)) // We join/start simhub mid session
                ) {
                // Entered pitlane
                PitCount++;
                PitEntryTime = realtimeData.SessionTime.TotalSeconds;
                LeaderboardPlugin.LogInfo($"#{RaceNumber} entered pitlane at {PitEntryTime}.");
            }

            if (!double.IsNaN(PitEntryTime) && NewData.CarLocation != CarLocationEnum.Pitlane) {
                // Left the pitlane
                LastPitTime = (realtimeData.SessionTime).TotalSeconds - PitEntryTime;
                TotalPitTime += LastPitTime;
                PitEntryTime = double.NaN;
                CurrentTimeInPits = double.NaN;
                LeaderboardPlugin.LogInfo($"#{RaceNumber} exited pitlane. Time in pits (Total,Last) = ({TotalPitTime:00.0}s,{LastPitTime:00.0}s)");
            }

            if (!double.IsNaN(PitEntryTime)) { 
                CurrentTimeInPits = realtimeData.SessionTime.TotalSeconds - PitEntryTime;
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
                || (_stintStartTime == null && NewData.CarLocation == CarLocationEnum.Track && (realtimeData.IsSession || realtimeData.IsPostSession)) // We join/start simhub mid session
            ) {
                _stintStartTime = realtimeData.SessionTime.TotalSeconds;
                LeaderboardPlugin.LogInfo($"#{RaceNumber} started stint at {_stintStartTime}");
            }

            // Stint ended
            if (OldData.CarLocation != CarLocationEnum.Pitlane && NewData.CarLocation == CarLocationEnum.Pitlane) { // Pitlane entry


                if (_stintStartTime != null) {
                    LastStintTime = realtimeData.SessionTime.TotalSeconds - (double)_stintStartTime;
                    CurrentDriver.OnStintEnd(LastStintTime);
                    _stintStartTime = null;
                    CurrentStintTime = double.NaN;
                }
                LastStintLaps = CurrentStintLaps;
                CurrentStintLaps = 0;

                LeaderboardPlugin.LogInfo($"#{RaceNumber} stint ended: {LastStintLaps} laps in {LastStintTime/60.0:00.0}min");

            }

            if (_stintStartTime != null) {
                CurrentStintTime = realtimeData.SessionTime.TotalSeconds - (double)_stintStartTime;
            }
        }

        private void UpdateBestLapSectors() {
            // Note that NewData.BestSessionLap doesn't contain the sectors of that best lap but the best sectors.
            if (OldData.Laps != NewData.Laps
                && NewData.LastLap.IsValidForBest
                && NewData.LastLap.LaptimeMS == NewData.BestSessionLap.LaptimeMS
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
            int overallPos, 
            int classPos, 
            float relSplinePos
        ) {
            if (IsFinished && _isRaceFinishPosSet) return;
            InClassPos = classPos;
            _splinePositionTime.Reset();

            OverallPos = overallPos;
            if (realtimeData.OldData.Phase == SessionPhase.SessionOver && realtimeData.IsRace) {
                if (CarIndex == leaderCar.CarIndex || leaderCar.IsFinished) {
                    if (NewData.Laps != OldData.Laps) {
                        IsFinished = true;
                        FinishTime = realtimeData.SessionTime;
                        LeaderboardPlugin.LogInfo($"Car #{RaceNumber} finished at {FinishTime}");
                    }
                }
            }

            // After finish we want to freeze total distances and gaps, but keep updating relatives
            if (!leaderCar.IsFinished) {
                DistanceToLeader = (leaderCar.TotalSplinePosition - TotalSplinePosition) * Values.TrackData.TrackMeters;
                DistanceToClassLeader = (classLeaderCar.TotalSplinePosition - TotalSplinePosition) * Values.TrackData.TrackMeters;
                TotalDistanceToFocused = (focusedCar.TotalSplinePosition - TotalSplinePosition) * Values.TrackData.TrackMeters;
            }

            OnTrackDistanceToFocused = relSplinePos * Values.TrackData.TrackMeters;
            SetGaps(realtimeData, leaderCar, classLeaderCar, focusedCar, carAhead, carAheadInClass);

            if (IsFinished) _isRaceFinishPosSet = true;
        }

        #endregion

        #region Gap calculations

        private void SetGaps(RealtimeData realtimeData, CarData leader, CarData classLeader, CarData focused, CarData carAhead, CarData carAheadInClass) {
            if (realtimeData.IsRace) {
                // Use time gaps on track
                // We update the gap only if CalculateGap returns a proper value because we don't want to update the gap if one of the cars has finished. 
                // That would result in wrong gaps. We keep the gaps at the last valid value and update once both cars have finished.

                var gap = CalculateGap(this, leader);
                if (!double.IsNaN(gap)) GapToLeader = gap;

                gap = CalculateGap(this, classLeader);
                if (!double.IsNaN(gap)) GapToClassLeader = gap;

                gap = CalculateGap(focused, this);
                if (!double.IsNaN(gap)) GapToFocusedTotal = gap;

                if (carAhead != null) {
                    gap = CalculateGap(this, carAhead);
                    if (!double.IsNaN(gap)) GapToAhead = gap;
                } else {
                    gap = double.NaN;
                }

                if (carAheadInClass != null) {
                    gap = CalculateGap(this, carAheadInClass);
                    if (!double.IsNaN(gap)) GapToAheadInClass = gap;
                } else {
                    gap = double.NaN;
                }

            } else {
                // Use best laps to calculate gaps
                var thisBestLap = NewData?.BestSessionLap?.LaptimeMS;
                if (thisBestLap == null) {
                    GapToLeader = double.NaN;
                    GapToAheadInClass = double.NaN;
                    GapToClassLeader = double.NaN;
                    GapToAhead = double.NaN;
                    return;
                }

                var leaderBestLap = leader?.NewData?.BestSessionLap?.LaptimeMS;
                GapToLeader = leaderBestLap != null ? ((double)thisBestLap - (double)leaderBestLap) / 1000.0 : double.NaN;

                var classLeaderBestLap = classLeader?.NewData?.BestSessionLap?.LaptimeMS;
                GapToClassLeader = classLeaderBestLap != null ? ((double)thisBestLap - (double)classLeaderBestLap) / 1000.0 : double.NaN;

                var aheadBestLap = carAhead?.NewData?.BestSessionLap?.LaptimeMS;
                GapToAhead = aheadBestLap != null ? ((double)thisBestLap - (double)aheadBestLap) / 1000.0 : double.NaN;

                var aheadInClassBestLap = carAheadInClass?.NewData?.BestSessionLap?.LaptimeMS;
                GapToAheadInClass = aheadInClassBestLap != null ? ((double)thisBestLap - (double)aheadInClassBestLap) / 1000.0 : double.NaN;
            }

            CalculateRelativeGapToFocused(focused);
        }


        private void CalculateRelativeGapToFocused(CarData focusedCar) {
            var focusedPos = focusedCar.NewData?.SplinePosition;
            var thisPos = NewData?.SplinePosition;
            if (focusedPos == null || thisPos == null) { 
                GapToFocusedOnTrack = double.NaN;
                return;
            };

            if (Values.TrackData == null 
                || (TrackData.LapInterpolators[focusedCar.CarClass] == null && TrackData.LapInterpolators[CarClass] == null)
            ) {
                GapToFocusedOnTrack = OnTrackDistanceToFocused / (175.0 / 3.6);
                return;
            }

            var relativeSplinePos = CalculateRelativeSplinePosition((float)thisPos, (float)focusedPos);
            
            double gap;
            if (relativeSplinePos > 0) {
                // This car is ahead of focused, gap should be the time it takes focused car to reach this car's position
                // That is use focusedCar lap data to calculate the gap
                var cls = TrackData.LapInterpolators[focusedCar.CarClass] != null ? focusedCar.CarClass : CarClass;
                gap = CalculateGapBetweenPos((float)focusedPos, (float)thisPos, focusedCar.GetSplinePosTime(cls), GetSplinePosTime(cls), TrackData.LapInterpolators[cls].LapTime);
            } else {
                // This car is behind of focused, gap should be the time it takes us to reach focused car
                // That is use this cars lap data to calculate gap
                var cls = TrackData.LapInterpolators[CarClass] != null ? CarClass : focusedCar.CarClass; // If this car's best lap is not available, use focused car's
                gap = -CalculateGapBetweenPos((float)thisPos, (float)focusedPos, GetSplinePosTime(cls), focusedCar.GetSplinePosTime(cls), TrackData.LapInterpolators[cls].LapTime);
            }
            if (!double.IsNaN(gap)) GapToFocusedOnTrack = gap;
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
        public static double CalculateGap(CarData from, CarData to) {
            if (from.CarIndex == to.CarIndex)  return 0;

            var distBetween = to.TotalSplinePosition - from.TotalSplinePosition; // Negative if 'To' is behind

            if (distBetween <= -1) {
                // 'To' is more than a lap behind of 'from'
                return -Math.Floor(Math.Abs(distBetween)) + 100_000;
            } else if (Values.TrackData != null && distBetween >= 1) {
                // 'To' is more than a lap ahead of 'from'
                return Math.Floor(Math.Abs(distBetween)) + 100_000;
            } else {
                // If both cars are finished their position on track doesn't matter anymore
                // Gap between then is the gap on the finish line
                if (from.IsFinished && to.IsFinished) {
                    return ((TimeSpan)from.FinishTime).TotalSeconds - ((TimeSpan)to.FinishTime).TotalSeconds;
                } else if (from.IsFinished || to.IsFinished) {
                    return double.NaN;
                }

                // We don't have lap interpolators available, use naive method to calculate the gap
                if (Values.TrackData == null 
                    || (TrackData.LapInterpolators[to.CarClass] == null && TrackData.LapInterpolators[from.CarClass] == null) 
                ) {
                    //LeaderboardPlugin.LogInfo("Used naive gap calculator");
                    return distBetween * Values.TrackData.TrackMeters / (175.0 / 3.6);
                }

                var fromPos = from.NewData?.SplinePosition;
                var toPos = to.NewData?.SplinePosition;
                if (fromPos == null || toPos == null) return double.NaN;

                double gap = double.NaN;
                if (distBetween > 0) {
                    // To car is ahead of from, gap should be the time it takes 'from' car to reach 'to' car's position
                    // That is use 'from' lap data to calculate the gap
                    var cls = TrackData.LapInterpolators[from.CarClass] != null ? from.CarClass : to.CarClass;
                    gap = CalculateGapBetweenPos((float)fromPos, (float)toPos, from.GetSplinePosTime(cls), to.GetSplinePosTime(cls), TrackData.LapInterpolators[cls].LapTime);
                } else {
                    // 'to' car is behind of 'from', gap should be the time it takes 'to' to reach 'from'
                    // That is use 'to' cars lap data to calculate the gap
                    var cls = TrackData.LapInterpolators[to.CarClass] != null ? to.CarClass : from.CarClass;
                    gap = -CalculateGapBetweenPos((float)toPos, (float)fromPos, to.GetSplinePosTime(cls), from.GetSplinePosTime(cls), TrackData.LapInterpolators[cls].LapTime);
                }
                return gap;
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
                // Ahead is on another lap, gap is time for behindpos to end lap, and then reach aheadpos
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
        public float CalculateRelativeSplinePosition(CarData otherCar) {
            if (NewData == null || otherCar.NewData == null) return float.NaN;
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
        public static float CalculateRelativeSplinePosition(float thisPos, float posRelativeTo) {
            var relSplinePos = thisPos - posRelativeTo;
            if (relSplinePos > 0.5) { // Pos is more than half a lap ahead, so technically it's closer from behind. Take one lap away to show it behind us.
                relSplinePos -= 1;
            } else if (relSplinePos < -0.5) { // Pos is more than half a lap behind, so it's in front. Add one lap to show it in front of us.
                relSplinePos += 1;
            }
            return relSplinePos;
        }

        #endregion

        public override string ToString() {
            var pos = NewData?.Position ?? -1;
            float splinepos = NewData?.SplinePosition ?? -1;
            float speed = NewData?.Kmh ?? 200;
            return $"CarId {CarIndex:000} #{RaceNumber,-4}, {Drivers[0].InitialPlusLastName(),-20} P{pos:00}/{InClassPos:00} L{NewData?.Laps ?? -1 :00}: SplinePos:{splinepos:0.000} LapsBySplinePos:{LapsBySplinePosition:00} TotalSplinePos:{TotalSplinePosition:0.000} Gaps_ToLeader:{GapToLeader:000.0}| ToClassLeader:{GapToClassLeader:000.0}| ToFocusedOnTrack:{GapToFocusedOnTrack:000.0}| ToFocusedTotal:{GapToFocusedTotal:000.0}, Distances_ToLeader:{DistanceToLeader:00000.0}| ToClassLeader:{DistanceToClassLeader:00000.0}| ToFocusedOnTrack:{OnTrackDistanceToFocused:00000.0}| ToFocusedTotal:{TotalDistanceToFocused:00000.0}";
        }



    }
}
