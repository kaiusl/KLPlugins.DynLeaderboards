using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KLPlugins.Leaderboard.Enums;
using MathNet.Numerics.Interpolation;

namespace KLPlugins.Leaderboard.ksBroadcastingNetwork.Structs {
    public class CarData {
        public CarInfo Info { get; set; }
        public RealtimeCarUpdate RealtimeCarUpdate { get; set; }
        public float TotalSplinePosition { get; set; }
        public float OnTrackDistanceToFocused { get; set; }
        public float DistanceToLeader { get; set; }
        public float DistanceToClassLeader { get; set; }
        public float TotalDistanceToFocused { get; set; }
        public int LapsBySplinePosition { get; set; }

        public double GapToLeader { get; set; }
        public double GapToClassLeader { get; set; }
        public double GapToFocusedTotal { get; set; }
        public double GapToFocusedOnTrack { get; set; }

        public int InClassPos { get; set; }
        public int OverallPos { get; set; }
        public int MissedRealtimeUpdates { get; set; }
        public bool FirstAdded = false;
        public bool IsFinished = false;
        public TimeSpan? FinishTime = null;

        public int StartPos = -1;
        public int StartPosInClass = -1;

        public int?[] BestLapSectors = new int?[] { null, null, null };

        private bool _isLapFinished = false;
        ////////////////////////

        public CarData(CarInfo info, RealtimeCarUpdate update) {
            Info = info;
            RealtimeCarUpdate = update;
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


        public void OnRealtimeCarUpdate(RealtimeCarUpdate update, RaceSessionType session, SessionPhase phase) {
            if (IsFinished) return;
            
            if (RealtimeCarUpdate != null) _isLapFinished = update.Laps != RealtimeCarUpdate.Laps;

            if (session == RaceSessionType.Race) {

                // If we start SimHub in the middle of session and cars are on the different laps, the car behind will gain a lap over
                // For example: P1 has just crossed the line and has completed 3 laps, P2 has 2 laps
                // But LapsBySplinePosition is 0 for both, if now P2 crosses the line,
                // it's LapsBySplinePosition is increased and it would be shown lap ahead of tha actual leader
                // Thus we add current laps to the LapsBySplinePosition
                if (_isFirstUpdate && phase == SessionPhase.Session) {
                    if (Values.TrackData == null || update.SplinePosition == 1 || update.SplinePosition == 0
                        || (Values.TrackData.TrackId == TrackType.Silverstone && 0.9789979 < update.SplinePosition && update.SplinePosition < 0.9791052) // Silverstone
                        || (Values.TrackData.TrackId == TrackType.Spa && 0.9961125 < update.SplinePosition && update.SplinePosition < 0.9962250) // Spa
                    ) {
                        //LeaderboardPlugin.LogInfo($"Ignored car #{Info.RaceNumber}");
                        // This is critical point when the lap changes, we don't know yet if it's the old lap or new
                        // Wait for the next update where we know that laps counter has been increased
                        return;
                    } else {
                        AddInitialLaps(update);
                    }
                }

                // RealtimeCarUpdate.Laps and SplinePosition updates are not always in sync.
                // This results in some weirdness on lap finish. Count laps myself based on spline position.
                if (RealtimeCarUpdate != null 
                    && RealtimeCarUpdate.SplinePosition > 0.9 
                    && update.SplinePosition < 0.1 
                    && RealtimeCarUpdate.CarLocation == update.CarLocation
                ) {
                    LapsBySplinePosition++;
                }

                // On certain tracks (Spa) first half of the grid is ahead of the start/finish line,
                // need to add the line crossing lap, otherwise they will be shown lap behind
                if (RealtimeCarUpdate != null && (phase == SessionPhase.PreFormation) && update.SplinePosition < 0.1 && LapsBySplinePosition == 0) {
                    LapsBySplinePosition++;
                }

                TotalSplinePosition = update.SplinePosition + LapsBySplinePosition;
            }

            // Update best sectors.
            if (RealtimeCarUpdate != null && RealtimeCarUpdate.Laps != update.Laps && update.LastLap.IsValidForBest && update.LastLap.LaptimeMS == update.BestSessionLap.LaptimeMS) {
                for (int i = 0; i < 3; i++) {
                    BestLapSectors[i] = update.LastLap.Splits[i];
                }
            }

            RealtimeCarUpdate = update;
        }

        private bool _isRaceFinishPosSet = false;
        public void OnRealtimeUpdate(RealtimeUpdate update, CarData leaderCar, CarData classLeaderCar, CarData focusedCar, int overallPos, int classPos, float relSplinePos) {
            if (IsFinished && _isRaceFinishPosSet) return;

            if (classPos != 0) {
                InClassPos = classPos;
            };

            OverallPos = overallPos;
            
                        
            if (update.Phase == SessionPhase.SessionOver) {
                if (Info.CarIndex == leaderCar.Info.CarIndex || leaderCar.IsFinished) {
                    if (_isLapFinished) {
                        IsFinished = true;
                        FinishTime = update.SessionTime;
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
            SetGaps(update, leaderCar, classLeaderCar, focusedCar);

            if (IsFinished) _isRaceFinishPosSet = true;
        }


        private void SetGaps(RealtimeUpdate update, CarData leader, CarData classLeader, CarData focused) {
            if (update.SessionType == RaceSessionType.Race) {
                CalculateGapsOnTrack(leader, classLeader, focused);
            } else if (update.SessionType != RaceSessionType.Race) {
                var thisBestLap = RealtimeCarUpdate?.BestSessionLap?.LaptimeMS;

                var leaderBestLap = leader.RealtimeCarUpdate?.BestSessionLap?.LaptimeMS;
                if (thisBestLap != null && leaderBestLap != null) {
                    GapToLeader = ((double)thisBestLap - (double)leaderBestLap) / 1000.0;
                } else {
                    GapToLeader = double.NaN;
                }

                var classLeaderBestLap = classLeader.RealtimeCarUpdate?.BestSessionLap?.LaptimeMS;
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


                if (Values.TrackData == null || !TrackData.LapInterpolators.ContainsKey(Info.CarClass)) {
                    GapToLeader = DistanceToLeader / (175.0 / 3.6);
                    return;
                }
                if (!leader.IsFinished) {
                    var leaderPos = leader.RealtimeCarUpdate?.SplinePosition;
                    var thisPos = RealtimeCarUpdate?.SplinePosition;
                    var gap = CalculateGapBetweenPos(thisPos, leaderPos, TrackData.LapInterpolators[Info.CarClass]);
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

                if (Values.TrackData == null || !TrackData.LapInterpolators.ContainsKey(Info.CarClass)) {
                    GapToClassLeader = DistanceToClassLeader / (175.0 / 3.6);
                    return;
                }

                if (!classLeader.IsFinished) {
                    var leaderPos = classLeader.RealtimeCarUpdate?.SplinePosition;
                    var thisPos = RealtimeCarUpdate?.SplinePosition;
                    var gap = CalculateGapBetweenPos(thisPos, leaderPos, TrackData.LapInterpolators[Info.CarClass]);
                    if (!double.IsNaN(gap)) GapToClassLeader = gap;
                }
            }
        }

        private void CalculateGapToFocusedTotal(CarData focusedCar) {
            if (focusedCar.Info.CarIndex == Info.CarIndex) { 
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

                if (Values.TrackData == null || !TrackData.LapInterpolators.ContainsKey(Info.CarClass)) {
                    GapToFocusedTotal = TotalDistanceToFocused / (175.0 / 3.6);
                    return;
                }

                var focusedPos = focusedCar.RealtimeCarUpdate?.SplinePosition;
                var thisPos = RealtimeCarUpdate?.SplinePosition;

                double gap = double.NaN;
                if (TotalSplinePosition > focusedCar.TotalSplinePosition) {
                    // This car is ahead of focused, gap should be the time it takes focused car to reach this car's position
                    // That is use focusedCar lap data to calculate the gap
                    if (!IsFinished) {
                        if (!TrackData.LapInterpolators.ContainsKey(focusedCar.Info.CarClass)) { // If focused car's best lap is not available, use this car's
                            gap = CalculateGapBetweenPos(focusedPos, thisPos, TrackData.LapInterpolators[Info.CarClass]);
                        } else {
                            gap = CalculateGapBetweenPos(focusedPos, thisPos, TrackData.LapInterpolators[focusedCar.Info.CarClass]);
                        }
                    }
                } else {
                    // This car is behind of focused, gap should be the time it takes us to reach focused car
                    // That is use this cars lap data to calculate gap
                    if (!focusedCar.IsFinished) {
                        if (!TrackData.LapInterpolators.ContainsKey(Info.CarClass)) { // If this car's best lap is not available, use focused car's
                            gap = -CalculateGapBetweenPos(thisPos, focusedPos, TrackData.LapInterpolators[focusedCar.Info.CarClass]);
                        } else {
                            gap = -CalculateGapBetweenPos(thisPos, focusedPos, TrackData.LapInterpolators[Info.CarClass]);
                        }
                    }
                    
                }
                if (!double.IsNaN(gap)) GapToFocusedTotal = gap;

            }
        }

        private void CalculateRelativeGapToFocused(CarData focusedCar) {
            var focusedPos = focusedCar.RealtimeCarUpdate?.SplinePosition;
            var thisPos = RealtimeCarUpdate?.SplinePosition;
            if (focusedPos == null || thisPos == null) { 
                GapToFocusedOnTrack = double.NaN;
                return;
            };

            if (Values.TrackData == null || (!TrackData.LapInterpolators.ContainsKey(Info.CarClass) && !TrackData.LapInterpolators.ContainsKey(focusedCar.Info.CarClass))) {
                GapToFocusedOnTrack = OnTrackDistanceToFocused / (175.0 / 3.6);
                return;
            }

            var relativeSplinePos = CalculateRelativeSplinePosition((float)thisPos, (float)focusedPos);
            double gap;
            if (relativeSplinePos > 0) {
                // This car is ahead of focused, gap should be the time it takes focused car to reach this car's position
                // That is use focusedCar lap data to calculate the gap

                if (!TrackData.LapInterpolators.ContainsKey(focusedCar.Info.CarClass)) { // If focused car's best lap is not available, use this car's
                    gap = CalculateGapBetweenPos(focusedPos, thisPos, TrackData.LapInterpolators[Info.CarClass]);
                } else {
                    gap = CalculateGapBetweenPos(focusedPos, thisPos, TrackData.LapInterpolators[focusedCar.Info.CarClass]);
                }
            } else {
                // This car is behind of focused, gap should be the time it takes us to reach focused car
                // That is use this cars lap data to calculate gap

                if (!TrackData.LapInterpolators.ContainsKey(Info.CarClass)) { // If this car's best lap is not available, use focused car's
                    gap = -CalculateGapBetweenPos(thisPos, focusedPos, TrackData.LapInterpolators[focusedCar.Info.CarClass]);
                } else {
                    gap = -CalculateGapBetweenPos(thisPos, focusedPos, TrackData.LapInterpolators[Info.CarClass]);
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
            if (RealtimeCarUpdate == null || otherCar.RealtimeCarUpdate == null) return float.NaN;
            return CalculateRelativeSplinePosition(RealtimeCarUpdate.SplinePosition, otherCar.RealtimeCarUpdate.SplinePosition);
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
            var pos = RealtimeCarUpdate?.Position ?? -1;
            float splinepos = RealtimeCarUpdate?.SplinePosition ?? -1;
            float speed = RealtimeCarUpdate?.Kmh ?? 200;
            return $"CarId {Info.CarIndex:000} #{Info.RaceNumber,-4}, {Info.Drivers[0].InitialPlusLastName(),-20} P{pos:00}/{InClassPos:00} L{RealtimeCarUpdate?.Laps ?? -1 :00}: SplinePos:{splinepos:0.000} LapsBySplinePos:{LapsBySplinePosition:00} TotalSplinePos:{TotalSplinePosition:0.000} Gaps_ToLeader:{GapToLeader:000.0}| ToClassLeader:{GapToClassLeader:000.0}| ToFocusedOnTrack:{GapToFocusedOnTrack:000.0}| ToFocusedTotal:{GapToFocusedTotal:000.0}, Distances_ToLeader:{DistanceToLeader:00000.0}| ToClassLeader:{DistanceToClassLeader:00000.0}| ToFocusedOnTrack:{OnTrackDistanceToFocused:00000.0}| ToFocusedTotal:{TotalDistanceToFocused:00000.0}";
        }

        public DriverInfo GetCurrentDriver() {
            return Info.Drivers[RealtimeCarUpdate?.DriverIndex ?? Info.CurrentDriverIndex];
        }


    }
}
