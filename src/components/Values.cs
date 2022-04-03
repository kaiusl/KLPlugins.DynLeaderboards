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
using System.Threading;

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
        public int FocusedCarIdx { get; private set; } = _defaultIdxValue;
        public CarClassArray<int> BestLapByClassCarIdxs { get; private set; } = new CarClassArray<int>(_defaultIdxValue);
        public double MaxDriverStintTime { get; private set; } = -1;
        public double MaxDriverTotalDriveTime { get; private set; } = -1;

        // Store relative spline positions for relative leaderboard,
        // need to store separately as we need to sort by spline pos at the end on update loop
        private List<CarSplinePos> _relativeSplinePositions = new List<CarSplinePos>();
        private CarClassArray<int> _classLeaderIdxs = new CarClassArray<int>(_defaultIdxValue); // Indexes of class leaders in Cars list
        private List<ushort> _lastUpdateCarIds = new List<ushort>();
        private ACCUdpRemoteClientConfig _broadcastConfig;
        private bool _startingPositionsSet = false;
        private const int _defaultIdxValue = -1;

        private readonly object _updadeLock = new object();
        private static Mutex mut = new Mutex();

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
            _classLeaderIdxs.Reset();
            BestLapByClassCarIdxs.Reset();
            _relativeSplinePositions.Clear();
            _startingPositionsSet = false;
            MaxDriverStintTime = -1;
            MaxDriverTotalDriveTime = -1;
            lock (_updadeLock) {
                _entryListUpdateRunning = false;
                _realtimeCarUpdate = false;
                _realtimeUpdate = false;
                _trackUpdate = false;
            }
            
    }

        private void ResetPos() {
            for (int i = 0; i < LeaderboardPlugin.Settings.NumOverallPos; i++) {
                PosInClassCarsIdxs[i] = _defaultIdxValue;
            }

            for (int i = 0; i < LeaderboardPlugin.Settings.NumRelativePos * 2 + 1; i++) {
                RelativePosOnTrackCarsIdxs[i] = _defaultIdxValue;
            }

            _relativeSplinePositions.Clear();
            FocusedCarIdx = _defaultIdxValue;
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

        }

        public CarData GetCar(int i) => Cars.ElementAtOrDefault(i);

        public CarData GetFocusedCar() {
            if (FocusedCarIdx == _defaultIdxValue) return null;
            return Cars[FocusedCarIdx];
        }

        public CarData GetBestLapCar(CarClass cls) {
            var idx = BestLapByClassCarIdxs[cls];
            if (idx == BestLapByClassCarIdxs.DefaultValue) return null;
            return Cars[idx];
        }

        public int? GetBestLapCarIdx(CarClass cls) {
            return BestLapByClassCarIdxs[cls];
        }

        public CarData GetFocusedClassBestLapCar() {
            var focusedClass = GetFocusedCar()?.CarClass;
            if (focusedClass == null) return null;
            var idx = BestLapByClassCarIdxs[(CarClass)focusedClass];
            if (idx == BestLapByClassCarIdxs.DefaultValue) return null;
            return Cars[idx];
        }

        public int? GetFocusedClassBestLapCarIdx() {
            var focusedClass = GetFocusedCar()?.CarClass;
            if (focusedClass == null) return null;
            return BestLapByClassCarIdxs[(CarClass)focusedClass];
        }


        // DBG
        public CarData DbgGetInClassPos(int i) {
            var idx = PosInClassCarsIdxs[i];
            if (idx == _defaultIdxValue) return null;
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

            if (_entryListUpdateRunning || _realtimeCarUpdate || _realtimeUpdate || _trackUpdate) {
                LeaderboardPlugin.LogInfo($"OnRealtimeUpdate while other update was running.");
            }

            _realtimeUpdate = true;

            //var swatch = Stopwatch.StartNew();
            //LeaderboardPlugin.LogInfo($"RealtimeUpdate update. ThreadId={Thread.CurrentThread.ManagedThreadId}");

            if (RealtimeData == null) {
                RealtimeData = new RealtimeData(update);
                _realtimeUpdate = false;
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


            if (Cars.Count == 0) {
                _realtimeUpdate = false;
                return;
            };
            ClearMissingCars();
            SetOverallOrder();
            FocusedCarIdx = Cars.FindIndex(x => x.CarIndex == update.FocusedCarIndex);
            if (FocusedCarIdx != _defaultIdxValue && !RealtimeData.IsNewSession) {
                UpdateCarData();
            }

            //swatch.Stop();
            //TimeSpan ts = swatch.Elapsed;
            //File.AppendAllText($"{LeaderboardPlugin.Settings.PluginDataLocation}\\Logs\\timings\\OnRealtimeUpdate_{LeaderboardPlugin.PluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");

            //LeaderboardPlugin.LogInfo($"Finished RealtimeUpdate update. ThreadId={Thread.CurrentThread.ManagedThreadId}");
            _realtimeUpdate = false;
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
                    if ((a.IsFinished || b.IsFinished) && a.NewData != null && b.NewData != null) {
                        if (a.NewData.Laps != b.NewData.Laps) {
                            return a.NewData.Laps.CompareTo(b.NewData.Laps);
                        } else {
                            // At least one of them must be finished, if both earlier finish time should be ahead.
                            // If one hasn't finished, assign max value so it would be set behind the other one.
                            var aFTime = a.FinishTime == null ? TimeSpan.MaxValue.TotalSeconds : ((TimeSpan)a.FinishTime).TotalSeconds;
                            var bFTime = b.FinishTime == null ? TimeSpan.MaxValue.TotalSeconds : ((TimeSpan)b.FinishTime).TotalSeconds;
                            return aFTime.CompareTo(bFTime);
                        }
                    } else if ((a.TotalSplinePosition == b.TotalSplinePosition) && a.NewData != null && b.NewData != null) {
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
            var classPos = new CarClassArray<int>(_defaultIdxValue);

            for (int i = 0; i < Cars.Count; i++) {
                var thisCar = Cars[i];

                var thisClass = thisCar.CarClass;
                var clsPos = classPos[thisClass];

                if (clsPos != classPos.DefaultValue) {
                    clsPos++;
                } else {
                    // First time seeing this class car, must be the class leader
                    clsPos = 1;
                }

                thisCar.SetStartingPositions(i + 1, clsPos);
                classPos[thisClass] = clsPos;
            }
            _startingPositionsSet = true;
        }

        /// <summary>
        /// Update car related data like positions and gaps
        /// </summary>
        private void UpdateCarData() {
            // Clear old data
            _relativeSplinePositions.Clear();

            var classPositions = new CarClassArray<int>(_defaultIdxValue); 
            var leaderCar = Cars[0];
            var focusedCar = Cars[FocusedCarIdx];
            var focusedClass = focusedCar.CarClass;
            var aheadInClassCarIdxs = new CarClassArray<int>(_defaultIdxValue);

            _classLeaderIdxs.Reset();
            BestLapByClassCarIdxs.Reset();
 
            for (int i = 0; i < Cars.Count; i++) {
                var thisCar = Cars[i];

                var thisClass = thisCar.CarClass;
                var clsPos = classPositions[thisClass];
                if (clsPos != classPositions.DefaultValue) {
                    clsPos++;
                } else {
                    // First time seeing this class car, must be the class leader
                    clsPos = 1;
                    _classLeaderIdxs[thisClass] = i;
                }
                classPositions[thisClass] = clsPos;

                if (thisClass == focusedClass) {
                    PosInClassCarsIdxs[clsPos - 1] = i;
                }

                var carAhead = i != 0 ? Cars[i - 1] : null;
                var carAheadInClassIdx = aheadInClassCarIdxs[thisClass];
                var carAheadInClass = carAheadInClassIdx != aheadInClassCarIdxs.DefaultValue ? Cars[carAheadInClassIdx] : null;

                var relSplinePos = thisCar.CalculateRelativeSplinePosition(focusedCar);
                thisCar.OnRealtimeUpdate(RealtimeData, leaderCar, Cars[_classLeaderIdxs[thisClass]], focusedCar, carAhead, carAheadInClass,  i + 1, clsPos, relSplinePos);
                aheadInClassCarIdxs[thisClass] = i;

                // Since we cannot remove cars after finish, don't add cars that have left to the relative
                if (thisCar.MissedRealtimeUpdates < 10) _relativeSplinePositions.Add(new CarSplinePos(i, relSplinePos));

                // Update best laps
                var thisBest = thisCar.NewData?.BestSessionLap?.Laptime;
                if (thisBest != null) {
                    var thisIdx = BestLapByClassCarIdxs[thisClass];
                    if (thisIdx != BestLapByClassCarIdxs.DefaultValue) {
                        if (Cars[thisIdx].NewData.BestSessionLap.Laptime >= thisBest) BestLapByClassCarIdxs[thisClass] = i;
                    } else {
                        BestLapByClassCarIdxs[thisClass] = i;
                    }

                    var overallIdx = BestLapByClassCarIdxs[CarClass.Overall];
                    if (overallIdx != BestLapByClassCarIdxs.DefaultValue) {
                        if (Cars[overallIdx].NewData.BestSessionLap.Laptime >= thisBest) BestLapByClassCarIdxs[CarClass.Overall] = i;
                    } else {
                        BestLapByClassCarIdxs[(int)CarClass.Overall] = i;
                    }
                }
            }

            // If somebody left the session, need to reset following class positions
            var startpos = classPositions[focusedClass];
            if (startpos == classPositions.DefaultValue) {
                startpos = 0;
            }
            for (int i = startpos; i < LeaderboardPlugin.Settings.NumOverallPos; i++) {
                if (PosInClassCarsIdxs[i] == _defaultIdxValue) break; // All following must already be -1
                PosInClassCarsIdxs[i] = _defaultIdxValue;
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
                    RelativePosOnTrackCarsIdxs[i] = _defaultIdxValue;
                }
            }

        }

        #endregion

        #region EntryListUpdate

        private volatile bool _entryListUpdateRunning = false;
        private volatile bool _realtimeCarUpdate = false;
        private volatile bool _realtimeUpdate = false;
        private volatile bool _trackUpdate = false;
        private void OnEntryListUpdate(string sender, CarInfo car) {
            // Add new cars if not already added, update car info of all the cars (adds new drivers if some were missing)

            if (_entryListUpdateRunning || _realtimeCarUpdate || _realtimeUpdate || _trackUpdate) {
                LeaderboardPlugin.LogInfo($"OnEntryListUpdate while other update was running {_entryListUpdateRunning}, {_realtimeUpdate}, {_realtimeCarUpdate}, {_trackUpdate}");
            }
            _entryListUpdateRunning = true;
            var idx = Cars.FindIndex(x => x.CarIndex == car.CarIndex);
            if (idx == -1) {
                Cars.Add(new CarData(car, null));
            } else {
                Cars[idx].UpdateCarInfo(car);
            }
            _entryListUpdateRunning = false;
        }
        #endregion

        #region RealtimeCarUpdate
        private void OnRealtimeCarUpdate(string sender, RealtimeCarUpdate update) {

            //LeaderboardPlugin.LogInfo($"RealtimeCarUpdate update for car #{update.CarIndex}. ThreadId={Thread.CurrentThread.ManagedThreadId}");
            if (_entryListUpdateRunning || _realtimeCarUpdate || _realtimeUpdate || _trackUpdate) {
                LeaderboardPlugin.LogInfo($"OnRealtimeCarUpdate while other update was running. {_entryListUpdateRunning}, {_realtimeUpdate}, {_realtimeCarUpdate}, {_trackUpdate}");
            }
            _realtimeCarUpdate = true;
            if (RealtimeData == null) {
                _realtimeCarUpdate = false;
                return;
            };
            // Update Realtime data of existing cars
            // If found new car, BroadcastClient itself requests new entry list
            var idx = Cars.FindIndex(x => x.CarIndex == update.CarIndex);
            if (idx == -1) {
                // Car wasn't found, wait for entry list update
                _realtimeCarUpdate = false;
                return;
            };
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
            //LeaderboardPlugin.LogInfo($"Finished RealtimeCarUpdate update for car #{update.CarIndex}. ThreadId={Thread.CurrentThread.ManagedThreadId}");
            _realtimeCarUpdate = false;
            
        }
        #endregion

        #region TrackUpdate
        private void OnTrackDataUpdate(string sender, TrackData update) {
            if (_entryListUpdateRunning || _realtimeCarUpdate || _realtimeUpdate || _trackUpdate) {
                LeaderboardPlugin.LogInfo($"OnTrackDataUpdate while other update was running.");
            }

            _trackUpdate = true;

            //LeaderboardPlugin.LogInfo($"TrackData update. ThreadId={Thread.CurrentThread.ManagedThreadId}");
            // Update track data
            //AccBroadcastDataPlugin.LogInfo("New track update.");
            TrackData = update;
            TrackData.ReadDefBestLaps();
            //LeaderboardPlugin.LogInfo($"Finished TrackData update. ThreadId={Thread.CurrentThread.ManagedThreadId}");
            _trackUpdate = false;
        }
        #endregion

        #endregion

    }


}