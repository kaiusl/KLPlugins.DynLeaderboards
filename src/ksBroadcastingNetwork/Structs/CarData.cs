using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KLPlugins.Leaderboard.ksBroadcastingNetwork.Structs {
    public class CarData {

        public class TimePos {
            public float SplinePos = 0.0f;
            public double Time = 0.0;

            public TimePos(float splinePos, double time) {
                SplinePos = splinePos;
                Time = time;
            }
        }

        public class LapPos {
            public float SplinePos = 0.0f;
            public double LapTime = 0.0;
            public float Speed = 0.0f;

            public LapPos(float splinePos, double lapTime, float speed) {
                SplinePos = splinePos;
                LapTime = lapTime;
                Speed = speed;

            }
        }

        public CarInfo Info { get; set; }
        public RealtimeCarUpdate RealtimeCarUpdate { get; set; }
        public float TotalSplinePosition { get; set; }
        public float DistanceToFocused { get; set; }
        public float DistanceToLeader { get; set; }
        public float DistanceToClassLeader { get; set; }
        public int LapsBySplinePosition { get; set; }

        public double GapToLeader { get; set; }
        public double GapToClassLeader { get; set; }
        public double GapToFocused { get; set; }

        public List<LapPos> BestLap = new List<LapPos>();
        public List<LapPos> CurrentLap = new List<LapPos>();

        public int OverallPos { get; set; }
        public int InClassPos { get; set; }

        public int MissedRealtimeUpdates { get; set; }


        ////////////////////////

        public CarData(CarInfo info, RealtimeCarUpdate update) {
            Info = info;
            RealtimeCarUpdate = update;
            DistanceToFocused = float.NaN;
            DistanceToLeader = float.NaN;
            DistanceToClassLeader = float.NaN;
            LapsBySplinePosition = 0;
            OverallPos = -1;
            InClassPos = -1;
            ReadDefBestLap();
            GapToClassLeader = double.NaN;
            GapToLeader = double.NaN;
            GapToFocused = double.NaN;
        }

        public void OnNewSession() {
            DistanceToFocused = float.NaN;
            DistanceToLeader = float.NaN;
            DistanceToClassLeader = float.NaN;
            LapsBySplinePosition = 0;
            OverallPos = -1;
            InClassPos = -1;
            GapToClassLeader = double.NaN;
            GapToLeader = double.NaN;
            GapToFocused = double.NaN;
            RealtimeCarUpdate = null;
            CurrentLap.Clear();
        }

        public void OnRealtimeCarUpdate(RealtimeCarUpdate update, RaceSessionType session, SessionPhase phase) {
            // RealtimeCarUpdate.Laps and SplinePosition updates are not always in sync.
            // This results in some weirdness on lap finish. Count laps myself based on spline position.
            if (RealtimeCarUpdate != null && RealtimeCarUpdate.SplinePosition > 0.9 && update.SplinePosition < 0.1 && RealtimeCarUpdate.CarLocation == update.CarLocation) {
                LapsBySplinePosition++;

                CurrentLap.RemoveAll(x => x.SplinePos > 0.9 && x.LapTime < 10); // LapTime can reset before splinePos resets, remove such weird points
                if (CurrentLap.Count != 0 && update.LastLap.IsValidForBest && LapsBySplinePosition > 2 && update.LastLap.Type == LapType.Regular) {
                    // Use last valid proper lap
                    BestLap = new List<LapPos>(CurrentLap);
                }

                CurrentLap.Clear();
            }

            // On certain tracks (Spa) first half of the grid is ahead of the start/finish line,
            // need to add the line crossing lap, otherwise they will be shown lap behind
            if (RealtimeCarUpdate != null && (phase == SessionPhase.PreFormation) && update.SplinePosition < 0.1 && LapsBySplinePosition == 0) {
                RealtimeCarUpdate = update;
                LapsBySplinePosition++;
            }


            if (update.CurrentLap.LaptimeMS != null && phase == SessionPhase.Session && update.Kmh > 10 && LapsBySplinePosition != 0) {
                CurrentLap.Add(new LapPos(update.SplinePosition, update.CurrentLap.LaptimeMS / 1000.0 ?? 0.0, update.Kmh));
            }

            TotalSplinePosition = update.SplinePosition + LapsBySplinePosition;
            RealtimeCarUpdate = update;
        }

        public void OnRealtimeUpdate(RealtimeUpdate update, CarData leaderCar, CarData classLeaderCar, CarData focusedCar, int classPos, float relSplinePos) {
            InClassPos = classPos;
            DistanceToLeader = (leaderCar.TotalSplinePosition - TotalSplinePosition) * Values.TrackData.TrackMeters;
            DistanceToClassLeader = (classLeaderCar.TotalSplinePosition - TotalSplinePosition) * Values.TrackData.TrackMeters;
            DistanceToFocused = relSplinePos * Values.TrackData.TrackMeters;
            SetGaps(update, leaderCar, classLeaderCar, focusedCar);
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
            CalculateRelativeGapToFocused(focused);
        }

        private void CalculateGapToLeader(CarData leader) {
            if (DistanceToLeader > Values.TrackData.TrackMeters) {
                GapToLeader = -Math.Floor((DistanceToLeader) / Values.TrackData.TrackMeters);
            } else {
                if (BestLap.Count == 0) {
                    GapToLeader = DistanceToLeader / (175.0 / 3.6);
                    return;
                }

                var leaderPos = leader.RealtimeCarUpdate?.SplinePosition;
                var thisPos = RealtimeCarUpdate?.SplinePosition;
                var gap = CalculateGapBetweenPos(thisPos, leaderPos, BestLap);
                if (!double.IsNaN(gap)) GapToLeader = gap;
            }
        }

        private void CalculateGapToClassLeader(CarData classLeader) {
            if (DistanceToClassLeader > Values.TrackData.TrackMeters) {
                GapToClassLeader = -Math.Floor((DistanceToClassLeader) / Values.TrackData.TrackMeters);
            } else {
                if (BestLap.Count == 0) {
                    GapToClassLeader = DistanceToClassLeader / (175.0 / 3.6);
                    return;
                }

                var leaderPos = classLeader.RealtimeCarUpdate?.SplinePosition;
                var thisPos = RealtimeCarUpdate?.SplinePosition;
                var gap = CalculateGapBetweenPos(thisPos, leaderPos, BestLap);
                if (!double.IsNaN(gap)) GapToClassLeader = gap; 
            }
        }

        private void CalculateRelativeGapToFocused(CarData focusedCar) {
            var focusedPos = focusedCar.RealtimeCarUpdate?.SplinePosition;
            var thisPos = RealtimeCarUpdate?.SplinePosition;
            if (focusedPos == null || thisPos == null) { 
                GapToFocused = double.NaN;
                return;
            };

            if (focusedCar.BestLap.Count == 0 && BestLap.Count == 0) {
                GapToFocused = DistanceToFocused / (175.0 / 3.6);
                return;
            }

            var relativeSplinePos = CalculateRelativeSplinePosition((float)thisPos, (float)focusedPos);
            double gap;
            if (relativeSplinePos > 0) {
                // This car is ahead of focused, gap should be the time it takes focused car to reach this car's position
                // That is use focusedCar lap data to calculate the gap

                if (focusedCar.BestLap.Count == 0) { // If focused car's best lap is not available, use this car's
                    gap = CalculateGapBetweenPos(focusedPos, thisPos, BestLap);
                } else {
                    gap = CalculateGapBetweenPos(focusedPos, thisPos, focusedCar.BestLap);
                }
            } else {
                // This car is behind of focused, gap should be the time it takes us to reach focused car
                // That is use this cars lap data to calculate gap

                if (BestLap.Count == 0) { // If this car's best lap is not available, use focused car's
                    gap = CalculateGapBetweenPos(thisPos, focusedPos, focusedCar.BestLap);
                } else {
                    gap = CalculateGapBetweenPos(thisPos, focusedPos, BestLap);
                }
            }
            if (!double.IsNaN(gap)) GapToFocused = gap;
        }

        /// <summary>
        /// Calculates the gap in seconds from <paramref name="behindPos"/> to <paramref name="aheadPos"/> using lap data from <paramref name="lap"/>.
        /// </summary>
        /// <returns>
        /// Positive value or <typeparamref name="double"><c>NaN</c> if gap cannot be calculated.
        /// </returns>
        /// <param name="behindPos"></param>
        /// <param name="aheadPos"></param>
        /// <param name="lap"></param>
        /// <returns></returns>
        public static double CalculateGapBetweenPos(float? behindPos, float? aheadPos, List<LapPos> lap) {
            if (behindPos == null || aheadPos == null) return double.NaN;
            var startIdx = lap.Find(x => x.SplinePos >= behindPos);
            var endIdx = lap.Find(x => x.SplinePos >= aheadPos);
            if (startIdx == null || endIdx == null) return double.NaN;
            var best = lap.FindLast(x => x.LapTime > 10).LapTime;
            if (aheadPos < behindPos) {
                // Ahead is on another lap, gap is time for behindpos to end lap, and then reach aheadpos
                return best - startIdx.LapTime + endIdx.LapTime;
            } else {
                // We must be on the same lap, gap is time for behindpos to reach aheadpos
                return endIdx.LapTime - startIdx.LapTime;
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
            var laptime = BestLap.Count != 0.0 ? BestLap.Last().LapTime : -1.0;
            return $"CarId {Info.CarIndex} #{Info.RaceNumber}, {Info.Drivers[0].FirstName} {Info.Drivers[0].LastName}: SP{splinepos} L{LapsBySplinePosition} P{pos}/{InClassPos} GapToLeader:{GapToLeader:0.0}/{GapToClassLeader:0.0}/{GapToFocused:0.0} Best.Last:{laptime} {CurrentLap.Count}/{BestLap.Count}";


            //return $"CarId {Info.CarIndex}: P{pos} Gap200kmh:{DistanceToLeader/55.0:0.00}, GapCurrentSpeed:{DistanceToLeader/(speed / 3.6):0.00}, GapLeaderSpeed:{DistanceToLeader/(LeaderSpeed/3.6):0.00} #{Info.RaceNumber}, {Info.Drivers[0].FirstName} {Info.Drivers[0].LastName}";
        }

        public DriverInfo GetCurrentDriver() {
            return Info.Drivers[RealtimeCarUpdate?.DriverIndex ?? Info.CurrentDriverIndex];
        }

        /// <summary>
        /// Read default lap data for calculation of gaps.
        /// </summary>
        public void ReadDefBestLap() {
            if (Values.TrackData == null) return;

            string carClass = Info.CarClass.ToString();
            if (carClass.StartsWith("CUP") || carClass.StartsWith("ST") || carClass == "CHL") {
                carClass = "GTC";
            }

            var fname = $"{LeaderboardPlugin.Settings.PluginDataLocation}\\laps\\{Values.TrackData.TrackId}_{carClass}.txt";
            if (!File.Exists(fname)) return;

            BestLap.Clear();
            foreach (var l in File.ReadLines(fname)) {
                var splits = l.Split(';');
                BestLap.Add(new LapPos(float.Parse(splits[0]), double.Parse(splits[1]) / 1000.0, float.Parse(splits[2])));
            }

        }

    }
}
