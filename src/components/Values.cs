using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using KLPlugins.Leaderboard.ksBroadcastingNetwork;
using KLPlugins.Leaderboard.ksBroadcastingNetwork.Structs;
using System.Collections.Generic;
using System.Linq;
using KLPlugins.Leaderboard.Enums;

namespace KLPlugins.Leaderboard {


    /// <summary>
    /// Storage and calculation of new properties
    /// </summary>
    public class Values : IDisposable {

        class CarSplinePos {
            // Index into Cars array
            public int CarIdx = -1;
            // Corresponding splinePosition
            public float SplinePos = 0;

            public CarSplinePos(int idx, float pos) {
                CarIdx = idx;
                SplinePos = pos;
            }
        }

        public ACCUdpRemoteClient BroadcastClient { get; private set; }
        public RealtimeUpdate RealtimeUpdate { get; private set; }
        public static TrackData TrackData { get; private set; }

        // Idea with cars is to store one copy of data
        // And store different arrays with indexes into Cars in positioning order
        // We also need a local array which we update from RealtimeCarUpdates
        // and once all updates are finished we update public arrays.
        public List<CarData> Cars { get; private set; }
        public int[] OverallPosCarsIdxs { get; private set; }
        public int[] PosInClassCarsIdxs { get; private set; }
        public int[] OverallPosOnTrackCarsIdxs { get; private set; }


        private int[] _overallPosCarsIdxs = new int[LeaderboardPlugin.Settings.NumOverallPos];
        // Store relative spline positions for relative leaderboard,
        // need to store separately as we need to sort by spline pos at the end on update loop
        private List<CarSplinePos> _relativeSplinePositions = new List<CarSplinePos>();
        // Store overall spline positions for overall leaderboard oreding in races
        private List<CarSplinePos> _overallSplinePositions = new List<CarSplinePos>();
        private int _focusedCarIdx = -1;
        private List<ushort> _lastUpdateCarIds = new List<ushort>();
        private double _clock = 0.0;
        private int _timeMultiplier = -1;

        private Dictionary<CarClass, int> ClassLeaderIdx = new Dictionary<CarClass, int>();

        public Values() {
            Cars = new List<CarData>();
            OverallPosCarsIdxs = new int[LeaderboardPlugin.Settings.NumOverallPos];
            PosInClassCarsIdxs = new int[LeaderboardPlugin.Settings.NumOverallPos];
            OverallPosOnTrackCarsIdxs = new int[LeaderboardPlugin.Settings.NumRelativePos * 2 + 1];
            ResetPos();
        }

        public void Reset() {
            if (BroadcastClient != null) {
                DisposeBroadcastClient();
            }
            RealtimeUpdate = null;
            TrackData = null;
            Cars.Clear();
            ResetPos();
            _timeMultiplier = -1;
            _clock = 0.0;
            _lastUpdateCarIds.Clear();
        }

        private void ResetPos() {
            for (int i = 0; i < LeaderboardPlugin.Settings.NumOverallPos; i++) {
                OverallPosCarsIdxs[i] = -1;
                _overallPosCarsIdxs[i] = -1;
                PosInClassCarsIdxs[i] = -1;
            }

            for (int i = 0; i < LeaderboardPlugin.Settings.NumRelativePos * 2 + 1; i++) {
                OverallPosOnTrackCarsIdxs[i] = -1;
            }

            _relativeSplinePositions.Clear();
            _overallSplinePositions.Clear();
            _focusedCarIdx = -1;
            ClassLeaderIdx.Clear();
        }


        #region IDisposable Support
        ~Values() {
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        private bool isDisposed = false;
        protected virtual void Dispose(bool disposing) {
            if (!isDisposed) {
                if (disposing) {
                    LeaderboardPlugin.LogInfo("Disposed");
                    DisposeBroadcastClient();
                }

                isDisposed = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }
        #endregion


        public void OnGameStateChanged(bool running, PluginManager manager) {
            if (running) {
                if (BroadcastClient != null) {
                    LeaderboardPlugin.LogWarn("Broadcast client wasn't 'null' at start of new event. Shouldn't be possible, there is a bug in disposing of Broadcast client from previous session.");
                    DisposeBroadcastClient();
                }
                ConnectToBroadcastClient();
            } else {
                Reset();
            }

        }

        public void OnDataUpdate(PluginManager pm, GameData data) {            
            if (_timeMultiplier == -1) {
                _timeMultiplier = (int)pm.GetPropertyValue("RaceEngineerPlugin.Session.TimeMultiplier");
            }
        }

        public CarData GetCar(int i) {
            if (i >= Cars.Count) return null;
            return Cars[i];
        }

        public CarData GetFocusedCar() {
            if (_focusedCarIdx == -1) return null;
            return Cars[_focusedCarIdx];
        }

        // DBG
        public CarData DbgGetOverallPos(int i) {
            var idx = OverallPosCarsIdxs[i];
            if (idx == -1) return null;
            return Cars[idx];
        }

        public CarData DbgGetInClassPos(int i) {
            var idx = PosInClassCarsIdxs[i];
            if (idx == -1) return null;
            return Cars[idx];
        }

        public CarData DbgGetOverallPosOnTrack(int i) {
            var idx = OverallPosOnTrackCarsIdxs[i];
            if (idx == -1) return null;
            return Cars[idx];
        }
        //DBG


        #region Broadcast client connection

        public void ConnectToBroadcastClient() {
            BroadcastClient = new ACCUdpRemoteClient("127.0.0.1", 9000, "ACCBDPlugin", "asd", "", 100);
            BroadcastClient.MessageHandler.OnConnectionStateChanged += OnBroadcastConnectionStateChanged;
            BroadcastClient.MessageHandler.OnNewEntrylist += OnNewEntryList;
            BroadcastClient.MessageHandler.OnEntrylistUpdate += OnEntryListUpdate;
            BroadcastClient.MessageHandler.OnRealtimeCarUpdate += OnRealtimeCarUpdate;
            BroadcastClient.MessageHandler.OnRealtimeUpdate += OnBroadcastRealtimeUpdate;
            BroadcastClient.MessageHandler.OnTrackDataUpdate += OnTrackDataUpdate;
        }

        public void DisposeBroadcastClient() {
            if (BroadcastClient != null) {
                BroadcastClient.Shutdown();
                BroadcastClient.Dispose();
                BroadcastClient = null;
            }
        }

        private void OnBroadcastConnectionStateChanged(int connectionId, bool connectionSuccess, bool isReadonly, string error) {
            if (connectionSuccess) {
                LeaderboardPlugin.LogInfo("Connected to broadcast client.");
            } else {
                LeaderboardPlugin.LogWarn($"Failed to connect to broadcast client. Err: {error}");
            }
        }

        // Updates come as:
        // New entry list
        // All the current CarInfos
        // Track data
        // *** Repeating
        //    Realtime update
        //    Realtime update for all the cars
        // ***
        // New entry list if found new car or driver

        private void OnBroadcastRealtimeUpdate(string sender, RealtimeUpdate update) {
            UpdateOverallPos(update);

            // Set currently focused car
            _focusedCarIdx = Cars.FindIndex(x => x.Info.CarIndex == update.FocusedCarIndex);

            if (_focusedCarIdx != -1) {
                var focusedCar = Cars[_focusedCarIdx];
                var focusedClass = focusedCar.Info.CarClass;

                UpdatePosInClass(focusedClass);
                UpdateRelativePosOnTrack(focusedCar);
            }

            _clock = (float)LeaderboardPlugin.pluginManager.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.Graphics.clock");
            _relativeSplinePositions.Clear();
            RealtimeUpdate = update;
            if (Cars.Count != 0.0 && OverallPosCarsIdxs[0] != -1 && _focusedCarIdx != -1) {
                var leaderCar = Cars[OverallPosCarsIdxs[0]];
                foreach (var car in Cars) {
                    var thisClass = car.Info.CarClass;
                    int classLeaderIdx;
                    if (ClassLeaderIdx.ContainsKey(thisClass)) {
                        classLeaderIdx = ClassLeaderIdx[thisClass];
                    } else {
                        classLeaderIdx = Array.Find(OverallPosCarsIdxs, (x) => Cars[x].Info.CarClass == thisClass);
                    }

                    car.LeaderSpeed = Cars[OverallPosCarsIdxs[0]].RealtimeUpdate?.Kmh ?? 1;
                    car.SetGaps(update, leaderCar, Cars[ClassLeaderIdx[thisClass]], Cars[_focusedCarIdx], _timeMultiplier);
                }

                Cars.RemoveAll(x => !_lastUpdateCarIds.Contains(x.Info.CarIndex));
            }

            _lastUpdateCarIds.Clear();
            // We can remove unused cars here, as there will be new indexes
        }

        private void UpdateOverallPos(RealtimeUpdate update) {
            // Update overall ordering
            if (update.SessionType == RaceSessionType.Race && _overallSplinePositions.Count != 0.0) {
                // In race use splinePosition + Laps to determine order as RealtimeCarUpdate.Position updates at the end of sector
                _overallSplinePositions.Sort((a, b) => b.SplinePos.CompareTo(a.SplinePos));
                for (int i = 0; i < LeaderboardPlugin.Settings.NumOverallPos; i++) {
                    var thisPos = _overallSplinePositions.ElementAtOrDefault(i);
                    OverallPosCarsIdxs[i] = thisPos != null ? thisPos.CarIdx : -1;
                }

                var firstPos = _overallSplinePositions.First();
                foreach (var thisPos in _overallSplinePositions) {
                    Cars[thisPos.CarIdx].DistanceToLeader = (firstPos.SplinePos - thisPos.SplinePos) * TrackData.TrackMeters;
                    var currentClass = Cars[thisPos.CarIdx].Info.CarClass;
                    var firstInClass = _overallSplinePositions.Find(x => Cars[x.CarIdx].Info.CarClass == currentClass);
                    Cars[thisPos.CarIdx].DistanceToClassLeader = (firstInClass.SplinePos - thisPos.SplinePos) * TrackData.TrackMeters;
                }

                _overallSplinePositions.Clear();
            } else {
                // In other session use RealtimeCarUpdate.Position as splinePosition + laps doesn't mean anything
                for (int i = 0; i < LeaderboardPlugin.Settings.NumOverallPos; i++) {
                    OverallPosCarsIdxs[i] = _overallPosCarsIdxs[i];
                    _overallPosCarsIdxs[i] = -1;
                }
            }
        }

        private void UpdatePosInClass(CarClass focusedClass) {
            if (Cars.Count == 0.0) return;
            Dictionary<CarClass, int> pos = new Dictionary<CarClass, int>();
            foreach (var idx in OverallPosCarsIdxs) {
                if (idx == -1) break; // Reached the end on list

                var car = Cars[idx];
                if (pos.ContainsKey(car.Info.CarClass)) {
                    pos[car.Info.CarClass]++;
                } else {
                    pos[car.Info.CarClass] = 1;
                    ClassLeaderIdx[car.Info.CarClass] = idx;
                }

                car.InClassPos = pos[car.Info.CarClass];

                if (Cars[idx].Info.CarClass == focusedClass) {
                    PosInClassCarsIdxs[pos[car.Info.CarClass] - 1] = idx;
                }
            }

            // If somebody left the session, need to reset following class positions
            var startpos = 0;
            if (pos.ContainsKey(focusedClass)) { 
                startpos = pos[focusedClass];
            }
            for (int i = startpos; i < LeaderboardPlugin.Settings.NumOverallPos; i++) {
                PosInClassCarsIdxs[i] = -1;
            }
        }

        private void UpdateRelativePosOnTrack(CarData focusedCar) {
            for (int i = 0; i < LeaderboardPlugin.Settings.NumRelativePos * 2 + 1; i++) {
                OverallPosOnTrackCarsIdxs[i] = -1;
            }

            // Calculate relative postition to focused car.
            for (int i = 0; i < _relativeSplinePositions.Count; i++) {
                var relativeSplinePos = _relativeSplinePositions[i].SplinePos - focusedCar.RealtimeUpdate.SplinePosition;
                if (relativeSplinePos > 0.5) { // Car is more than half a lap ahead, so technically it's closer from behind. Take one lap away to show it behind us.
                    relativeSplinePos -= 1;
                } else if (relativeSplinePos < -0.5) { // Car is more than half a lap behind, so it's in front. Add one lap to show it in front of us.
                    relativeSplinePos += 1;
                }

                _relativeSplinePositions[i].SplinePos = relativeSplinePos;
                Cars[_relativeSplinePositions[i].CarIdx].DistanceToFocused = relativeSplinePos*TrackData.TrackMeters;
            }

            _relativeSplinePositions.Sort((a, b) => a.SplinePos.CompareTo(b.SplinePos));

            var ahead = _relativeSplinePositions
                .Where(x => x.SplinePos > 0)
                .Take(LeaderboardPlugin.Settings.NumRelativePos)
                .Reverse()
                .ToList()
                .ConvertAll(x => x.CarIdx);
            var behind = _relativeSplinePositions
                .Where(x => x.SplinePos < 0)
                .Reverse()
                .Take(LeaderboardPlugin.Settings.NumRelativePos)
                .ToList()
                .ConvertAll(x => x.CarIdx);

            ahead.CopyTo(OverallPosOnTrackCarsIdxs, LeaderboardPlugin.Settings.NumRelativePos - ahead.Count);
            OverallPosOnTrackCarsIdxs[LeaderboardPlugin.Settings.NumRelativePos] = _focusedCarIdx;
            behind.CopyTo(OverallPosOnTrackCarsIdxs, LeaderboardPlugin.Settings.NumRelativePos + 1);

            _relativeSplinePositions.Clear();
        }


        private void OnNewEntryList(string sender) {
            // Do nothing
            //AccBroadcastDataPlugin.LogInfo("New entry list requested.");
        }

        private void OnEntryListUpdate(string sender, CarInfo car) {
            // Add new cars if not already added, update car info of all the cars (adds new drivers if some were missing)

            //AccBroadcastDataPlugin.LogInfo($"New entry list update for carId = {car.CarIndex}");
            var idx = Cars.FindIndex(x => x.Info.CarIndex == car.CarIndex);
            if (idx == -1) {
                Cars.Add(new CarData(car, null));
            } else {
                Cars[idx].Info = car;
            }
        }

        private void OnRealtimeCarUpdate(string sender, RealtimeCarUpdate update) {
            if (RealtimeUpdate == null) return;
            // Update Realtime data of existing cars
            // If found new car, BroadcastClient itself requests new entry list
            var idx = Cars.FindIndex(x => x.Info.CarIndex == update.CarIndex);
            if (idx == -1) return; // Car wasn't found, wait for entry list update
            var car = Cars[idx];
            car.OnRealtimeCarUpdate(update, _clock, RealtimeUpdate.SessionType, RealtimeUpdate.Phase);

            _relativeSplinePositions.Add(new CarSplinePos(idx, update.SplinePosition));
            if (RealtimeUpdate.SessionType == RaceSessionType.Race) {
                _overallSplinePositions.Add(new CarSplinePos(idx, update.SplinePosition + car.LapsBySplinePosition));
            } else {
                var pos = update.Position;
                _overallPosCarsIdxs[pos - 1] = idx;
            }

            _lastUpdateCarIds.Add(car.Info.CarIndex);

            //File.AppendAllText($"{AccBroadcastDataPlugin.Settings.DataLocation}\\{TrackData.TrackId}_{car.Info.CarClass}_{car.LapsBySplinePosition}.txt", $"{update.SplinePosition};{update.CurrentLap.LaptimeMS};{update.Kmh};\n");

        }

        private void OnTrackDataUpdate(string sender, TrackData update) {
            // Update track data
            //AccBroadcastDataPlugin.LogInfo("New track update.");
            TrackData = update;

            foreach (var car in Cars) {
                if (car.BestLap.Count == 0) {
                    car.ReadDefBestLap();
                }
            } 
        }

        #endregion

    }


}