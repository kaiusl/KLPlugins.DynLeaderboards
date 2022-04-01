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

            if (OldData == null) return;
            
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
        public void OnRealtimeUpdate(RealtimeData realtimeData, CarData leaderCar, CarData classLeaderCar, CarData focusedCar, int overallPos, int classPos, float relSplinePos) {
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
            SetGaps(realtimeData, leaderCar, classLeaderCar, focusedCar);

            if (IsFinished) _isRaceFinishPosSet = true;
        }


        private void SetGaps(RealtimeData realtimeData, CarData leader, CarData classLeader, CarData focused) {
            if (realtimeData.IsRace) {
                CalculateGapsOnTrack(leader, classLeader, focused);
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

                CalculateRelativeGapToFocused(focused);
            }
        }

        private void CalculateGapsOnTrack(CarData leader, CarData classLeader, CarData focused) {
            CalculateGapToLeader(leader);
            CalculateGapToClassLeader(classLeader);
            CalculateGapToFocusedTotal(focused);
            CalculateRelativeGapToFocused(focused);
    }

        private void CalculateGapToLeader(CarData leader) {
            if (OverallPos == 1) {
                GapToLeader = 0;
                return;
            }

            if (DistanceToLeader > Values.TrackData.TrackMeters) {
                GapToLeader = -Math.Floor((DistanceToLeader) / Values.TrackData.TrackMeters);
            } else {
                if (IsFinished) {
                    GapToLeader = ((TimeSpan)FinishTime).TotalSeconds - ((TimeSpan)leader.FinishTime).TotalSeconds;
                    return;
                }


                if (Values.TrackData == null || !TrackData.LapInterpolators.ContainsKey(CarClass)) {
                    GapToLeader = DistanceToLeader / (175.0 / 3.6);
                    return;
                }
                if (!leader.IsFinished) {
                    var leaderPos = leader.NewData?.SplinePosition;
                    var thisPos = NewData?.SplinePosition;
                    var gap = CalculateGapBetweenPos(thisPos, leaderPos, TrackData.LapInterpolators[CarClass]);
                    if (!double.IsNaN(gap)) GapToLeader = gap;
                }
            }
        }

        private void CalculateGapToClassLeader(CarData classLeader) {
            if (InClassPos == 1) { 
                GapToClassLeader = 0;
                return;
            }

            if (DistanceToClassLeader > Values.TrackData.TrackMeters) {
                GapToClassLeader = -Math.Floor((DistanceToClassLeader) / Values.TrackData.TrackMeters);
            } else {
                if (IsFinished) {
                    GapToClassLeader = ((TimeSpan)FinishTime).TotalSeconds - ((TimeSpan)classLeader.FinishTime).TotalSeconds;
                    return;
                }

                if (Values.TrackData == null || !TrackData.LapInterpolators.ContainsKey(CarClass)) {
                    GapToClassLeader = DistanceToClassLeader / (175.0 / 3.6);
                    return;
                }

                if (!classLeader.IsFinished) {
                    var leaderPos = classLeader.NewData?.SplinePosition;
                    var thisPos = NewData?.SplinePosition;
                    var gap = CalculateGapBetweenPos(thisPos, leaderPos, TrackData.LapInterpolators[CarClass]);
                    if (!double.IsNaN(gap)) GapToClassLeader = gap;
                }
            }
        }

        private void CalculateGapToFocusedTotal(CarData focusedCar) {
            if (focusedCar.CarIndex == CarIndex) { 
                GapToFocusedTotal = 0;
                return;
            }

            if (TotalDistanceToFocused > Values.TrackData.TrackMeters) { 
                // This car is more than a lap behind of focused car
                GapToFocusedTotal = -Math.Floor(Math.Abs(TotalDistanceToFocused / Values.TrackData.TrackMeters)) + 10000;
            } else if (TotalDistanceToFocused < -Values.TrackData.TrackMeters) {
                // This car is more than a lap ahead of focused car
                GapToFocusedTotal = Math.Floor(Math.Abs(TotalDistanceToFocused / Values.TrackData.TrackMeters)) + 10000;
            } else {
                if (IsFinished && focusedCar.IsFinished) {
                    GapToFocusedTotal = ((TimeSpan)focusedCar.FinishTime).TotalSeconds - ((TimeSpan)FinishTime).TotalSeconds;
                    return;
                }

                if (Values.TrackData == null || !TrackData.LapInterpolators.ContainsKey(CarClass)) {
                    GapToFocusedTotal = TotalDistanceToFocused / (175.0 / 3.6);
                    return;
                }

                var focusedPos = focusedCar.NewData?.SplinePosition;
                var thisPos = NewData?.SplinePosition;

                double gap = double.NaN;
                if (TotalSplinePosition > focusedCar.TotalSplinePosition) {
                    // This car is ahead of focused, gap should be the time it takes focused car to reach this car's position
                    // That is use focusedCar lap data to calculate the gap
                    if (!IsFinished) {
                        if (!TrackData.LapInterpolators.ContainsKey(focusedCar.CarClass)) { // If focused car's best lap is not available, use this car's
                            gap = CalculateGapBetweenPos(focusedPos, thisPos, TrackData.LapInterpolators[CarClass]);
                        } else {
                            gap = CalculateGapBetweenPos(focusedPos, thisPos, TrackData.LapInterpolators[focusedCar.CarClass]);
                        }
                    }
                } else {
                    // This car is behind of focused, gap should be the time it takes us to reach focused car
                    // That is use this cars lap data to calculate gap
                    if (!focusedCar.IsFinished) {
                        if (!TrackData.LapInterpolators.ContainsKey(CarClass)) { // If this car's best lap is not available, use focused car's
                            gap = -CalculateGapBetweenPos(thisPos, focusedPos, TrackData.LapInterpolators[focusedCar.CarClass]);
                        } else {
                            gap = -CalculateGapBetweenPos(thisPos, focusedPos, TrackData.LapInterpolators[CarClass]);
                        }
                    }
                    
                }
                if (!double.IsNaN(gap)) GapToFocusedTotal = gap;

            }
        }

        private void CalculateRelativeGapToFocused(CarData focusedCar) {
            var focusedPos = focusedCar.NewData?.SplinePosition;
            var thisPos = NewData?.SplinePosition;
            if (focusedPos == null || thisPos == null) { 
                GapToFocusedOnTrack = double.NaN;
                return;
            };

            if (Values.TrackData == null || (!TrackData.LapInterpolators.ContainsKey(CarClass) && !TrackData.LapInterpolators.ContainsKey(focusedCar.CarClass))) {
                GapToFocusedOnTrack = OnTrackDistanceToFocused / (175.0 / 3.6);
                return;
            }

            var relativeSplinePos = CalculateRelativeSplinePosition((float)thisPos, (float)focusedPos);
            double gap;
            if (relativeSplinePos > 0) {
                // This car is ahead of focused, gap should be the time it takes focused car to reach this car's position
                // That is use focusedCar lap data to calculate the gap

                if (!TrackData.LapInterpolators.ContainsKey(focusedCar.CarClass)) { // If focused car's best lap is not available, use this car's
                    gap = CalculateGapBetweenPos(focusedPos, thisPos, TrackData.LapInterpolators[CarClass]);
                } else {
                    gap = CalculateGapBetweenPos(focusedPos, thisPos, TrackData.LapInterpolators[focusedCar.CarClass]);
                }
            } else {
                // This car is behind of focused, gap should be the time it takes us to reach focused car
                // That is use this cars lap data to calculate gap

                if (!TrackData.LapInterpolators.ContainsKey(CarClass)) { // If this car's best lap is not available, use focused car's
                    gap = -CalculateGapBetweenPos(thisPos, focusedPos, TrackData.LapInterpolators[focusedCar.CarClass]);
                } else {
                    gap = -CalculateGapBetweenPos(thisPos, focusedPos, TrackData.LapInterpolators[CarClass]);
                }
            }
            if (!double.IsNaN(gap)) GapToFocusedOnTrack = gap;
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
        public static double CalculateGapBetweenPos(float? behindPos, float? aheadPos, LinearSpline lapInterp) {
            if (behindPos == null || aheadPos == null) return double.NaN;
            var start = lapInterp.Interpolate((double)behindPos);
            var end = lapInterp.Interpolate((double)aheadPos);

            var best = lapInterp.Interpolate(0.99);
            if (aheadPos < behindPos) {
                // Ahead is on another lap, gap is time for behindpos to end lap, and then reach aheadpos
                return best - start + end;
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
