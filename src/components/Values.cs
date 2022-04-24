using GameReaderCommon;
using KLPlugins.DynLeaderboards.Enums;
using KLPlugins.DynLeaderboards.ksBroadcastingNetwork;
using KLPlugins.DynLeaderboards.ksBroadcastingNetwork.Structs;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace KLPlugins.DynLeaderboards {
    /// <summary>
    /// Storage and calculation of new properties
    /// </summary>
    public class Values : IDisposable {
        private class CarSplinePos {
            // Index into Cars array
            public int CarIdx = -1;
            // Corresponding splinePosition
            public float SplinePos = 0;

            public CarSplinePos(int idx, float pos) {
                CarIdx = idx;
                SplinePos = pos;
            }
        }

        /// <summary>
        /// Contains values for specific dynamic leaderboard.
        /// 
        /// To access data you need to invoke provided delegates.
        /// </summary>
        public class DynLeaderboardValues {
            public string Name => Settings.Name;
            public Leaderboard CurrentLeaderboard => Settings.CurrentLeaderboard();

            public delegate CarData DynCarDelegate(int i);
            public delegate int? FocusedCarIdxInLDynLeaderboardDelegate();
            public delegate double? DynGapToFocusedDelegate(int i);
            public delegate double? DynGapToAheadDelegate(int i);
            public delegate double? DynBestLapDeltaToFocusedBestDelegate(int i);
            public delegate double? DynLastLapDeltaToFocusedBestDelegate(int i);
            public delegate double? DynLastLapDeltaToFocusedLastDelegate(int i);

            /// <summary>
            /// Get car at position `i` in currently selected dynamic leaderboard.
            /// </summary>
            public DynCarDelegate GetDynCar { get; internal set; }
            /// <summary>
            /// Get the position at which the focused car is in the currently selected dynamic leaderboard.
            /// </summary>
            public FocusedCarIdxInLDynLeaderboardDelegate GetFocusedCarIdxInDynLeaderboard { get; internal set; }
            /// <summary>
            /// Get the gap of the car at position `i` in currently selected dynamic leaderboard to the focused car.
            /// </summary>
            public DynGapToFocusedDelegate GetDynGapToFocused { get; internal set; }
            /// <summary>
            /// Get the gap of the car at position `i` in currently selected dynamic leaderboard to the car ahead in currently selected leaderboard.
            /// </summary>
            public DynGapToAheadDelegate GetDynGapToAhead { get; internal set; }
            /// <summary>
            /// Get the delta of the best lap of the car at position `i` in currently selected dynamic leaderboard to the focused car's best lap.
            /// </summary>
            public DynBestLapDeltaToFocusedBestDelegate GetDynBestLapDeltaToFocusedBest { get; internal set; }
            /// <summary>
            /// Get the delta of the last lap of the car at position `i` in currently selected dynamic leaderboard to the focused car's best lap.
            /// </summary>
            public DynLastLapDeltaToFocusedBestDelegate GetDynLastLapDeltaToFocusedBest { get; internal set; }
            /// <summary>
            /// Get the delta of the last lap of the car at position `i` in currently selected dynamic leaderboard to the focused car's last lap.
            /// </summary>
            public DynLastLapDeltaToFocusedLastDelegate GetDynLastLapDeltaToFocusedLast { get; internal set; }

            internal int?[] RelativePosOnTrackCarsIdxs { get; }
            internal int?[] RelativeOverallCarsIdxs { get; }
            internal int?[] RelativeClassCarsIdxs { get; }
            internal int?[] PartialRelativeOverallCarsIdxs { get; }
            internal int? FocusedCarPosInPartialRelativeOverallCarsIdxs { get; set; }
            internal int?[] PartialRelativeClassCarsIdxs { get; }
            internal int? FocusedCarPosInPartialRelativeClassCarsIdxs { get; set; }

            internal DynLeaderboardConfig Settings { get; private set; }

            internal DynLeaderboardValues(DynLeaderboardConfig settings) {
                Settings = settings;

                if (DynLeaderboardContainsAny(Leaderboard.RelativeOnTrack))
                    RelativePosOnTrackCarsIdxs = new int?[Settings.NumItems.OnTrackRelativePos * 2 + 1];

                if (DynLeaderboardContainsAny(Leaderboard.RelativeOverall))
                    RelativeOverallCarsIdxs = new int?[Settings.NumItems.OverallRelativePos * 2 + 1];

                if (DynLeaderboardContainsAny(Leaderboard.PartialRelativeOverall))
                    PartialRelativeOverallCarsIdxs = new int?[Settings.NumItems.PartialRelativeOverall_OverallPos + Settings.NumItems.PartialRelativeOverall_RelativePos * 2 + 1];

                if (DynLeaderboardContainsAny(Leaderboard.RelativeClass))
                    RelativeClassCarsIdxs = new int?[Settings.NumItems.ClassRelativePos * 2 + 1];

                if (DynLeaderboardContainsAny(Leaderboard.PartialRelativeClass))
                    PartialRelativeClassCarsIdxs = new int?[Settings.NumItems.PartialRelativeClass_ClassPos + Settings.NumItems.PartialRelativeClass_RelativePos * 2 + 1];
            }

            private bool DynLeaderboardContainsAny(params Leaderboard[] leaderboards) {
                foreach (var v in leaderboards) {
                    if (Settings.Order.Contains(v)) {
                        return true;
                    }
                }
                return false;
            }

            public void NextLeaderboard(Values values) {
                if (Settings.CurrentLeaderboardIdx == Settings.Order.Count - 1) {
                    Settings.CurrentLeaderboardIdx = 0;
                } else {
                    Settings.CurrentLeaderboardIdx++;
                }
                values.SetDynamicCarGetters(this);
            }

            public void PreviousLeaderboard(Values values) {
                if (Settings.CurrentLeaderboardIdx == 0) {
                    Settings.CurrentLeaderboardIdx = Settings.Order.Count - 1;
                } else {
                    Settings.CurrentLeaderboardIdx--;
                }
                values.SetDynamicCarGetters(this);
            }
        }


        public RealtimeData RealtimeData { get; private set; }
        public static TrackData TrackData { get; private set; }
        /// <summary>
        /// Get list of values specific to each dynamic leaderboards. 
        /// Through the items you can access cars in order of currently selected leaderboard.
        /// </summary>
        public IReadOnlyList<DynLeaderboardValues> GetDynLeaderboards { get => DynLeaderboardsValues.AsReadOnly(); }
        /// <summary>
        /// Get values for dynamic leaderboards by the index.
        /// </summary>
        public DynLeaderboardValues GetDynLeaderboard(int i) => DynLeaderboardsValues.ElementAtOrDefault(i);
        /// <summary>
        /// Get values for dynamic leaderboards by it's name.
        /// </summary>
        public DynLeaderboardValues GetDynLeaderboard(string name) => DynLeaderboardsValues.Find(x => x.Settings.Name == name);


        internal ACCUdpRemoteClient BroadcastClient { get; private set; }
        // Idea with cars is to store one copy of data
        // We keep cars array sorted in overall position order
        // Other orderings are stored in different array containing indices into Cars list
        internal List<CarData> Cars;
        internal int?[] PosInClassCarsIdxs { get; private set; }
        internal int? FocusedCarPosInClassCarsIdxs { get; private set; }
        internal int? FocusedCarIdx { get; private set; } = null;
        internal CarClassArray<int?> BestLapByClassCarIdxs { get; private set; } = new CarClassArray<int?>(null);
        internal double MaxDriverStintTime { get; private set; } = -1;
        internal double MaxDriverTotalDriveTime { get; private set; } = -1;
        internal List<DynLeaderboardValues> DynLeaderboardsValues { get; private set; } = new List<DynLeaderboardValues>();

        // Store relative spline positions for relative leaderboard,
        // need to store separately as we need to sort by spline pos at the end on update loop
        private List<CarSplinePos> _relativeSplinePositions = new List<CarSplinePos>();
        private CarClassArray<int?> _classLeaderIdxs = new CarClassArray<int?>(null); // Indexes of class leaders in Cars list
        private List<ushort> _lastUpdateCarIds = new List<ushort>();
        private ACCUdpRemoteClientConfig _broadcastConfig;
        private bool _startingPositionsSet = false;

        internal Values() {
            Cars = new List<CarData>();
            var num = DynLeaderboardsPlugin.Settings.GetMaxNumClassPos();
            if (num > 0) PosInClassCarsIdxs = new int?[100];

            ResetPos();
            _broadcastConfig = new ACCUdpRemoteClientConfig("127.0.0.1", "KLLeaderboardPlugin", DynLeaderboardsPlugin.Settings.BroadcastDataUpdateRateMs);
            foreach (var l in DynLeaderboardsPlugin.Settings.DynLeaderboardConfigs) {
                DynLeaderboardsValues.Add(new DynLeaderboardValues(l));
            }
            SetAllDynamicGetters();
        }

        internal void Reset() {
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
            outdata.Clear();
            bestLaps.Clear();

        }

        private void ResetIdxs(int?[] arr) {
            if (arr != null) {
                for (int i = 0; i < arr.Length; i++) {
                    arr[i] = null;
                }
            }
        }

        private void ResetPos() {
            ResetIdxs(PosInClassCarsIdxs);
            foreach (var l in DynLeaderboardsValues) {
                ResetIdxs(l.RelativeClassCarsIdxs);
                ResetIdxs(l.RelativeOverallCarsIdxs);
                ResetIdxs(l.RelativePosOnTrackCarsIdxs);
                ResetIdxs(l.PartialRelativeClassCarsIdxs);
                ResetIdxs(l.PartialRelativeOverallCarsIdxs);
            }

            _relativeSplinePositions.Clear();
            FocusedCarIdx = null;
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
                    DynLeaderboardsPlugin.LogInfo("Disposed");
                    DisposeBroadcastClient();
                }

                isDisposed = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }
        #endregion


        internal void OnGameStateChanged(bool running, PluginManager manager) {
            if (running) {
                if (BroadcastClient != null) {
                    DynLeaderboardsPlugin.LogWarn("Broadcast client wasn't 'null' at start of new event. Shouldn't be possible, there is a bug in disposing of Broadcast client from previous session.");
                    DisposeBroadcastClient();
                }
                ConnectToBroadcastClient();
            } else {
                Reset();
            }

        }

        internal void OnDataUpdate(PluginManager pm, GameData data) { }

        /// <returns>
        /// `null` if index is out of bounds.
        /// </returns>
        public CarData GetCar(int i) => Cars.ElementAtOrDefault(i);

        /// <returns>
        /// `null` if focused car is not yet set.
        /// </returns>
        public CarData GetFocusedCar() {
            if (FocusedCarIdx == null) return null;
            return Cars[(int)FocusedCarIdx];
        }

        /// <summary>
        /// Use `cls = CarClass.Overll` to get overall best lap.
        /// </summary>
        /// <returns>
        /// `null` if best lap is not found.
        /// </returns>
        public CarData GetBestLapCar(CarClass cls) {
            var idx = BestLapByClassCarIdxs[cls];
            if (idx == null) return null;
            return Cars[(int)idx];
        }

        /// <returns>
        /// `null` if best lap is not found or focused car is not yet set.
        /// </returns>
        public CarData GetFocusedClassBestLapCar() {
            var focusedClass = GetFocusedCar()?.CarClass;
            if (focusedClass == null) return null;
            return GetBestLapCar((CarClass)focusedClass);
        }


        private CarData GetCar(int i, int?[] idxs) {
            if (i > idxs.Length - 1) return null;
            var idx = idxs[i];
            if (idx == null) return null;
            return Cars[(int)idx];
        }

        internal void SetAllDynamicGetters() {
            foreach (var l in DynLeaderboardsValues) {
                SetDynamicCarGetters(l);
            }
        }

        internal void SetDynamicCarGetters(DynLeaderboardValues l) {
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
                    l.GetDynCar = (i) => GetCar(i, l.RelativeOverallCarsIdxs);
                    l.GetFocusedCarIdxInDynLeaderboard = () => l.Settings.NumItems.OverallRelativePos;
                    l.GetDynGapToFocused = (i) => l.GetDynCar(i)?.GapToFocusedTotal;
                    l.GetDynGapToAhead = (i) => l.GetDynCar(i)?.GapToAhead;
                    l.GetDynBestLapDeltaToFocusedBest = (i) => l.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                    l.GetDynLastLapDeltaToFocusedBest = (i) => l.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                    l.GetDynLastLapDeltaToFocusedLast = (i) => l.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                    break;
                case Leaderboard.RelativeClass:
                    l.GetDynCar = (i) => GetCar(i, l.RelativeClassCarsIdxs);
                    l.GetFocusedCarIdxInDynLeaderboard = () => l.Settings.NumItems.ClassRelativePos;
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
                    l.GetFocusedCarIdxInDynLeaderboard = () => l.Settings.NumItems.OnTrackRelativePos;
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



        internal void AddNewLeaderboard(DynLeaderboardConfig s) {
            DynLeaderboardsValues.Add(new DynLeaderboardValues(s));
            SetAllDynamicGetters();
        }

        #region Broadcast client connection

        internal void ConnectToBroadcastClient() {
            BroadcastClient = new ACCUdpRemoteClient(_broadcastConfig);
            BroadcastClient.MessageHandler.OnEntrylistUpdate += OnEntryListUpdate;
            BroadcastClient.MessageHandler.OnRealtimeCarUpdate += OnRealtimeCarUpdate;
            BroadcastClient.MessageHandler.OnRealtimeUpdate += OnBroadcastRealtimeUpdate;
            BroadcastClient.MessageHandler.OnTrackDataUpdate += OnTrackDataUpdate;
        }

        internal async void DisposeBroadcastClient() {
            if (BroadcastClient != null) {
                await BroadcastClient.ShutdownAsync();
                BroadcastClient.Dispose();
                BroadcastClient = null;
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

        #region RealtimeUpdate


        internal double RealtimeUpdateTime = 0;
        private void OnBroadcastRealtimeUpdate(string sender, RealtimeUpdate update) {
            var swatch = Stopwatch.StartNew();
            //LeaderboardPlugin.LogInfo($"RealtimeUpdate update. ThreadId={Thread.CurrentThread.ManagedThreadId}");

            if (RealtimeData == null) {
                RealtimeData = new RealtimeData(update);
                return;
            } else {
                RealtimeData.OnRealtimeUpdate(update);
            }

            if (RealtimeData.IsNewSession) {
                OnNewSession();
            }

            if (RealtimeData.IsRace && RealtimeData.IsPreSession && MaxDriverStintTime == -1) {
                SetMaxStintTimes();
            }

            if (Cars.Count == 0) { return; };
            ClearMissingCars();
            SetOverallOrder();
            FocusedCarIdx = Cars.FindIndex(x => x.CarIndex == update.FocusedCarIndex);
            if (FocusedCarIdx != null && !RealtimeData.IsNewSession) {
                SetRelativeOrders();
                UpdateCarData();
            }

            swatch.Stop();
            TimeSpan ts = swatch.Elapsed;
            RealtimeUpdateTime = ts.TotalMilliseconds;
            //File.AppendAllText($"{LeaderboardPlugin.Settings.PluginDataLocation}\\Logs\\timings\\OnRealtimeUpdate_{LeaderboardPlugin.PluginStartTime}.txt", $"{ts.TotalMilliseconds}\n");
        }

        private void OnNewSession() {
            // Clear all data at the beginning of session
            // Technically we only need clear parts of the data, but this is simpler
            DynLeaderboardsPlugin.LogInfo("New session.");
            Cars.Clear();
            BroadcastClient.MessageHandler.RequestEntryList();
            outdata.Clear();
            bestLaps.Clear();
            ResetPos();
            _lastUpdateCarIds.Clear();
            _relativeSplinePositions.Clear();
            _startingPositionsSet = false;
        }

        private void SetMaxStintTimes() {
            MaxDriverStintTime = (int)DynLeaderboardsPlugin.PManager.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.Graphics.DriverStintTimeLeft") / 1000.0;
            MaxDriverTotalDriveTime = (int)DynLeaderboardsPlugin.PManager.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.Graphics.DriverStintTotalTimeLeft") / 1000.0;
            if (MaxDriverTotalDriveTime == 65535) { // This is max value, which means that the limit doesn't exist
                MaxDriverTotalDriveTime = -1;
            }
        }

        private void ClearMissingCars() {
            // Idea here is that realtime updates come as repeating loop of
            // * Realtime update
            // * RealtimeCarUpdate for each car
            // Thus if we keep track of cars seen in the last loop, we can remove cars that have left the session
            // However as we receive data as UDP packets, there is a possibility that some packets go missing
            // Then we could possibly remove cars that are actually still in session
            // Thus we keep track of how many times in order each car hasn't received the update
            // If it's larger than some number, we remove the car
            if (_lastUpdateCarIds.Count != 0) {
                foreach (var car in Cars) {
                    if (!_lastUpdateCarIds.Contains(car.CarIndex)) {
                        car.MissedRealtimeUpdates++;
                    } else {
                        car.MissedRealtimeUpdates = 0;
                    }
                }

                // Also don't remove cars that have finished as we want to freeze the results after finish
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
                    if (a == b) return 0;
                    if ((a.IsFinished || b.IsFinished) && a.NewData != null && b.NewData != null) {
                        if (a.NewData.Laps != b.NewData.Laps) {
                            return b.NewData.Laps.CompareTo(a.NewData.Laps);
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
                    if (a == b) return 0;
                    var apos = a?.NewData?.Position ?? 1000;
                    var bpos = b?.NewData?.Position ?? 1000;
                    return apos.CompareTo(bpos);
                }

                Cars.Sort(cmp);
            }
        }

        private void SetRelativeOrders() {
            Debug.Assert(FocusedCarIdx != null, "FocusedCarIdx == null, but cannot be");

            foreach (var l in DynLeaderboardsValues) {
                switch (l.Settings.CurrentLeaderboard()) {
                    case Leaderboard.RelativeOverall:
                        SetRelativeOverallOrder(l);
                        break;
                    case Leaderboard.PartialRelativeOverall:
                        SetPartialRelativeOverallOrder(l);
                        break;
                    case Leaderboard.RelativeClass:
                        SetRelativeClassOrder(l);
                        break;
                    case Leaderboard.PartialRelativeClass:
                        SetPartialRelativeClassOrder(l);
                        break;
                    default:
                        break;
                }
            }
        }

        private void SetRelativeOverallOrder(DynLeaderboardValues l) {
            Debug.Assert(l.RelativeOverallCarsIdxs != null);

            var relPos = l.Settings.NumItems.OverallRelativePos;
            for (int i = 0; i < relPos * 2 + 1; i++) {
                var idx = FocusedCarIdx - relPos + i;
                l.RelativeOverallCarsIdxs[i] = idx < Cars.Count && idx >= 0 ? idx : null;
            }
        }

        private void SetPartialRelativeOverallOrder(DynLeaderboardValues l) {
            Debug.Assert(l.PartialRelativeOverallCarsIdxs != null);

            var overallPos = l.Settings.NumItems.PartialRelativeOverall_OverallPos;
            var relPos = l.Settings.NumItems.PartialRelativeOverall_RelativePos;

            l.FocusedCarPosInPartialRelativeOverallCarsIdxs = null;
            for (int i = 0; i < overallPos + relPos * 2 + 1; i++) {
                int? idx = i;
                if (i > overallPos - 1 && FocusedCarIdx > overallPos + relPos) {
                    idx += FocusedCarIdx - overallPos - relPos;
                }
                l.PartialRelativeOverallCarsIdxs[i] = idx < Cars.Count ? idx : null;
                if (idx == FocusedCarIdx) {
                    l.FocusedCarPosInPartialRelativeOverallCarsIdxs = i;
                }
            }
        }

        private void SetRelativeClassOrder(DynLeaderboardValues l) {
            Debug.Assert(l.RelativeClassCarsIdxs != null);

            for (int i = 0; i < l.Settings.NumItems.ClassRelativePos * 2 + 1; i++) {
                int? idx = Cars[(int)FocusedCarIdx].InClassPos - l.Settings.NumItems.ClassRelativePos + i - 1;
                idx = PosInClassCarsIdxs.ElementAtOrDefault((int)idx);
                l.RelativeClassCarsIdxs[i] = idx != null && idx < Cars.Count && idx >= 0 ? idx : null;
            }
        }

        private void SetPartialRelativeClassOrder(DynLeaderboardValues l) {
            Debug.Assert(l.PartialRelativeClassCarsIdxs != null);

            var overallPos = l.Settings.NumItems.PartialRelativeClass_ClassPos;
            var relPos = l.Settings.NumItems.PartialRelativeClass_RelativePos;

            for (int i = 0; i < overallPos + relPos * 2 + 1; i++) {
                int? idx = i;
                var focusedClassPos = Cars[(int)FocusedCarIdx].InClassPos;
                if (i > overallPos - 1 && focusedClassPos > overallPos + relPos) {
                    idx += focusedClassPos - overallPos - relPos;
                }
                idx = PosInClassCarsIdxs.ElementAtOrDefault((int)idx);
                l.PartialRelativeClassCarsIdxs[i] = idx != null && idx < Cars.Count && idx >= 0 ? idx : null;
                if (idx == FocusedCarIdx) {
                    l.FocusedCarPosInPartialRelativeClassCarsIdxs = i;
                }
            }
        }

        private void SetStartionOrder() {
            Cars.Sort((a, b) => a.NewData.Position.CompareTo(b.NewData.Position)); // Spline position may give wrong results if cars are sitting on the grid

            var classPositions = new CarClassArray<int>(0); // Keep track of what class position are we at the moment
            for (int i = 0; i < Cars.Count; i++) {
                var thisCar = Cars[i];
                var thisClass = thisCar.CarClass;
                var classPos = ++classPositions[thisClass];
                thisCar.SetStartingPositions(i + 1, classPos);
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

            var classPositions = new CarClassArray<int>(0);  // Keep track of what class position are we at the moment
            var lastSeenInClassCarIdxs = new CarClassArray<int?>(null);  // Keep track of the indexes of last cars seen in each class

            var leaderCar = Cars[0];
            var focusedCar = Cars[(int)FocusedCarIdx];
            var focusedClass = focusedCar.CarClass;

            UpdateCarDataFirstPass(focusedCar);
            UpdateRelativeOrder();

            for (int i = 0; i < Cars.Count; i++) {
                var thisCar = Cars[i];
                var thisCarClass = thisCar.CarClass;

                var thisCarClassPos = ++classPositions[thisCarClass];
                if (thisCarClassPos == classPositions.DefaultValue + 1) {
                    _classLeaderIdxs[thisCarClass] = i;
                }

                if (PosInClassCarsIdxs != null
                    && thisCarClass == focusedClass
                    && thisCarClassPos - 1 < DynLeaderboardsPlugin.Settings.GetMaxNumClassPos()
                ) {
                    PosInClassCarsIdxs[thisCarClassPos - 1] = i;
                    if (i == FocusedCarIdx) {
                        FocusedCarPosInClassCarsIdxs = thisCarClassPos - 1;
                    }
                }

                var carAheadInClassIdx = lastSeenInClassCarIdxs[thisCarClass];
                var overallBestLapCarIdx = BestLapByClassCarIdxs[CarClass.Overall];
                var classBestLapCarIdx = BestLapByClassCarIdxs[thisCarClass];

                thisCar.OnRealtimeUpdate(
                    realtimeData: RealtimeData,
                    leaderCar: leaderCar,
                    classLeaderCar: Cars[(int)_classLeaderIdxs[thisCarClass]],
                    focusedCar: focusedCar,
                    carAhead: i != 0 ? Cars[i - 1] : null,
                    carAheadInClass: carAheadInClassIdx != null ? Cars[(int)carAheadInClassIdx] : null,
                    carAheadOnTrack: GetCarAheadOnTrack(thisCar),
                    overallBestLapCar: overallBestLapCarIdx != null ? Cars[(int)overallBestLapCarIdx] : null,
                    classBestLapCar: classBestLapCarIdx != null ? Cars[(int)classBestLapCarIdx] : null,
                    overallPos: i + 1,
                    classPos: thisCarClassPos
                    );

                lastSeenInClassCarIdxs[thisCarClass] = i;
            }

            // If somebody left the session, need to reset following class positions
            for (int i = classPositions[focusedClass]; i < DynLeaderboardsPlugin.Settings.GetMaxNumClassPos(); i++) {
                if (PosInClassCarsIdxs[i] == null) break; // All following must already be nulls
                PosInClassCarsIdxs[i] = null;
            }
        }

        private CarData GetCarAheadOnTrack(CarData c) {
            // Closest car ahead is the one with smallest positive relative spline position.
            CarData closestCar = null;
            double relsplinepos = double.MaxValue;
            foreach (var car in Cars) {
                var pos = car.CalculateRelativeSplinePosition(c);
                if (pos > 0 && pos < relsplinepos) {
                    closestCar = car;
                    relsplinepos = pos;
                }
            }
            return closestCar;
        }

        private void UpdateCarDataFirstPass(CarData focusedCar) {
            BestLapByClassCarIdxs.Reset();
            // We need to update best lap idxs first as we need to pass best lap cars on
            // and we cannot do it if BestLapByClassCarIdxs contains false indices as some cars may have left.
            for (int i = 0; i < Cars.Count; i++) {
                var thisCar = Cars[i];
                var thisCarClass = thisCar.CarClass;
                var thisCarBestLap = thisCar.NewData?.BestSessionLap?.Laptime;
                if (thisCarBestLap != null) {
                    var classBestLapCarIdx = BestLapByClassCarIdxs[thisCarClass];
                    if (classBestLapCarIdx != null) {
                        if (Cars[(int)classBestLapCarIdx].NewData.BestSessionLap.Laptime >= thisCarBestLap)
                            BestLapByClassCarIdxs[thisCarClass] = i;
                    } else {
                        BestLapByClassCarIdxs[thisCarClass] = i;
                    }

                    var overallBestLapCarIdx = BestLapByClassCarIdxs[CarClass.Overall];
                    if (overallBestLapCarIdx != null) {
                        if (Cars[(int)overallBestLapCarIdx].NewData.BestSessionLap.Laptime >= thisCarBestLap)
                            BestLapByClassCarIdxs[CarClass.Overall] = i;
                    } else {
                        BestLapByClassCarIdxs[CarClass.Overall] = i;
                    }
                }

                // Update relative spline positions
                var relSplinePos = thisCar.CalculateRelativeSplinePosition(focusedCar);
                // Since we cannot remove cars after finish, don't add cars that have left to the relative
                if (thisCar.MissedRealtimeUpdates < 10) _relativeSplinePositions.Add(new CarSplinePos(i, relSplinePos));

                thisCar.IsFocused = thisCar.CarIndex == focusedCar.CarIndex;

            }
        }

        private void UpdateRelativeOrder() {
            _relativeSplinePositions.Sort((a, b) => a.SplinePos.CompareTo(b.SplinePos));

            foreach (var l in DynLeaderboardsValues) {
                var relPos = l.Settings.NumItems.OnTrackRelativePos;
                var ahead = _relativeSplinePositions
                    .Where(x => x.SplinePos > 0)
                    .Take(relPos)
                    .Reverse()
                    .ToList()
                    .ConvertAll(x => (int?)x.CarIdx);
                var behind = _relativeSplinePositions
                    .Where(x => x.SplinePos < 0)
                    .Reverse()
                    .Take(relPos)
                    .ToList()
                    .ConvertAll(x => (int?)x.CarIdx);


                ahead.CopyTo(l.RelativePosOnTrackCarsIdxs, relPos - ahead.Count);
                l.RelativePosOnTrackCarsIdxs[relPos] = FocusedCarIdx;
                behind.CopyTo(l.RelativePosOnTrackCarsIdxs, relPos + 1);

                // Set missing positions to -1
                var startidx = relPos - ahead.Count;
                var endidx = relPos + behind.Count + 1;
                for (int i = 0; i < relPos * 2 + 1; i++) {
                    if (i < startidx || i >= endidx) {
                        l.RelativePosOnTrackCarsIdxs[i] = null;
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


        private Dictionary<int, string> outdata = new Dictionary<int, string>();
        private Dictionary<CarClass, double> bestLaps = new Dictionary<CarClass, double>();

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

            //CreateLapInterpolatorsData(update, car);
        }

        private void CreateLapInterpolatorsData(RealtimeCarUpdate update, CarData car) {
            if (outdata.ContainsKey(car.CarIndex) && car.NewData.CarLocation != CarLocationEnum.Track) {
                outdata.Remove(car.CarIndex);
            }

            if (!bestLaps.ContainsKey(car.CarClass)) {
                var fname = $"{DynLeaderboardsPlugin.Settings.PluginDataLocation}\\laps\\{TrackData.TrackId}_{car.CarClass}.txt";
                if (File.Exists(fname)) {
                    try {
                        var t = 0.0;

                        foreach (var l in File.ReadLines(fname)) {
                            if (l == "") continue;
                            // Data order: splinePositions, lap time in ms, speed in kmh
                            var splits = l.Split(';');
                            double p = float.Parse(splits[0]);
                            t = double.Parse(splits[1]);
                        }
                        bestLaps[car.CarClass] = t;
                        DynLeaderboardsPlugin.LogInfo($"Read class {car.CarClass} best lap time {t}");

                    } catch (Exception ex) {

                    }
                }
            }

            if (car.OldData != null && car.NewData.Laps != car.OldData.Laps && car.NewData.CarLocation == CarLocationEnum.Track) {
                if (!outdata.ContainsKey(car.CarIndex)) {
                    outdata[car.CarIndex] = "";
                    return;
                }

                var thisclass = car.CarClass;

                if (car.NewData?.LastLap?.Laptime != null
                    && car.NewData.LastLap.IsValidForBest
                    && (!bestLaps.ContainsKey(thisclass) || (car.NewData.LastLap.Laptime < bestLaps[thisclass]))
                ) {
                    DynLeaderboardsPlugin.LogInfo($"New best lap for {thisclass}: {TimeSpan.FromSeconds((double)car.NewData.LastLap.Laptime).ToString("mm\\:ss\\.fff")}");

                    bestLaps[thisclass] = (double)car.NewData.LastLap.Laptime;
                    var fname = $"{DynLeaderboardsPlugin.Settings.PluginDataLocation}\\laps\\{TrackData.TrackId}_{thisclass}.txt";
                    File.WriteAllText(fname, outdata[car.CarIndex]);
                }

                outdata[car.CarIndex] = "";
            }

            if (outdata.ContainsKey(car.CarIndex)) {
                if (outdata[car.CarIndex] != "") {
                    outdata[car.CarIndex] += "\n";
                }
                if (update.SplinePosition != 0 && update.SplinePosition != 1) {
                    outdata[car.CarIndex] += $"{update.SplinePosition};{update.CurrentLap.Laptime};{update.Kmh}";
                }
            }
        }

        #endregion

        #region TrackUpdate
        private void OnTrackDataUpdate(string sender, TrackData update) {

            //LeaderboardPlugin.LogInfo($"TrackData update. ThreadId={Thread.CurrentThread.ManagedThreadId}");
            // Update track data
            //AccBroadcastDataPlugin.LogInfo("New track update.");
            outdata.Clear();
            bestLaps.Clear();

            TrackData = update;
            TrackData.ReadDefBestLaps();
            //LeaderboardPlugin.LogInfo($"Finished TrackData update. ThreadId={Thread.CurrentThread.ManagedThreadId}");
        }
        #endregion

        #endregion

    }


}