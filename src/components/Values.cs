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

        public class DynLeaderboardValues {
            public int[] RelativePosOnTrackCarsIdxs { get; internal set; }

            public int[] RelativePosOverallCarsIdxs { get; internal set; }
            public int[] RelativePosClassCarsIdxs { get; internal set; }

            public int[] PartialRelativeOverallCarsIdxs { get; internal set; }
            public int? FocusedCarPosInPartialRelativeOverallCarsIdxs { get; internal set; }
            public int[] PartialRelativeClassCarsIdxs { get; internal set; }
            public int? FocusedCarPosInPartialRelativeClassCarsIdxs { get; internal set; }

            public delegate CarData GetDynCarDelegate(int i);
            public delegate int? GetFocusedCarIdxInLDynLeaderboardDelegate();
            public delegate double? DynGapToFocusedDelegate(int i);
            public delegate double? DynGapToAheadDelegate(int i);
            public delegate double? DynBestLapDeltaToFocusedBestDelegate(int i);
            public delegate double? DynLastLapDeltaToFocusedBestDelegate(int i);
            public delegate double? DynLastLapDeltaToFocusedLastDelegate(int i);

            public GetDynCarDelegate GetDynCar { get; internal set; }
            public GetFocusedCarIdxInLDynLeaderboardDelegate GetFocusedCarIdxInDynLeaderboard { get; internal set; }
            public DynGapToFocusedDelegate GetDynGapToFocused { get; internal set; }
            public DynGapToAheadDelegate GetDynGapToAhead { get; internal set; }
            public DynBestLapDeltaToFocusedBestDelegate GetDynBestLapDeltaToFocusedBest { get; internal set; }
            public DynLastLapDeltaToFocusedBestDelegate GetDynLastLapDeltaToFocusedBest { get; internal set; }
            public DynLastLapDeltaToFocusedLastDelegate GetDynLastLapDeltaToFocusedLast { get; internal set; }

            public PluginSettings.DynLeaderboardConfig Settings { get; private set; }

            public DynLeaderboardValues(PluginSettings.DynLeaderboardConfig settings) {
                Settings = settings;

                if (DynLeaderboardContainsAny(Leaderboard.RelativeOnTrack))
                    RelativePosOnTrackCarsIdxs = new int[Settings.NumOnTrackRelativePos * 2 + 1];

                if (DynLeaderboardContainsAny(Leaderboard.RelativeOverall))
                    RelativePosOverallCarsIdxs = new int[Settings.NumOverallRelativePos * 2 + 1];

                if (DynLeaderboardContainsAny(Leaderboard.PartialRelativeOverall))
                    PartialRelativeOverallCarsIdxs = new int[Settings.PartialRelativeOverallNumOverallPos + Settings.PartialRelativeOverallNumRelativePos * 2 + 1];

                if (DynLeaderboardContainsAny(Leaderboard.RelativeClass))
                    RelativePosClassCarsIdxs = new int[Settings.NumClassRelativePos * 2 + 1];

                if (DynLeaderboardContainsAny(Leaderboard.PartialRelativeClass))
                    PartialRelativeClassCarsIdxs = new int[Settings.PartialRelativeClassNumClassPos + Settings.PartialRelativeClassNumRelativePos * 2 + 1];
            }

            private bool DynLeaderboardContainsAny(params Leaderboard[] leaderboards) {
                foreach (var v in leaderboards) {
                    if (Settings.Order.Contains(v)) {
                        return true;
                    }
                }
                return false;
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
        public int? FocusedCarPosInClassCarsIdxs { get; private set; }

        public int FocusedCarIdx { get; private set; } = _defaultIdxValue;
        public CarClassArray<int> BestLapByClassCarIdxs { get; private set; } = new CarClassArray<int>(_defaultIdxValue);
        public double MaxDriverStintTime { get; private set; } = -1;
        public double MaxDriverTotalDriveTime { get; private set; } = -1;

        public List<DynLeaderboardValues> LeaderboardValues { get; private set; } = new List<DynLeaderboardValues>();

        // Store relative spline positions for relative leaderboard,
        // need to store separately as we need to sort by spline pos at the end on update loop
        private List<CarSplinePos> _relativeSplinePositions = new List<CarSplinePos>();
        private CarClassArray<int> _classLeaderIdxs = new CarClassArray<int>(_defaultIdxValue); // Indexes of class leaders in Cars list
        private List<ushort> _lastUpdateCarIds = new List<ushort>();
        private ACCUdpRemoteClientConfig _broadcastConfig;
        private bool _startingPositionsSet = false;
        private const int _defaultIdxValue = -1;

        public Values() {
            Cars = new List<CarData>();
            var num = LeaderboardPlugin.Settings.GetMaxNumClassPos();
            if (num > 0) PosInClassCarsIdxs = new int[num];
      
            ResetPos();
            _broadcastConfig = new ACCUdpRemoteClientConfig("127.0.0.1", "KLLeaderboardPlugin", LeaderboardPlugin.Settings.BroadcastDataUpdateRateMs);
            SetDynamicCarGetter();
            foreach (var l in LeaderboardPlugin.Settings.DynLeaderboardConfigs) {
                LeaderboardValues.Add(new DynLeaderboardValues(l));
            }
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
            _startingPositionsSet =  false;
            MaxDriverStintTime = -1;
            MaxDriverTotalDriveTime = -1;           
        }

        private void ResetIdxs(int[] arr) {
            if (arr != null) {
                for (int i = 0; i < arr.Length; i++) {
                    arr[i] = _defaultIdxValue;
                }
            }
        }

        private void ResetPos() {
            ResetIdxs(PosInClassCarsIdxs);
            foreach (var l in LeaderboardValues) {
                ResetIdxs(l.RelativePosClassCarsIdxs);
                ResetIdxs(l.RelativePosOverallCarsIdxs);
                ResetIdxs(l.RelativePosOnTrackCarsIdxs);
                ResetIdxs(l.PartialRelativeClassCarsIdxs);
                ResetIdxs(l.PartialRelativeOverallCarsIdxs);
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

        public void OnDataUpdate(PluginManager pm, GameData data) {}

        public CarData GetCar(int i) => Cars.ElementAtOrDefault(i);

        private CarData GetCar(int i, int[] idxs) {
            if (i > idxs.Length - 1) return null;
            var idx = idxs[i];
            if (idx == -1) return null;
            return Cars[idx];
        }

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

        public void AddNewLeaderboard(PluginSettings.DynLeaderboardConfig s) {
            LeaderboardValues.Add(new DynLeaderboardValues(s));
        }

        public void SetDynamicCarGetter() {
            foreach (var l in LeaderboardValues) {
                switch (l.Settings.CurrentLeaderboard()) {
                    case Leaderboard.Overall:
                        l.GetDynCar = (i) => GetCar(i);
                        l.GetFocusedCarIdxInDynLeaderboard = () => FocusedCarIdx;
                        l.GetDynGapToFocused = (i) => GetCar(i)?.GapToLeader;
                        l.GetDynGapToAhead = (i) => GetCar(i)?.GapToAhead;
                        l.GetDynBestLapDeltaToFocusedBest = (i) => GetCar(i)?.BestLapDeltaToLeaderBest;
                        l.GetDynLastLapDeltaToFocusedBest = (i) => GetCar(i)?.LastLapDeltaToLeaderBest;
                        l.GetDynLastLapDeltaToFocusedLast = (i) => GetCar(i)?.LastLapDeltaToLeaderLast;
                        break;
                    case Leaderboard.Class:
                        l.GetDynCar = (i) => GetCar(i, PosInClassCarsIdxs);
                        l.GetFocusedCarIdxInDynLeaderboard = () => FocusedCarPosInClassCarsIdxs;
                        l.GetDynGapToFocused = (i) => l.GetDynCar(i)?.GapToClassLeader;
                        l.GetDynGapToAhead = (i) => l.GetDynCar(i)?.GapToAheadInClass;
                        l.GetDynBestLapDeltaToFocusedBest = (i) => l.GetDynCar(i)?.BestLapDeltaToClassLeaderBest;
                        l.GetDynLastLapDeltaToFocusedBest = (i) => l.GetDynCar(i)?.LastLapDeltaToClassLeaderBest;
                        l.GetDynLastLapDeltaToFocusedLast = (i) => l.GetDynCar(i)?.LastLapDeltaToClassLeaderLast;
                        break;
                    case Leaderboard.RelativeOverall:
                        l.GetDynCar = (i) => GetCar(i, l.RelativePosOverallCarsIdxs);
                        l.GetFocusedCarIdxInDynLeaderboard = () => l.Settings.NumOverallRelativePos;
                        l.GetDynGapToFocused = (i) => l.GetDynCar(i)?.GapToFocusedTotal;
                        l.GetDynGapToAhead = (i) => l.GetDynCar(i)?.GapToAhead;
                        l.GetDynBestLapDeltaToFocusedBest = (i) => l.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                        l.GetDynLastLapDeltaToFocusedBest = (i) => l.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                        l.GetDynLastLapDeltaToFocusedLast = (i) => l.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                        break;
                    case Leaderboard.RelativeClass:
                        l.GetDynCar = (i) => GetCar(i, l.RelativePosClassCarsIdxs);
                        l.GetFocusedCarIdxInDynLeaderboard = () => l.Settings.NumClassRelativePos;
                        l.GetDynGapToFocused = (i) => l.GetDynCar(i)?.GapToFocusedTotal;
                        l.GetDynGapToAhead = (i) => l.GetDynCar(i)?.GapToAheadInClass;
                        l.GetDynBestLapDeltaToFocusedBest = (i) => l.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                        l.GetDynLastLapDeltaToFocusedBest = (i) => l.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                        l.GetDynLastLapDeltaToFocusedLast = (i) => l.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                        break;
                    case Leaderboard.PartialRelativeOverall:
                        l.GetDynCar = (i) => GetCar(i, l.PartialRelativeOverallCarsIdxs);
                        l.GetFocusedCarIdxInDynLeaderboard = () => l.FocusedCarPosInPartialRelativeOverallCarsIdxs;
                        l.GetDynGapToFocused = (i) => l.GetDynCar(i)?.GapToFocusedTotal;
                        l.GetDynGapToAhead = (i) => l.GetDynCar(i)?.GapToAhead;
                        l.GetDynBestLapDeltaToFocusedBest = (i) => l.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                        l.GetDynLastLapDeltaToFocusedBest = (i) => l.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                        l.GetDynLastLapDeltaToFocusedLast = (i) => l.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                        break;
                    case Leaderboard.PartialRelativeClass:
                        l.GetDynCar = (i) => GetCar(i, l.PartialRelativeClassCarsIdxs);
                        l.GetFocusedCarIdxInDynLeaderboard = () => l.FocusedCarPosInPartialRelativeClassCarsIdxs;
                        l.GetDynGapToFocused = (i) => l.GetDynCar(i)?.GapToFocusedTotal;
                        l.GetDynGapToAhead = (i) => l.GetDynCar(i)?.GapToAheadInClass;
                        l.GetDynBestLapDeltaToFocusedBest = (i) => l.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                        l.GetDynLastLapDeltaToFocusedBest = (i) => l.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                        l.GetDynLastLapDeltaToFocusedLast = (i) => l.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                        break;
                    case Leaderboard.RelativeOnTrack:
                        l.GetDynCar = (i) => GetCar(i, l.RelativePosOnTrackCarsIdxs);
                        l.GetFocusedCarIdxInDynLeaderboard = () => l.Settings.NumOnTrackRelativePos;
                        l.GetDynGapToFocused = (i) => l.GetDynCar(i)?.GapToFocusedOnTrack;
                        l.GetDynGapToAhead = (i) => l.GetDynCar(i)?.GapToAheadOnTrack;
                        l.GetDynBestLapDeltaToFocusedBest = (i) => l.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                        l.GetDynLastLapDeltaToFocusedBest = (i) => l.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                        l.GetDynLastLapDeltaToFocusedLast = (i) => l.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                        break;
                    default:
                        l.GetDynCar = (i) => null;
                        l.GetFocusedCarIdxInDynLeaderboard = () => null;
                        l.GetDynGapToFocused = (i) => double.NaN;
                        l.GetDynGapToAhead = (i) => double.NaN;
                        l.GetDynBestLapDeltaToFocusedBest = (i) => double.NaN;
                        l.GetDynLastLapDeltaToFocusedBest = (i) => double.NaN;
                        l.GetDynLastLapDeltaToFocusedLast = (i) => double.NaN;
                        break;
                }
            }
        }

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
            //var swatch = Stopwatch.StartNew();
            //LeaderboardPlugin.LogInfo($"RealtimeUpdate update. ThreadId={Thread.CurrentThread.ManagedThreadId}");

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


            if (Cars.Count == 0) {
                return;
            };
            ClearMissingCars();
            SetOverallOrder();
            FocusedCarIdx = Cars.FindIndex(x => x.CarIndex == update.FocusedCarIndex);
            SetOverallRelativeOrder();
            SetClassRelativeOrder();
            if (FocusedCarIdx != _defaultIdxValue && !RealtimeData.IsNewSession) {
                UpdateCarData();
            }

            //swatch.Stop();
            //TimeSpan ts = swatch.Elapsed;
            //File.AppendAllText($"{LeaderboardPlugin.Settings.PluginDataLocation}\\Logs\\timings\\OnRealtimeUpdate_{LeaderboardPlugin.PluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");
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

        private void SetOverallRelativeOrder() {
            foreach (var l in LeaderboardValues) {
                if (l.RelativePosOverallCarsIdxs != null) {
                    for (int i = 0; i < l.Settings.NumOverallRelativePos*2+1; i++) {
                        var idx = FocusedCarIdx - l.Settings.NumOverallRelativePos + i;
                        l.RelativePosOverallCarsIdxs[i] = idx < Cars.Count && idx >= 0 ? idx : _defaultIdxValue;
                    }
                }

                if (l.PartialRelativeOverallCarsIdxs != null) {
                    l.FocusedCarPosInPartialRelativeOverallCarsIdxs = null;
                    for (int i = 0; i < l.Settings.PartialRelativeOverallNumOverallPos + l.Settings.PartialRelativeOverallNumRelativePos*2+1; i++) {
                        var idx = i;
                        if (i > l.Settings.PartialRelativeOverallNumOverallPos - 1 && FocusedCarIdx > l.Settings.PartialRelativeOverallNumOverallPos + l.Settings.PartialRelativeOverallNumRelativePos) {
                            idx += FocusedCarIdx - l.Settings.PartialRelativeOverallNumOverallPos - l.Settings.PartialRelativeOverallNumRelativePos;
                        }
                        l.PartialRelativeOverallCarsIdxs[i] = idx < Cars.Count ? idx : _defaultIdxValue;
                        if (idx == FocusedCarIdx) {
                            l.FocusedCarPosInPartialRelativeOverallCarsIdxs = i;
                        }
                    }
                }
            }
        }

        private void SetClassRelativeOrder() {
            foreach (var l in LeaderboardValues) {
                if (l.RelativePosClassCarsIdxs != null) {
                    for (int i = 0; i < l.Settings.NumClassRelativePos*2+1; i++) {
                        var idx = Cars[FocusedCarIdx].InClassPos - l.Settings.NumClassRelativePos + i;
                        idx = PosInClassCarsIdxs.ElementAtOrDefault(idx);
                        l.RelativePosClassCarsIdxs[i] = idx < Cars.Count && idx >= 0 ? idx : _defaultIdxValue;
                    }
                }

                if (l.PartialRelativeClassCarsIdxs != null) {
                    var overallPos = l.Settings.PartialRelativeClassNumClassPos;
                    var relPos = l.Settings.PartialRelativeClassNumRelativePos;

                    for (int i = 0; i < overallPos + relPos*2+1; i++) {
                        var idx = i;
                        var focusedClassPos = Cars[FocusedCarIdx].InClassPos;
                        if (i > overallPos - 1 && focusedClassPos > overallPos + relPos) {
                            idx += focusedClassPos - overallPos - relPos;
                        }
                        idx = PosInClassCarsIdxs.ElementAtOrDefault(idx);
                        l.PartialRelativeClassCarsIdxs[i] = idx < Cars.Count && idx >= 0 ? idx : _defaultIdxValue;
                        if (idx == FocusedCarIdx) {
                            l.FocusedCarPosInPartialRelativeClassCarsIdxs = i;
                        }
                    }
                }
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
            _classLeaderIdxs.Reset();

            var classPositions = new CarClassArray<int>(_defaultIdxValue); 
            var leaderCar = Cars[0];
            var focusedCar = Cars[FocusedCarIdx];
            var focusedClass = focusedCar.CarClass;
            var aheadInClassCarIdxs = new CarClassArray<int>(_defaultIdxValue);

            UpdateBestLapIdxs(focusedCar);
            UpdateRelativeOrder();

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

                if (PosInClassCarsIdxs != null && thisClass == focusedClass) {
                    PosInClassCarsIdxs[clsPos - 1] = i;
                    if (i == FocusedCarIdx) {
                        FocusedCarPosInClassCarsIdxs = clsPos - 1;
                    }
                }

                var carAhead = i != 0 ? Cars[i - 1] : null;
                var carAheadInClassIdx = aheadInClassCarIdxs[thisClass];
                var carAheadInClass = carAheadInClassIdx != aheadInClassCarIdxs.DefaultValue ? Cars[carAheadInClassIdx] : null;

                var relSplinePos = thisCar.CalculateRelativeSplinePosition(focusedCar);
                var overallBestLapCarIdx = BestLapByClassCarIdxs[CarClass.Overall];
                var classBestLapCarIdx = BestLapByClassCarIdxs[thisClass];
                thisCar.OnRealtimeUpdate(
                    realtimeData: RealtimeData, 
                    leaderCar: leaderCar, 
                    classLeaderCar: Cars[_classLeaderIdxs[thisClass]], 
                    focusedCar: focusedCar, 
                    carAhead: carAhead, 
                    carAheadInClass: carAheadInClass, 
                    carAheadOnTrack: GetCarAheadOnTrack(thisCar),
                    overallBestLapCar: overallBestLapCarIdx != BestLapByClassCarIdxs.DefaultValue ?  Cars[overallBestLapCarIdx] : null,
                    classBestLapCar: classBestLapCarIdx != BestLapByClassCarIdxs.DefaultValue ? Cars[classBestLapCarIdx] : null,
                    overallPos: i + 1, 
                    classPos: clsPos,
                    relSplinePos: relSplinePos
                    );
                aheadInClassCarIdxs[thisClass] = i;


            }

            // If somebody left the session, need to reset following class positions
            if (PosInClassCarsIdxs != null) {
                var startpos = classPositions[focusedClass];
                if (startpos == classPositions.DefaultValue) {
                    startpos = 0;
                }
                for (int i = startpos; i < LeaderboardPlugin.Settings.GetMaxNumClassPos(); i++) {
                    if (PosInClassCarsIdxs[i] == _defaultIdxValue) break; // All following must already be -1
                    PosInClassCarsIdxs[i] = _defaultIdxValue;
                }
            }
        }

        private CarData GetCarAheadOnTrack(CarData c) {
            CarData closestCar = null;
            double relsplinepos = double.MaxValue;
            foreach (var car in Cars) {
                var pos = c.CalculateRelativeSplinePosition(car);
                if (pos > 0 && pos < relsplinepos) {
                    closestCar = car;
                    relsplinepos = pos;
                }
            }
            return closestCar;
        }

        private void UpdateBestLapIdxs(CarData focusedCar) {
            BestLapByClassCarIdxs.Reset();
            // We need to update best lap idxs first as we need to pass best lap cars on,
            // and we cannot do it if BestLapByClassCarIdxs contains false indices as some cars may have left.
            for (int i = 0; i < Cars.Count; i++) {
                var thisCar = Cars[i];
                var thisClass = thisCar.CarClass;
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

                var relSplinePos = thisCar.CalculateRelativeSplinePosition(focusedCar);
                // Since we cannot remove cars after finish, don't add cars that have left to the relative
                if (thisCar.MissedRealtimeUpdates < 10) _relativeSplinePositions.Add(new CarSplinePos(i, relSplinePos));
            }
        }

        private void UpdateRelativeOrder() {
            _relativeSplinePositions.Sort((a, b) => a.SplinePos.CompareTo(b.SplinePos));

            foreach (var l in LeaderboardValues) {
                var relPos = l.Settings.NumOnTrackRelativePos;
                var ahead = _relativeSplinePositions
                    .Where(x => x.SplinePos > 0)
                    .Take(relPos)
                    .Reverse()
                    .ToList()
                    .ConvertAll(x => x.CarIdx);
                var behind = _relativeSplinePositions
                    .Where(x => x.SplinePos < 0)
                    .Reverse()
                    .Take(relPos)
                    .ToList()
                    .ConvertAll(x => x.CarIdx);


                ahead.CopyTo(l.RelativePosOnTrackCarsIdxs, relPos - ahead.Count);
                l.RelativePosOnTrackCarsIdxs[relPos] = FocusedCarIdx;
                behind.CopyTo(l.RelativePosOnTrackCarsIdxs, relPos + 1);

                // Set missing positions to -1
                var startidx = relPos - ahead.Count;
                var endidx = relPos + behind.Count + 1;
                for (int i = 0; i < relPos * 2 + 1; i++) {
                    if (i < startidx || i >= endidx) {
                        l.RelativePosOnTrackCarsIdxs[i] = _defaultIdxValue;
                    }
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
            if (RealtimeData == null) {
                return;
            };
            // Update Realtime data of existing cars
            // If found new car, BroadcastClient itself requests new entry list
            var idx = Cars.FindIndex(x => x.CarIndex == update.CarIndex);
            if (idx == -1) {
                // Car wasn't found, wait for entry list update
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
            
        }
        #endregion

        #region TrackUpdate
        private void OnTrackDataUpdate(string sender, TrackData update) {
           
            //LeaderboardPlugin.LogInfo($"TrackData update. ThreadId={Thread.CurrentThread.ManagedThreadId}");
            // Update track data
            //AccBroadcastDataPlugin.LogInfo("New track update.");
            TrackData = update;
            TrackData.ReadDefBestLaps();
            //LeaderboardPlugin.LogInfo($"Finished TrackData update. ThreadId={Thread.CurrentThread.ManagedThreadId}");
        }
        #endregion

        #endregion

    }


}