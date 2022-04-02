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
using Newtonsoft.Json;
using KLPlugins.Leaderboard.src.ksBroadcastingNetwork.Structs;

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
        public RealtimeData RealtimeData { get; private set; }
        public static TrackData TrackData { get; private set; }

        // Idea with cars is to store one copy of data
        // We keep cars array sorted in overall position order
        // Other orderings are stored in different array containing indices into Cars list
        public List<CarData> Cars { get; private set; }
        public int[] PosInClassCarsIdxs { get; private set; }
        public int[] RelativePosOnTrackCarsIdxs { get; private set; }
        public int FocusedCarIdx = -1;
        public CarClassArray<int> BestLapByClassCarIdxs = new CarClassArray<int>(-1);
        public double MaxDriverStintTime = -1;
        public double MaxDriverTotalDriveTime = -1;

        // Store relative spline positions for relative leaderboard,
        // need to store separately as we need to sort by spline pos at the end on update loop
        private List<CarSplinePos> _relativeSplinePositions = new List<CarSplinePos>();
        private CarClassArray<int> _classLeaderIdxs = new CarClassArray<int>(-1); // Indexes of class leaders in Cars list
        private List<ushort> _lastUpdateCarIds = new List<ushort>();
        private ACCUdpRemoteClientConfig _broadcastConfig;
        private bool _startingPositionsSet = false;
        private const int _numClasses = 9;

        public Values() {
            Cars = new List<CarData>();
            PosInClassCarsIdxs = new int[LeaderboardPlugin.Settings.NumOverallPos];
            RelativePosOnTrackCarsIdxs = new int[LeaderboardPlugin.Settings.NumRelativePos * 2 + 1];
            ResetPos();
            _broadcastConfig = new ACCUdpRemoteClientConfig("127.0.0.1", "KLLeaderboardPlugin", LeaderboardPlugin.Settings.BroadcastDataUpdateRateMs);
        }

        public void Reset() {
            if (BroadcastClient != null) {
                DisposeBroadcastClient();
            }
            RealtimeData = null;
            TrackData = null;
            Cars.Clear();
            ResetPos();
            _lastUpdateCarIds.Clear();
            _classLeaderIdxs.SetAll(-1);
            BestLapByClassCarIdxs.SetAll(-1);
            _relativeSplinePositions.Clear();
            _startingPositionsSet = false;
            MaxDriverStintTime = -1;
            MaxDriverTotalDriveTime = -1;
        }

        private void ResetPos() {
            for (int i = 0; i < LeaderboardPlugin.Settings.NumOverallPos; i++) {
                PosInClassCarsIdxs[i] = -1;
            }

            for (int i = 0; i < LeaderboardPlugin.Settings.NumRelativePos * 2 + 1; i++) {
                RelativePosOnTrackCarsIdxs[i] = -1;
            }

            _relativeSplinePositions.Clear();
            FocusedCarIdx = -1;
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
            //if (BroadcastClient != null && !BroadcastClient.IsConnected) {
            //    DisposeBroadcastClient();
            //    ConnectToBroadcastClient();
            //}
        }

        public CarData GetCar(int i) {
            if (i >= Cars.Count) return null;
            return Cars[i];
        }

        public CarData GetFocusedCar() {
            if (FocusedCarIdx == -1) return null;
            return Cars[FocusedCarIdx];
        }

        public CarData GetBestLapCar(CarClass cls) {
            var idx = BestLapByClassCarIdxs[cls];
            if (idx == -1) return null;
            return Cars[(int)idx];
        }

        public int? GetBestLapCarIdx(CarClass cls) {
            return BestLapByClassCarIdxs[cls];
        }

        public CarData GetFocusedClassBestLapCar() {
            var focusedClass = GetFocusedCar()?.CarClass;
            if (focusedClass == null) return null;
            var idx = BestLapByClassCarIdxs[(CarClass)focusedClass];
            if (idx == -1) return null;
            return Cars[(int)idx];
        }

        public int? GetFocusedClassBestLapCarIdx() {
            var focusedClass = GetFocusedCar()?.CarClass;
            if (focusedClass == null) return null;
            return BestLapByClassCarIdxs[(CarClass)focusedClass];
        }


        // DBG
        public CarData DbgGetInClassPos(int i) {
            var idx = PosInClassCarsIdxs[i];
            if (idx == -1) return null;
            return Cars[idx];
        }

        public CarData DbgGetOverallPosOnTrack(int i) {
            var idx = RelativePosOnTrackCarsIdxs[i];
            if (idx == -1) return null;
            return Cars[idx];
        }
        //DBG


        #region Broadcast client connection

        public void ConnectToBroadcastClient() {
            BroadcastClient = new ACCUdpRemoteClient(_broadcastConfig);
            //BroadcastClient.MessageHandler.OnConnectionStateChanged += OnBroadcastConnectionStateChanged;
            BroadcastClient.MessageHandler.OnEntrylistUpdate += OnEntryListUpdate;
            BroadcastClient.MessageHandler.OnRealtimeCarUpdate += OnRealtimeCarUpdate;
            BroadcastClient.MessageHandler.OnRealtimeUpdate += OnBroadcastRealtimeUpdate;
            BroadcastClient.MessageHandler.OnTrackDataUpdate += OnTrackDataUpdate;
        }

        public async void DisposeBroadcastClient() {
            if (BroadcastClient != null) {
                await BroadcastClient.ShutdownAsync();
                BroadcastClient.Dispose();
                BroadcastClient = null;
            }
        }

        //private void OnBroadcastConnectionStateChanged(int connectionId, bool connectionSuccess, bool isReadonly, string error) {
        //    if (connectionSuccess) {
        //        LeaderboardPlugin.LogInfo("Connected to broadcast client.");
        //    } else {
        //        LeaderboardPlugin.LogWarn($"Failed to connect to broadcast client. Err: {error}");
        //    }
        //}

        // Updates come as:
        // New entry list
        // All the current CarInfos
        // Track data
        // *** Repeating
        //    Realtime update
        //    Realtime update for all the cars
        // ***
        // New entry list if found new car or driver

        #region RealtimeUpdate


        private void OnBroadcastRealtimeUpdate(string sender, RealtimeUpdate update) {
            var swatch = Stopwatch.StartNew();

            if (RealtimeData == null) {
                RealtimeData = new RealtimeData(update);
                return;
            } else {
                RealtimeData.OnRealtimeUpdate(update);
            }

            if (RealtimeData.IsNewSession) {
                LeaderboardPlugin.LogInfo("New session.");
                Cars.Clear();
                BroadcastClient.MessageHandler.RequestEntryList();
                ResetPos();
                _lastUpdateCarIds.Clear();
                _relativeSplinePositions.Clear();
                _startingPositionsSet = false;
            }

            if (RealtimeData.IsRace && RealtimeData.IsPreSession && MaxDriverStintTime == -1) {
                MaxDriverStintTime = (int)LeaderboardPlugin.PManager.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.Graphics.DriverStintTimeLeft") / 1000.0;
                MaxDriverTotalDriveTime = (int)LeaderboardPlugin.PManager.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.Graphics.DriverStintTotalTimeLeft") / 1000.0;
                if (MaxDriverTotalDriveTime == 65535) { // This is max, essentially the limit doesn't exist then
                    MaxDriverTotalDriveTime = -1;
                }
            }


            if (Cars.Count == 0) return;
            ClearMissingCars();
            SetOverallOrder();
            FocusedCarIdx = Cars.FindIndex(x => x.CarIndex == update.FocusedCarIndex);
            if (FocusedCarIdx != -1 && !RealtimeData.IsNewSession) {
                UpdateCarData();
            }

            swatch.Stop();
            TimeSpan ts = swatch.Elapsed;
            File.AppendAllText($"{LeaderboardPlugin.Settings.PluginDataLocation}\\Logs\\timings\\OnRealtimeUpdate_{LeaderboardPlugin.PluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");
        }

        private void ClearMissingCars() {
            // Idea here is that realtime updates come as repeating loop of
            // * Realtime update
            // * RealtimeCarUpdate for each car
            // Thus if we keep track of cars seen in the last loop, we can remove cars that have left the session
            // However as we recieve data as UDP packets, there is a possibility that some packets go missing
            // Then we could possibly remove cars that are actually still in session
            // Thus we keep track of how many times in order each car hasn't recieved the update
            // If it's larger than some number, we remove the car
            if (_lastUpdateCarIds.Count != 0) {
                foreach (var car in Cars) {
                    if (!_lastUpdateCarIds.Contains(car.CarIndex)) {
                        car.MissedRealtimeUpdates++;
                    } else {
                        car.MissedRealtimeUpdates = 0;
                    }
                }

                // Also don't remove cars that have finished as we want to freeze the results
                var numRemovedCars = Cars.RemoveAll(x => x.MissedRealtimeUpdates > 10 && !x.IsFinished);
            }
            _lastUpdateCarIds.Clear();
        }

        private void SetOverallOrder() {
            // Sort cars in overall position order
            if (RealtimeData.IsRace) {
                // Set starting positions. Should be set by ACCs positions as positions by splinePosition can be slightly off from that
                if (!_startingPositionsSet && Cars.All(x => x.NewData != null)) {
                    SetStartionOrder();
                }


                // In race use TotalSplinePosition (splinePosition + laps) which updates realtime.
                // RealtimeCarUpdate.Position only updates at the end of sector
                // Also larger TotalSplinePosition means car is in front, so sort in descending order

                int cmp(CarData a, CarData b) {
                    if ((a.IsFinished || b.IsFinished || a.TotalSplinePosition == b.TotalSplinePosition) && a.NewData != null && b.NewData != null) {
                        return a.NewData.Position.CompareTo(b.NewData.Position);
                    }
                    return b.TotalSplinePosition.CompareTo(a.TotalSplinePosition);
                };

                Cars.Sort(cmp);
            } else {
                // In other sessions TotalSplinePosition doesn't make any sense, use RealtimeCarUpdate.Position

                int cmp(CarData a, CarData b) {
                    var apos = a.NewData?.Position ?? 1001;
                    var bpos = b.NewData?.Position ?? 1000;
                    return apos.CompareTo(bpos);
                }


                 Cars.Sort(cmp);
            }
        }

        private void SetStartionOrder() {
            Cars.Sort((a, b) => a.NewData.Position.CompareTo(b.NewData.Position));
            int?[] classPos = new int?[_numClasses];

            for (int i = 0; i < Cars.Count; i++) {
                var thisCar = Cars[i];

                var thisClass = thisCar.CarClass;
                var clsPos = classPos[(int)thisClass];

                if (clsPos != null) {
                    clsPos++;
                } else {
                    // First time seeing this class car, must be the class leader
                    clsPos = 1;
                }

                thisCar.SetStartingPositions(i + 1, (int)clsPos);
                classPos[(int)thisClass] = clsPos;
            }
            _startingPositionsSet = true;
        }

        /// <summary>
        /// Update car related data like positions and gaps
        /// </summary>
        private void UpdateCarData() {
            // Clear old data
            _relativeSplinePositions.Clear();

            var classPositions = new CarClassArray<int>(-1); 
            var leaderCar = Cars[0];
            var focusedCar = Cars[FocusedCarIdx];
            var focusedClass = focusedCar.CarClass;
            var aheadInClassCarIdxs = new CarClassArray<int>(-1);

            _classLeaderIdxs.SetAll(-1);
            BestLapByClassCarIdxs.SetAll(-1);
 
            for (int i = 0; i < Cars.Count; i++) {
                var thisCar = Cars[i];

                var thisClass = thisCar.CarClass;
                var clsPos = classPositions[thisClass];
                if (clsPos != -1) {
                    clsPos++;
                } else {
                    // First time seeing this class car, must be the class leader
                    clsPos = 1;
                    _classLeaderIdxs[thisClass] = i;
                }
                classPositions[thisClass] = clsPos;

                if (thisClass == focusedClass) {
                    PosInClassCarsIdxs[(int)clsPos - 1] = i;
                }

                var carAhead = i != 0 ? Cars[i - 1] : null;
                var carAheadInClassIdx = aheadInClassCarIdxs[thisClass];
                var carAheadInClass = carAheadInClassIdx != -1 ? Cars[(int)carAheadInClassIdx] : null;

                var relSplinePos = thisCar.CalculateRelativeSplinePosition(focusedCar);
                thisCar.OnRealtimeUpdate(RealtimeData, leaderCar, Cars[_classLeaderIdxs[thisClass]], focusedCar, carAhead, carAheadInClass,  i + 1, (int)clsPos, relSplinePos);
                aheadInClassCarIdxs[thisClass] = i;

                // Since we cannot remove cars after finish, don't add cars that have left to the relative
                if (thisCar.MissedRealtimeUpdates < 10) _relativeSplinePositions.Add(new CarSplinePos(i, relSplinePos));

                // Update best laps
                var thisBest = thisCar.NewData?.BestSessionLap?.LaptimeMS / 1000.0;
                if (thisBest != null) {
                    var thisIdx = BestLapByClassCarIdxs[thisClass];
                    if (thisIdx != -1) {
                        if (Cars[thisIdx].NewData.BestSessionLap.LaptimeMS / 1000.0 >= thisBest) BestLapByClassCarIdxs[thisClass] = i;
                    } else {
                        BestLapByClassCarIdxs[thisClass] = i;
                    }

                    var overallIdx = BestLapByClassCarIdxs[CarClass.Overall];
                    if (overallIdx != -1) {
                        if (Cars[overallIdx].NewData.BestSessionLap.LaptimeMS / 1000.0 >= thisBest) BestLapByClassCarIdxs[CarClass.Overall] = i;
                    } else {
                        BestLapByClassCarIdxs[(int)CarClass.Overall] = i;
                    }
                }

                if (RealtimeData.IsFocusedChange) {
                    thisCar.GapToAheadInClass = null;
                }
            }

            // If somebody left the session, need to reset following class positions
            var startpos = classPositions[focusedClass];
            if (startpos == -1) {
                startpos = 0;
            }
            for (int i = startpos; i < LeaderboardPlugin.Settings.NumOverallPos; i++) {
                if (PosInClassCarsIdxs[i] == -1) break; // All following must already be -1
                PosInClassCarsIdxs[i] = -1;
            }

            UpdateRelativeOrder();
        }

        private void UpdateRelativeOrder() {
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


            ahead.CopyTo(RelativePosOnTrackCarsIdxs, LeaderboardPlugin.Settings.NumRelativePos - ahead.Count);
            RelativePosOnTrackCarsIdxs[LeaderboardPlugin.Settings.NumRelativePos] = FocusedCarIdx;
            behind.CopyTo(RelativePosOnTrackCarsIdxs, LeaderboardPlugin.Settings.NumRelativePos + 1);

            // Set missing positions to -1
            var startidx = LeaderboardPlugin.Settings.NumRelativePos - ahead.Count;
            var endidx = LeaderboardPlugin.Settings.NumRelativePos + behind.Count + 1;
            for (int i = 0; i < LeaderboardPlugin.Settings.NumRelativePos * 2 + 1; i++) {
                if (i < startidx || i >= endidx) {
                    RelativePosOnTrackCarsIdxs[i] = -1;
                }
            }

        }

        #endregion

        #region EntryListUpdate

        private void OnEntryListUpdate(string sender, CarInfo car) {
            // Add new cars if not already added, update car info of all the cars (adds new drivers if some were missing)
            var idx = Cars.FindIndex(x => x.CarIndex == car.CarIndex);
            if (idx == -1) {
                Cars.Add(new CarData(car, null));
            } else {
                Cars[idx].UpdateCarInfo(car);
            }
        }
        #endregion

        #region RealtimeCarUpdate
        private void OnRealtimeCarUpdate(string sender, RealtimeCarUpdate update) {
            if (RealtimeData == null) return;
            // Update Realtime data of existing cars
            // If found new car, BroadcastClient itself requests new entry list
            var idx = Cars.FindIndex(x => x.CarIndex == update.CarIndex);
            if (idx == -1) return; // Car wasn't found, wait for entry list update
            var car = Cars[idx];
            car.OnRealtimeCarUpdate(update, RealtimeData);
            _lastUpdateCarIds.Add(car.CarIndex);

            //if (car.LapsBySplinePosition == 2 && update.SplinePosition != 1) {
            //    string carClass = car.CarClass.ToString();

            //    var fname = $"{LeaderboardPlugin.Settings.PluginDataLocation}\\laps\\{TrackData.TrackId}_{carClass}.txt";
            //    if (car.FirstAdded) {
            //        File.AppendAllText(fname, $"\n");
            //    }
            //   // File.AppendAllText(fname, $"{update.SplinePosition};{update.CurrentLap.LaptimeMS};{update.Kmh};");
            //    car.FirstAdded = true;

            //}

        }
        #endregion

        #region TrackUpdate
        private void OnTrackDataUpdate(string sender, TrackData update) {
            // Update track data
            //AccBroadcastDataPlugin.LogInfo("New track update.");
            TrackData = update;
            TrackData.ReadDefBestLaps();
            
        }
        #endregion

        #endregion

    }


}