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
        public RealtimeCarUpdate RealtimeUpdate { get; set; }
        public bool HasCrossedTheLineAtRaceStart { get; set; }
        public float DistanceToFocused { get; set; }
        public float DistanceToLeader { get; set; }
        public float DistanceToClassLeader { get; set; }
        public int LapsBySplinePosition { get; set; }
        public float LeaderSpeed { get; set; }

        public TimePos[] OverallTimePos = new TimePos[100];
        public int _lastUpdatedTimePos = -1;

        public double GapToLeader { get; set; }
        public double GapToClassLeader { get; set; }
        public double GapToFocused { get; set; }

        public double GapToLeaderV2 { get; set; }
        public double GapToClassLeaderV2 { get; set; }
        public double GapToFocusedV2 { get; set; }

        public List<LapPos> BestLap = new List<LapPos>();
        public List<LapPos> CurrentLap = new List<LapPos>();

        public int OverallPos { get; set; }
        public int InClassPos { get; set; }

        ////////////////////////

        public CarData(CarInfo info, RealtimeCarUpdate update) {
            Info = info;
            RealtimeUpdate = update;
            HasCrossedTheLineAtRaceStart = false;
            DistanceToFocused = float.NaN;
            DistanceToLeader = float.NaN;
            DistanceToClassLeader = float.NaN;
            LapsBySplinePosition = 0;
            LeaderSpeed = 1;
            OverallPos = -1;
            InClassPos = -1;
            ReadDefBestLap();
        }

        public void OnRealtimeCarUpdate(RealtimeCarUpdate update, double clock, RaceSessionType session, SessionPhase phase) {
            // RealtimeCarUpdate.Laps and SplinePosition updates are not always in sync.
            // This results in some weirdness on lap finish. Count laps myself based on spline position.
            if (RealtimeUpdate != null && RealtimeUpdate.SplinePosition > 0.9 && update.SplinePosition < 0.1 && RealtimeUpdate.CarLocation == update.CarLocation) {
                LapsBySplinePosition++;

                CurrentLap.RemoveAll(x => x.SplinePos > 0.9 && x.LapTime < 10); // LapTime can reset before splinePos resets, remove such weird points
                if (CurrentLap.Count != 0 && update.LastLap.IsValidForBest && LapsBySplinePosition > 2 && update.LastLap.Type == LapType.Regular) {
                    // Use last valid proper lap
                    BestLap = new List<LapPos>(CurrentLap);
                }

                CurrentLap.Clear();
            }

            if (session == RaceSessionType.Race) {
                var timePosIdx = (int)Math.Floor(update.SplinePosition * 100);
                if (timePosIdx > 99) {
                    timePosIdx = 99;
                } 

                if (timePosIdx != _lastUpdatedTimePos) {
                    if (timePosIdx - _lastUpdatedTimePos > 1 && timePosIdx != 0 && _lastUpdatedTimePos != 100) {
                        // We jumped over some buckets, set missed buckets to null, so we don't use them accidentaly to get wrong gap
                        for (int i = _lastUpdatedTimePos + 1; i < timePosIdx; i++) {
                            OverallTimePos[i] = null;
                        }
                    }
                    OverallTimePos[timePosIdx] = new TimePos(update.SplinePosition + LapsBySplinePosition, clock);
                    _lastUpdatedTimePos = timePosIdx;
                }
            }

            if (update.CurrentLap.LaptimeMS != null && phase == SessionPhase.Session && update.Kmh > 10 && LapsBySplinePosition != 0) {
                CurrentLap.Add(new LapPos(update.SplinePosition, update.CurrentLap.LaptimeMS / 1000.0 ?? 0.0, update.Kmh));
            }

            RealtimeUpdate = update;
        }

        public void SetGaps(RealtimeUpdate update, CarData leader, CarData classLeader, CarData focused, int timeMultiplier) {
            if (update.SessionType == RaceSessionType.Race && timeMultiplier != -1) {

                CalculateGapsV2(leader, classLeader, focused);
                CalculateGapsV1(leader, classLeader, focused, timeMultiplier);


            } else if (update.SessionType != RaceSessionType.Race) {
                var thisBestLap = update.BestSessionLap.LaptimeMS;
                if (thisBestLap == null) return;

                var leaderBestLap = leader.RealtimeUpdate?.BestSessionLap.LaptimeMS;
                if (thisBestLap != null && leaderBestLap != null) {
                    GapToLeader = ((double)thisBestLap - (double)leaderBestLap) / 1000.0;
                }

                var classLeaderBestLap = leader.RealtimeUpdate?.BestSessionLap.LaptimeMS;
                if (thisBestLap != null && classLeaderBestLap != null) {
                    GapToClassLeader = ((double)thisBestLap - (double)classLeaderBestLap) / 1000.0;
                }
            }
        }

        private void CalculateGapsV2(CarData leader, CarData classLeader, CarData focused) {
            CalculateGapToLeader(leader);
            CalculateGapToClassLeader(classLeader);
            CalculateRelativeGapToFocused(focused);
        }

        public void CalculateGapToLeader(CarData leader) {
            if (DistanceToLeader > Values.TrackData.TrackMeters) {
                GapToLeaderV2 = -Math.Floor((DistanceToLeader) / Values.TrackData.TrackMeters);
            } else {
                var leaderPos = leader.RealtimeUpdate?.SplinePosition;
                var thisPos = RealtimeUpdate?.SplinePosition;
                if (leaderPos == null || thisPos == null) return;

                if (BestLap.Count == 0) {
                    GapToLeaderV2 = DistanceToLeader / (175.0 / 3.6);
                    return;
                }
                var startIdx = BestLap.Find(x => x.SplinePos >= thisPos);
                var endIdx = BestLap.Find(x => x.SplinePos >= leaderPos);
                if (startIdx == null || endIdx == null) return;
                var best = BestLap.FindLast(x => x.LapTime > 10).LapTime;
                if (leaderPos < thisPos) {
                    // Leader is on another lap, gap is time for this to end lap, and then reach leader pos
                    GapToLeaderV2 = best - startIdx.LapTime + endIdx.LapTime;
                } else {
                    // We must be on the same lap, gap is time for this to reach leader pos from current pos
                    GapToLeaderV2 = endIdx.LapTime - startIdx.LapTime;
                }
            }
        }

        public void CalculateGapToClassLeader(CarData classLeader) {
            if (DistanceToClassLeader > Values.TrackData.TrackMeters) {
                GapToClassLeaderV2 = -Math.Floor((DistanceToClassLeader) / Values.TrackData.TrackMeters);
            } else {
                var leaderPos = classLeader.RealtimeUpdate?.SplinePosition;
                var thisPos = RealtimeUpdate?.SplinePosition;
                if (leaderPos == null || thisPos == null) return;

                if (BestLap.Count == 0) {
                    GapToClassLeaderV2 = DistanceToClassLeader / (175.0 / 3.6);
                    return;
                }
                var startIdx = BestLap.Find(x => x.SplinePos >= thisPos);
                var endIdx = BestLap.Find(x => x.SplinePos >= leaderPos);
                if (startIdx == null || endIdx == null) return;
                var best = BestLap.FindLast(x => x.LapTime > 10).LapTime;
                if (leaderPos < thisPos) {
                    // Leader is on another lap, gap is time for this to end lap, and then reach leader pos
                    GapToClassLeaderV2 = best - startIdx.LapTime + endIdx.LapTime;
                } else {
                    // We must be on the same lap, gap is time for this to reach leader pos from current pos
                    GapToClassLeaderV2 = endIdx.LapTime - startIdx.LapTime;
                }
            }
        }

        public void CalculateRelativeGapToFocused(CarData focusedCar) {
            var focusedPos = focusedCar.RealtimeUpdate?.SplinePosition;
            var thisPos = RealtimeUpdate?.SplinePosition;
            if (focusedPos == null || thisPos == null) return;
            

            var relativeSplinePos = thisPos - focusedPos;
            if (relativeSplinePos > 0.5) { // Car is more than half a lap ahead, so technically it's closer from behind. Take one lap away to show it behind us.
                relativeSplinePos -= 1;
            } else if (relativeSplinePos < -0.5) { // Car is more than half a lap behind, so it's in front. Add one lap to show it in front of us.
                relativeSplinePos += 1;
            }
            bool isAhead = relativeSplinePos > 0;

            if (isAhead) {
                // This car is ahead of focused, gap should be the time it takes focused car to reach this car's position
                // That is use focusedCar lap data to calculate the gap

                if (focusedCar.BestLap.Count == 0) {
                    GapToFocusedV2 = DistanceToFocused / (175.0 / 3.6);
                    return;
                }

                var start = focusedCar.BestLap.Find(x => x.SplinePos >= focusedPos);
                var end = focusedCar.BestLap.Find(x => x.SplinePos >= thisPos);
                if (start == null || end == null) return;
                var best = BestLap.FindLast(x => x.LapTime > 10).LapTime;
                if (thisPos < focusedPos) {
                    // Leader is on another lap, gap is time for this to end lap, and then reach leader pos
                    GapToFocusedV2 = best - start.LapTime + end.LapTime;
                } else {
                    // We must be on the same lap, gap is time for this to reach leader pos from current pos
                    GapToFocusedV2 = end.LapTime - start.LapTime;
                }

            } else {
                // This car is behind of focused, gap should be the time it takes us to reach focused car
                // That is use this cars lap data to calculate gap

                if (BestLap.Count == 0) {
                    GapToFocusedV2 = DistanceToFocused / (175.0 / 3.6);
                    return;
                }
                var start = BestLap.Find(x => x.SplinePos >= thisPos);
                var end = BestLap.Find(x => x.SplinePos >= focusedPos);
                if (start == null || end == null) return;
                var best = BestLap.FindLast(x => x.LapTime > 10).LapTime;
                if (focusedPos < thisPos) {
                    // Leader is on another lap, gap is time for this to end lap, and then reach leader pos
                    GapToFocusedV2 = best - start.LapTime + end.LapTime;
                } else {
                    // We must be on the same lap, gap is time for this to reach leader pos from current pos
                    GapToFocusedV2 = end.LapTime - start.LapTime;
                }
            }
        }



        private void CalculateGapsV1(CarData leader, CarData classLeader, CarData focused, int timeMultiplier) {
            // Gaps V1
            var thisPos = OverallTimePos[_lastUpdatedTimePos];
            if (thisPos == null) return;

            var leaderPos = leader.OverallTimePos[_lastUpdatedTimePos];
            if (leaderPos != null) { // If null leader has missed current time bucket, keep previous gap.
                                     // BUG: If this car is standing, this will only update once leader passes this car
                var gap = (thisPos.Time - leaderPos.Time) / timeMultiplier;
                if (gap > 0) {
                    GapToLeader = gap;
                }
                if (DistanceToLeader > Values.TrackData.TrackMeters) {
                    GapToLeader = -Math.Floor((DistanceToLeader + 100) / Values.TrackData.TrackMeters);
                }
            }

            var classLeaderPos = classLeader.OverallTimePos[_lastUpdatedTimePos];
            if (classLeaderPos != null) {
                // BUG: If this car is standing, this will only update once class leader passes this car
                var gap = (thisPos.Time - classLeaderPos.Time) / timeMultiplier;
                if (gap > 0) {
                    GapToClassLeader = gap;
                }

                if (DistanceToClassLeader > Values.TrackData.TrackMeters) {
                    GapToClassLeader = -Math.Floor((DistanceToClassLeader + 100) / Values.TrackData.TrackMeters);
                }

            }

            // Index into OverlapTimePos needs to be from the car that's behind.
            // Otherwise the car behind hasn't updated that given position yet and would be shown lap behind.
            var relativeSplinePos = RealtimeUpdate.SplinePosition - focused.RealtimeUpdate.SplinePosition;
            if (relativeSplinePos > 0.5) { // Car is more than half a lap ahead, so technically it's closer from behind. Take one lap away to show it behind us.
                relativeSplinePos -= 1;
            } else if (relativeSplinePos < -0.5) { // Car is more than half a lap behind, so it's in front. Add one lap to show it in front of us.
                relativeSplinePos += 1;
            }
            int idx;
            if (relativeSplinePos > 0) {
                idx = focused._lastUpdatedTimePos;
            } else {
                idx = _lastUpdatedTimePos;
            }

            var focusedPos = focused.OverallTimePos[idx];
            thisPos = OverallTimePos[idx];
            if (focusedPos != null && thisPos != null) {
                GapToFocused = (focusedPos.Time - thisPos.Time) / timeMultiplier;
                // BUG: If either car is standing, this will only update once they pass each other
            }
        }

        public override string ToString() {
            var pos = RealtimeUpdate?.Position ?? -1;
            float trackpos = RealtimeUpdate?.Delta ?? -1;
            float speed = RealtimeUpdate?.Kmh ?? 200;
            var laptime = BestLap.Count != 0.0 ? BestLap.Last().LapTime : -1.0;
            return $"CarId {Info.CarIndex} #{Info.RaceNumber}, {Info.Drivers[0].FirstName} {Info.Drivers[0].LastName}: P{pos}/{InClassPos} GapToLeader:{GapToLeaderV2:0.0}/{GapToClassLeaderV2:0.0}/{GapToFocusedV2:0.0} Best.Last:{laptime} {CurrentLap.Count}/{BestLap.Count}";


            //return $"CarId {Info.CarIndex}: P{pos} Gap200kmh:{DistanceToLeader/55.0:0.00}, GapCurrentSpeed:{DistanceToLeader/(speed / 3.6):0.00}, GapLeaderSpeed:{DistanceToLeader/(LeaderSpeed/3.6):0.00} #{Info.RaceNumber}, {Info.Drivers[0].FirstName} {Info.Drivers[0].LastName}";
        }

        public DriverInfo GetCurrentDriver() {
            return Info.Drivers[RealtimeUpdate?.DriverIndex ?? Info.CurrentDriverIndex];
        }

        public void ReadDefBestLap() {
            if (Values.TrackData == null) return;

            string carClass = Info.CarClass.ToString();
            if (carClass.StartsWith("CUP") || carClass.StartsWith("ST") || carClass == "CHL") {
                carClass = "GTC";
            }

            var fname = $"{LeaderboardPlugin.Settings.DataLocation}\\laps\\{Values.TrackData.TrackId}_{carClass}.txt";
            if (!File.Exists(fname)) return;

            BestLap.Clear();
            foreach (var l in File.ReadLines(fname)) {
                var splits = l.Split(';');
                BestLap.Add(new LapPos(float.Parse(splits[0]), double.Parse(splits[1]) / 1000.0, float.Parse(splits[2])));
            }

        }

    }
}
