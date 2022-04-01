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
        public ushort CarIndex { get; }
        public CarType CarModelType { get; internal set; }
        public CarClass CarClass { get; internal set; }
        public string TeamName { get; internal set; }
        public int RaceNumber { get; internal set; }
        public CupCategory CupCategory { get; internal set; }
        public int CurrentDriverIndex { get; internal set; }
        public List<DriverData> Drivers { get; internal set; } = new List<DriverData>();
        public NationalityEnum TeamNationality { get; internal set; }

        internal void AddDriver(DriverInfo driverInfo) {
            Drivers.Add(new DriverData(driverInfo));
        }

        public RealtimeCarUpdate NewData { get; private set; }
        public RealtimeCarUpdate OldData { get; private set; }

        public float TotalSplinePosition { get; private set; }
        public float OnTrackDistanceToFocused { get; private set; }
        public float DistanceToLeader { get; private set; }
        public float DistanceToClassLeader { get; private set; }
        public float TotalDistanceToFocused { get; private set; }
        public int LapsBySplinePosition { get; private set; }

        public double GapToLeader { get; private set; }
        public double GapToClassLeader { get; private set; }
        public double GapToFocusedTotal { get; private set; }
        public double GapToFocusedOnTrack { get; private set; }
        public double? GapToAheadInClass { get; internal set; }
        public double? GapToAhead { get; private set; }

        public int InClassPos { get; private set; }
        public int OverallPos { get; private set; }
        public int MissedRealtimeUpdates { get; internal set; }
        public bool FirstAdded = false;
        public bool IsFinished = false;
        public TimeSpan? FinishTime = null;

        public int StartPos = -1;
        public int StartPosInClass = -1;

        public int?[] BestLapSectors = new int?[] { null, null, null };

        public int PitCount = 0;
        public TimeSpan? PitEntryTime = null;
        public double TotalPitTime = 0;
        public double LastPitTime = 0;

        public double LastStintTime = double.NaN;
        public double CurrentStintTime = double.NaN;
        private double? _stintStartTime = null;

        public int LastStintLaps = 0;
        public int CurrentStintLaps = 0;

        private double? _lapTime = null;
        private LinearSpline _lapInterp;
        private double? _splinePosTime = null;
        private bool _isLapFinished = false;
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

            if (Values.TrackData != null && TrackData.LapTime.ContainsKey(CarClass)) {
                _lapTime = TrackData.LapTime[CarClass];
                _lapInterp = TrackData.LapInterpolators[CarClass];
            }

            OldData = null;
            NewData = update;
            OnTrackDistanceToFocused = float.NaN;
            DistanceToLeader = float.NaN;
            DistanceToClassLeader = float.NaN;
            LapsBySplinePosition = 0;
            InClassPos = -1;
            GapToClassLeader = double.NaN;
            GapToLeader = double.NaN;
            GapToFocusedTotal = double.NaN;
            GapToFocusedOnTrack = double.NaN;
            GapToAhead = null;
            GapToAheadInClass = null;
        }

        public void UpdateCarInfo(CarInfo info) {
            // Only thing that can change is drivers
            // We need to make sure that the order is as specified by new info
            // But also add new drivers. We keep old drivers but move them to the end of list
            // as they might rejoin and then we need to have the old data. (I'm not sure if ACC keeps those drivers or not, but we make sure to keep the data.)

            CurrentDriverIndex = info.CurrentDriverIndex;
            if (Drivers.Count == info.Drivers.Count && Drivers.Zip(info.Drivers, (a, b) => a.Equals(b)).All(x => x)) return; // All drivers are same

            for (int i = 0; i < info.Drivers.Count; i++) {
                if (Drivers[i].Equals(info.Drivers[i])) continue; // this driver is same

                var oldIdx = Drivers.FindIndex(x => x.Equals(info.Drivers[i]));
                if (oldIdx == -1) {
                    // Must be new driver
                    Drivers.Insert(i, new DriverData(info.Drivers[i]));
                } else {
                    // Driver is present but it's order has changed
                    var old = Drivers[oldIdx];
                    Drivers.RemoveAt(oldIdx);
                    Drivers.Insert(i, old);
                }
            }

        }

        private bool _isFirstUpdate = true;
        private void AddInitialLaps(RealtimeCarUpdate update) {
            if (_isFirstUpdate && LapsBySplinePosition == 0) {
                LapsBySplinePosition = update.Laps;

                if ((Values.TrackData.TrackId == TrackType.Silverstone && update.SplinePosition >= 0.9791052)
                    || (Values.TrackData.TrackId == TrackType.Spa && update.SplinePosition >= 0.9962250)
                ) {
                    // This is the position of finish line, position where lap count is increased.
                    // This means that in above we added one extra lap as by SplinePosition it's not new lap yet.
                    LapsBySplinePosition -= 1;
                    //LeaderboardPlugin.LogInfo($"Remove lap from #{Info.RaceNumber}");
                }
            }
            _isFirstUpdate = false;
        }

        public void SetStartingPositions(int overall, int inclass) { 
            StartPos = overall;
            StartPosInClass = inclass;
        }


        public void OnRealtimeCarUpdate(RealtimeCarUpdate update, RealtimeData realtimeData) {
            if (IsFinished) return;

            OldData = NewData;
            NewData = update;

            if (_lapTime == null && Values.TrackData != null && TrackData.LapTime.ContainsKey(CarClass)) {
                _lapTime = TrackData.LapTime[CarClass];
                _lapInterp = TrackData.LapInterpolators[CarClass];
            }

            if (OldData == null) return;

            if (_lapTime != null) {
                _splinePosTime = _lapInterp.Interpolate(NewData.SplinePosition);
            }

            _isLapFinished = OldData.Laps != NewData.Laps;

            if (realtimeData.IsRace) {

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
                        //LeaderboardPlugin.LogInfo($"Ignored car #{Info.RaceNumber}");
                        // This is critical point when the lap changes, we don't know yet if it's the old lap or new
                        // Wait for the next update where we know that laps counter has been increased
                        return;
                    } else {
                        AddInitialLaps(NewData);
                    }
                }
                _isFirstUpdate = false;
                // RealtimeCarUpdate.Laps and SplinePosition updates are not always in sync.
                // This results in some weirdness on lap finish. Count laps myself based on spline position.
                if (OldData.SplinePosition > 0.9 
                    && NewData.SplinePosition < 0.1 
                    && OldData.CarLocation == NewData.CarLocation
                ) {
                    LapsBySplinePosition++;
                }

                // On certain tracks (Spa) first half of the grid is ahead of the start/finish line,
                // need to add the line crossing lap, otherwise they will be shown lap behind
                if ((realtimeData.Phase == SessionPhase.PreFormation) && NewData.SplinePosition < 0.1 && LapsBySplinePosition == 0) {
                    LapsBySplinePosition++;
                }

                TotalSplinePosition = NewData.SplinePosition + LapsBySplinePosition;

                if (OldData.CarLocation != CarLocationEnum.Pitlane && NewData.CarLocation == CarLocationEnum.Pitlane) { // Entered pitlane
                    PitCount++;
                    PitEntryTime = realtimeData.SessionTime;
                }

                if (PitEntryTime != null && NewData.CarLocation != CarLocationEnum.Pitlane) { // Left the pitlane
                    LastPitTime = (realtimeData.SessionTime).TotalSeconds - ((TimeSpan)PitEntryTime).TotalSeconds;
                    TotalPitTime += LastPitTime;
                    PitEntryTime = null;
                }
            }

            // Lap finished
            if (NewData.Laps != OldData.Laps) {
                GetCurrentDriver().OnLapFinished(update.LastLap);
                CurrentStintLaps++;
            }

            // Stint started
            if ((OldData.CarLocation == CarLocationEnum.Pitlane && NewData.CarLocation != CarLocationEnum.Pitlane) // Pitlane exit
                || (realtimeData.IsRace && realtimeData.IsSessionStart) // Race start
            ) {
                _stintStartTime = realtimeData.SessionTime.TotalSeconds;

            }

            // Stint ended
            if (OldData.CarLocation != CarLocationEnum.Pitlane && NewData.CarLocation == CarLocationEnum.Pitlane) { // Pitlane entry
                LeaderboardPlugin.LogInfo($"Stint ended: #{RaceNumber} with {GetCurrentDriver().InitialPlusLastName()}");

                if (_stintStartTime != null) {
                    LastStintTime = realtimeData.SessionTime.TotalSeconds - (double)_stintStartTime;
                    GetCurrentDriver().OnStintEnd(LastStintTime);
                    _stintStartTime = null;
                    CurrentStintTime = double.NaN;
                }
                LastStintLaps = CurrentStintLaps;
                CurrentStintLaps = 0;
                                
            }

            if (_stintStartTime != null) {
                CurrentStintTime = realtimeData.SessionTime.TotalSeconds - (double)_stintStartTime;
            }


            // Update best sectors.
            if (OldData.Laps != NewData.Laps && NewData.LastLap.IsValidForBest && NewData.LastLap.LaptimeMS == NewData.BestSessionLap.LaptimeMS) {
                for (int i = 0; i < 3; i++) {
                    BestLapSectors[i] = update.LastLap.Splits[i];
                }
            }

            NewData = update;
        }

        public double GetDriverTotalDrivingTime(int i) {
            return Drivers[i].GetTotalDrivingTime(i == NewData.DriverIndex, CurrentStintTime);
        }

        public double GetCurrentDriverTotalDrivingTime() {
            return GetCurrentDriver().GetTotalDrivingTime(true, CurrentStintTime);
        }

        public double? GetCurrentTimeInPits(TimeSpan sessionTime) {
            if (PitEntryTime == null) return null;
            return sessionTime.TotalSeconds - ((TimeSpan)PitEntryTime).TotalSeconds;
        }

        private bool _isRaceFinishPosSet = false;
        public void OnRealtimeUpdate(
            RealtimeData realtimeData, 
            CarData leaderCar, 
            CarData classLeaderCar, 
            CarData focusedCar, 
            CarData carAhead, 
            CarData carBehind, 
            int overallPos, 
            int classPos, 
            float relSplinePos
        ) {
            if (IsFinished && _isRaceFinishPosSet) return;

            if (classPos != 0) {
                InClassPos = classPos;
            };

            OverallPos = overallPos;
            
            if (realtimeData.Phase == SessionPhase.SessionOver) {
                if (CarIndex == leaderCar.CarIndex || leaderCar.IsFinished) {
                    if (_isLapFinished) {
                        IsFinished = true;
                        FinishTime = realtimeData.SessionTime;
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
            SetGaps(realtimeData, leaderCar, classLeaderCar, focusedCar, carAhead, carBehind);

            if (IsFinished) _isRaceFinishPosSet = true;
        }


        private void SetGaps(RealtimeData realtimeData, CarData leader, CarData classLeader, CarData focused, CarData carAhead, CarData carAheadInClass) {
            if (realtimeData.IsRace) {
                var gap = CalculateGap(this, leader);
                if (!double.IsNaN(gap)) GapToLeader = gap;

                gap = CalculateGap(this, classLeader);
                if (!double.IsNaN(gap)) GapToClassLeader = gap;

                gap = CalculateGap(focused, this);
                if (!double.IsNaN(gap)) GapToFocusedTotal = gap;

                if (carAhead != null) {
                    gap = CalculateGap(this, carAhead);
                    if (!double.IsNaN(gap)) GapToAhead = gap;
                }

                if (carAheadInClass != null) {
                    gap = CalculateGap(this, carAheadInClass);
                    if (!double.IsNaN(gap)) GapToAheadInClass = gap;
                }

            } else {
                var thisBestLap = NewData?.BestSessionLap?.LaptimeMS;

                var leaderBestLap = leader.NewData?.BestSessionLap?.LaptimeMS;
                if (thisBestLap != null && leaderBestLap != null) {
                    GapToLeader = ((double)thisBestLap - (double)leaderBestLap) / 1000.0;
                } else {
                    GapToLeader = double.NaN;
                }

                var classLeaderBestLap = classLeader.NewData?.BestSessionLap?.LaptimeMS;
                if (thisBestLap != null && classLeaderBestLap != null) {
                    GapToClassLeader = ((double)thisBestLap - (double)classLeaderBestLap) / 1000.0;
                } else {
                    GapToClassLeader = double.NaN;
                }

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

            if (Values.TrackData == null || (focusedCar._lapTime == null && _lapTime == null)) {
                GapToFocusedOnTrack = OnTrackDistanceToFocused / (175.0 / 3.6);
                return;
            }

            var relativeSplinePos = CalculateRelativeSplinePosition((float)thisPos, (float)focusedPos);

            var focusedTimePos = focusedCar._splinePosTime;
            var thisTimePos = focusedCar._splinePosTime;
            if (focusedTimePos == null || thisTimePos == null) {
                GapToFocusedOnTrack = double.NaN;
                return;
            }

            double gap;
            if (relativeSplinePos > 0) {
                // This car is ahead of focused, gap should be the time it takes focused car to reach this car's position
                // That is use focusedCar lap data to calculate the gap

                if (!TrackData.LapInterpolators.ContainsKey(focusedCar.CarClass)) { // If focused car's best lap is not available, use this car's
                    gap = CalculateGapBetweenPos((double)focusedTimePos, (double)thisTimePos, (double)_lapTime);
                } else {
                    gap = CalculateGapBetweenPos((double)focusedTimePos, (double)thisTimePos, (double)focusedCar._lapTime);
                }
            } else {
                // This car is behind of focused, gap should be the time it takes us to reach focused car
                // That is use this cars lap data to calculate gap

                if (!TrackData.LapInterpolators.ContainsKey(CarClass)) { // If this car's best lap is not available, use focused car's
                    gap = -CalculateGapBetweenPos((double)thisTimePos, (double)focusedTimePos, (double)focusedCar._lapTime);
                } else {
                    gap = -CalculateGapBetweenPos((double)thisTimePos, (double)focusedTimePos, (double)_lapTime);
                }
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
            if (from.CarIndex == to.CarIndex) {
                return 0;
            }

            var distBetween = to.TotalSplinePosition - from.TotalSplinePosition; // Negative if 'To' is behind

            if (distBetween <= -1) {
                // 'To' is more than a lap behind of 'from'
                return -Math.Floor(Math.Abs(distBetween)) + 100_000;
            } else if (Values.TrackData != null && distBetween >= 1) {
                // 'To' is more than a lap ahead of 'from'
                return Math.Floor(Math.Abs(distBetween)) + 100_000;
            } else {
                if (from.IsFinished && to.IsFinished) {
                    return ((TimeSpan)from.FinishTime).TotalSeconds - ((TimeSpan)to.FinishTime).TotalSeconds;
                }

                if (Values.TrackData == null || (to._lapTime == null && from._lapTime == null) ) {
                    return distBetween * Values.TrackData.TrackMeters / (175.0 / 3.6);
                }

                var fromPos = from._splinePosTime;
                var toPos = to._splinePosTime;
                if (fromPos == null || toPos == null) return double.NaN;

                double gap = double.NaN;
                if (distBetween > 0) {
                    // To car is ahead of from, gap should be the time it takes 'from' car to reach this car's position
                    // That is use 'from' lap data to calculate the gap
                    if (!from.IsFinished) {
                        if (!TrackData.LapInterpolators.ContainsKey(from.CarClass)) { // If from car's best lap is not available, use to car's. One of those must be available
                            gap = CalculateGapBetweenPos((double)fromPos, (double)toPos, (double)to._lapTime);
                        } else {
                            gap = CalculateGapBetweenPos((double)fromPos, (double)toPos, (double)from._lapTime);
                        }
                    }
                } else {
                    // This car is behind of focused, gap should be the time it takes us to reach focused car
                    // That is use this cars lap data to calculate gap
                    if (!to.IsFinished) {
                        if (!TrackData.LapInterpolators.ContainsKey(to.CarClass)) { // If this car's best lap is not available, use focused car's
                            gap = -CalculateGapBetweenPos((double)toPos, (double)fromPos, (double)from._lapTime);
                        } else {
                            gap = -CalculateGapBetweenPos((double)toPos, (double)fromPos, (double)to._lapTime);
                        }
                    }
                }
                return gap;
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
        public static double CalculateGapBetweenPos(double start, double end, double lapTime) {
             if (end < start) {
                // Ahead is on another lap, gap is time for behindpos to end lap, and then reach aheadpos
                return lapTime - start + end;
            } else {
                // We must be on the same lap, gap is time for behindpos to reach aheadpos
                return end - start;
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
        public static double CalculateGapBetweenPos(float? behindPos, float? aheadPos, LinearSpline lapInterp, double lapTime) {
            if (behindPos == null || aheadPos == null) return double.NaN;
            var start = lapInterp.Interpolate((double)behindPos);
            var end = lapInterp.Interpolate((double)aheadPos);

            
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

        public override string ToString() {
            var pos = NewData?.Position ?? -1;
            float splinepos = NewData?.SplinePosition ?? -1;
            float speed = NewData?.Kmh ?? 200;
            return $"CarId {CarIndex:000} #{RaceNumber,-4}, {Drivers[0].InitialPlusLastName(),-20} P{pos:00}/{InClassPos:00} L{NewData?.Laps ?? -1 :00}: SplinePos:{splinepos:0.000} LapsBySplinePos:{LapsBySplinePosition:00} TotalSplinePos:{TotalSplinePosition:0.000} Gaps_ToLeader:{GapToLeader:000.0}| ToClassLeader:{GapToClassLeader:000.0}| ToFocusedOnTrack:{GapToFocusedOnTrack:000.0}| ToFocusedTotal:{GapToFocusedTotal:000.0}, Distances_ToLeader:{DistanceToLeader:00000.0}| ToClassLeader:{DistanceToClassLeader:00000.0}| ToFocusedOnTrack:{OnTrackDistanceToFocused:00000.0}| ToFocusedTotal:{TotalDistanceToFocused:00000.0}";
        }

        public DriverData GetCurrentDriver() {
            return Drivers[NewData?.DriverIndex ?? CurrentDriverIndex];
        }


    }
}
