using ACSharedMemory.ACC.Reader;
using GameReaderCommon;
using KLPlugins.DynLeaderboards.Car;
using KLPlugins.DynLeaderboards.Helpers;
using KLPlugins.DynLeaderboards.ksBroadcastingNetwork;
using KLPlugins.DynLeaderboards.ksBroadcastingNetwork.Structs;
using KLPlugins.DynLeaderboards.Realtime;
using KLPlugins.DynLeaderboards.Settings;
using KLPlugins.DynLeaderboards.Track;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace KLPlugins.DynLeaderboards {

    internal class CarSplinePos {

        // Index into Cars array
        public int CarIdx = -1;

        // Corresponding splinePosition
        public double SplinePos = 0;

        public CarSplinePos(int idx, double pos) {
            CarIdx = idx;
            SplinePos = pos;
        }
    }

    /// <summary>
    /// Storage and calculation of new properties
    /// </summary>
    internal class Values : IDisposable {
        public RealtimeData RealtimeData { get; private set; }
        public static TrackData TrackData { get; private set; }
        public double MaxDriverStintTime { get; private set; } = -1;
        public double MaxDriverTotalDriveTime { get; private set; } = -1;

        // Idea with cars is to store one copy of data
        // We keep cars array sorted in overall position order
        // Other orderings are stored in different array containing indices into Cars list
        internal List<CarData> Cars { get; private set; }

        internal ACCUdpRemoteClient BroadcastClient { get; private set; }
        internal int?[] PosInClassCarsIdxs { get; private set; }
        internal int? FocusedCarPosInClassCarsIdxs { get; private set; }
        internal int? FocusedCarIdx { get; private set; } = null;
        internal Statistics SessionEndTimeForBroadcastEventsTime = new Statistics();
        internal List<DynLeaderboardValues> LeaderboardValues { get; private set; } = new List<DynLeaderboardValues>();

        // Store relative spline positions for relative leaderboard,
        // need to store separately as we need to sort by spline pos at the end on update loop
        private CarClassArray<int?> _bestLapByClassCarIdxs = new CarClassArray<int?>(null);

        private List<CarSplinePos> _relativeSplinePositions = new List<CarSplinePos>();
        private CarClassArray<int?> _classLeaderIdxs = new CarClassArray<int?>(null); // Indexes of class leaders in Cars list
        private List<ushort> _lastUpdateCarIds = new List<ushort>();
        private ACCUdpRemoteClientConfig _broadcastConfig;
        private bool _startingPositionsSet = false;
        private Statistics _broadcastEvt_realtimeData_sessiontime_diff = new Statistics();

        internal float SessionTimeRemaining = float.NaN;
        internal ACCRawData RawData { get; private set; }

        internal Values() {
            Cars = new List<CarData>();
            var num = DynLeaderboardsPlugin.Settings.GetMaxNumClassPos();
            if (num > 0)
                PosInClassCarsIdxs = new int?[100];

            ResetPos();
            _broadcastConfig = new ACCUdpRemoteClientConfig("127.0.0.1", "KLDynLeaderboardsPlugin", DynLeaderboardsPlugin.Settings.BroadcastDataUpdateRateMs);
            foreach (var l in DynLeaderboardsPlugin.Settings.DynLeaderboardConfigs) {
                if (l.IsEnabled) {
                    LeaderboardValues.Add(new DynLeaderboardValues(l));
                }
            }
            SetDynamicCarGetter();
        }

        public CarData GetCar(int i) => Cars.ElementAtOrDefault(i);

        public CarData GetFocusedCar() {
            if (FocusedCarIdx == null || FocusedCarIdx == -1)
                return null;
            return Cars[(int)FocusedCarIdx];
        }

        public CarData GetBestLapCar(CarClass cls) {
            var idx = _bestLapByClassCarIdxs[cls];
            if (idx == null)
                return null;
            return Cars.ElementAtOrDefault((int)idx);
        }

        public CarData GetFocusedClassBestLapCar() {
            var focusedClass = GetFocusedCar()?.CarClass;
            if (focusedClass == null)
                return null;
            return GetBestLapCar((CarClass)focusedClass);
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
            _bestLapByClassCarIdxs.Reset();
            _relativeSplinePositions.Clear();
            _startingPositionsSet = false;
            MaxDriverStintTime = -1;
            MaxDriverTotalDriveTime = -1;
            SessionEndTimeForBroadcastEventsTime.Reset();
            _broadcastEvt_realtimeData_sessiontime_diff.Reset();
            SessionTimeRemaining = int.MaxValue;
        }

        private void ResetPos() {
            ResetIdxs(PosInClassCarsIdxs);
            foreach (var l in LeaderboardValues) {
                l.ResetPos();
            }

            _relativeSplinePositions.Clear();
            FocusedCarIdx = null;

            void ResetIdxs(int?[] arr) {
                if (arr != null) {
                    for (int i = 0; i < arr.Length; i++) {
                        arr[i] = null;
                    }
                }
            }
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

        #endregion IDisposable Support

        internal void OnDataUpdate(PluginManager pm, GameData data) {
            RawData = (ACCRawData)data.NewData.GetRawDataObject();
            SessionTimeRemaining = RawData.Graphics.SessionTimeLeft / 1000.0f;
        }

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

        internal CarData GetCar(int i, int?[] idxs) {
            var idx = idxs.ElementAtOrDefault(i);
            if (idx == null)
                return null;
            return Cars.ElementAtOrDefault((int)idx);
        }

        internal void SetDynamicCarGetter() {
            foreach (var l in LeaderboardValues) {
                l.SetDynGetters(this);
            }
        }

        internal void AddNewLeaderboard(DynLeaderboardConfig s) {
            LeaderboardValues.Add(new DynLeaderboardValues(s));
            SetDynamicCarGetter();
        }

        #region Broadcast client connection

        internal void ConnectToBroadcastClient() {
            BroadcastClient = new ACCUdpRemoteClient(_broadcastConfig);
            BroadcastClient.MessageHandler.OnEntrylistUpdate += OnEntryListUpdate;
            BroadcastClient.MessageHandler.OnRealtimeCarUpdate += OnRealtimeCarUpdate;
            BroadcastClient.MessageHandler.OnRealtimeUpdate += OnBroadcastRealtimeUpdate;
            BroadcastClient.MessageHandler.OnTrackDataUpdate += OnTrackDataUpdate;
            BroadcastClient.MessageHandler.OnBroadcastingEvent += OnBroadcastingEvent;
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

        private void OnBroadcastingEvent(string sender, BroadcastingEvent evt) {
            if (RealtimeData == null) {
                return;
            }
            //Debug.Assert(evt != null);
            //Debug.Assert(RealtimeData != null);
            if (RealtimeData.SessionRunningTime == TimeSpan.Zero)
                return;

            // Its possible for this message to be late, I have seen something like 5s. I think Acc also sends multiple ones as I also have seen double messages.
            // Anyways this would mess up finish detection and order as the finish times would be wrong
            // Thus we need to check for it.
            // Broadcast event gives us message time, which tells us how long the broadcast client has been connected (if I'm not mistaken)
            // Then we can calculate to time difference between RealtimeData.SessionRunningTime and broadcast event times.
            // Then if this difference is unusually large we know this must be a late broadcast event.
            // Using this difference we can also calculate the real RealtimeData.SessionRunningTime that would correspond to this message.
            //
            // Finally we need to take this into account when detecting finished. If we get an late event after the session time had run out,
            // we need to check that it had really run out at the moment the message was meant to be sent.

            var msgTime = evt.Time;
            var timeFromLastRealtimeUpdate = (DateTime.Now - RealtimeData.NewData.RecieveTime).TotalSeconds;
            var currentSessionRunningTime = RealtimeData.SessionRunningTime.TotalSeconds + timeFromLastRealtimeUpdate;

            // Store RealtimeData.SessionRunningTime and BroadcastEvent.MsgTime difference
            var sessiontime_diff = msgTime - currentSessionRunningTime;
            if (RealtimeData.SessionRemainingTime != TimeSpan.Zero && (_broadcastEvt_realtimeData_sessiontime_diff.Stats == null || _broadcastEvt_realtimeData_sessiontime_diff.Stats.Count < 100)) {
                _broadcastEvt_realtimeData_sessiontime_diff.Add(sessiontime_diff);
            }

            // Check if this event was late
            var isLateEvent = false;
            var msgTimeDiffFromExpected = 0.0;
            if (_broadcastEvt_realtimeData_sessiontime_diff.Stats != null && _broadcastEvt_realtimeData_sessiontime_diff.Stats.Count > 5) {
                msgTimeDiffFromExpected = Math.Abs(sessiontime_diff - _broadcastEvt_realtimeData_sessiontime_diff.Median);
                if (msgTimeDiffFromExpected > 0.1) {
                    isLateEvent = true;
                } else {
                    msgTimeDiffFromExpected = 0.0;
                }
            }

            // Store session end times for BroadcastEvents
            if (RealtimeData.OldData != null && RealtimeData.SessionRemainingTime != TimeSpan.Zero) {
                var endTime = msgTime + RealtimeData.SessionRemainingTime.TotalSeconds - timeFromLastRealtimeUpdate;
                SessionEndTimeForBroadcastEventsTime.Add(endTime);

                var sesstimeremainings = float.IsNaN(SessionTimeRemaining) ? float.MaxValue : SessionTimeRemaining;
                if (isLateEvent)
                    DynLeaderboardsPlugin.LogInfo($"#{evt.CarData?.RaceNumber} BroadcastEvent. IsLate={isLateEvent} by {msgTimeDiffFromExpected:0.000s}. SessionTimeRamaining={TimeSpan.FromSeconds(sesstimeremainings)}, SessionEndTimeForBroadcastEventsTime={TimeSpan.FromSeconds(SessionEndTimeForBroadcastEventsTime.Median)}, msgTime={TimeSpan.FromSeconds(msgTime)}, diff={TimeSpan.FromSeconds(SessionEndTimeForBroadcastEventsTime.Median - msgTime)}");
            }

            // Check if is finished
            if (evt.CarData == null)
                return;
            var car = Cars.Find(x => x.CarIndex == evt.CarData.CarIndex);
            if (evt.Type == BroadcastingCarEventType.LapCompleted
                && RealtimeData.IsRace
                && car != null
                && !car.SetFinishedOnNextUpdate // If broadcast event is late, we could have already set this
                && (Cars[0].CarIndex == car.CarIndex || Cars[0].SetFinishedOnNextUpdate)
            ) {
                if (SessionTimeRemaining == 0
                    || (SessionTimeRemaining == float.NaN
                        && !double.IsNaN(SessionEndTimeForBroadcastEventsTime.Median)
                        && SessionEndTimeForBroadcastEventsTime.Median <= msgTime
                        )
                ) {
                    // Check if the session was really over
                    var wasSessionReallyFinished = true;
                    var currentSessionRunningTimeAtMsgSent = currentSessionRunningTime - msgTimeDiffFromExpected;
                    var sessionFinishedTime = currentSessionRunningTimeAtMsgSent - RealtimeData.SessionTotalTime.TotalSeconds;
                    if (Cars[0].CarIndex == car.CarIndex && isLateEvent && sessionFinishedTime < 0) {
                        wasSessionReallyFinished = false;
                    }

                    if (wasSessionReallyFinished) {
                        var sesstimeremainings = float.IsNaN(SessionTimeRemaining) ? float.MaxValue : SessionTimeRemaining;
                        DynLeaderboardsPlugin.LogInfo($"#{car.RaceNumber} finished. IsLate={isLateEvent} by {msgTimeDiffFromExpected:0.000s}. SessionTimeRamaining={TimeSpan.FromSeconds(sesstimeremainings)}, SessionEndTimeForBroadcastEventsTime={TimeSpan.FromSeconds(SessionEndTimeForBroadcastEventsTime.Median)}, msgTime={TimeSpan.FromSeconds(msgTime)}, diff={TimeSpan.FromSeconds(SessionEndTimeForBroadcastEventsTime.Median - msgTime)}");
                        car.SetIsFinished(TimeSpan.FromSeconds(currentSessionRunningTimeAtMsgSent));
                    }
                }
            }
        }

        private void OnBroadcastRealtimeUpdate(string sender, RealtimeUpdate update) {
            if (Cars.Count == 0) return;    

            if (RealtimeData == null) {
                RealtimeData = new RealtimeData(update);
                return;
            } else {
                RealtimeData.OnRealtimeUpdate(update);
            }

            if (RealtimeData.IsNewSession) {
                // Clear all data at the beginning of session
                // Technically we only need clear parts of the data, but this is simpler
                DynLeaderboardsPlugin.LogInfo("New session.");
                Cars.Clear();
                BroadcastClient.MessageHandler.RequestEntryList();
                ResetPos();
                SessionEndTimeForBroadcastEventsTime.Reset();
                _broadcastEvt_realtimeData_sessiontime_diff.Reset();
                _lastUpdateCarIds.Clear();
                _relativeSplinePositions.Clear();
                _startingPositionsSet = false;
                SessionTimeRemaining = int.MaxValue;
            }

            SetMaxStintTimes();
            ClearMissingCars();

            // TODO: Do we still need this?
            // We need to check if the car is finished before setting the overall order
            // If we don't and the car just finished, it would gain a lap until the next update,
            // this causes flickering in results at the moment anyone finished
            if (RealtimeData.IsRace && RealtimeData.IsPostSession) {
                foreach (var c in Cars) {
                    c.CheckIsFinished();
                }
            }

            if (!_startingPositionsSet && RealtimeData.IsRace && Cars.All(x => x.NewData != null)) {
                SetStartionOrder();
            }
            SetOverallOrder();

            FocusedCarIdx = Cars.FindIndex(x => x.CarIndex == update.FocusedCarIndex);
            if (FocusedCarIdx != null && FocusedCarIdx != -1 && !RealtimeData.IsNewSession) {
                SetRelativeOrders();
                UpdateCarData();
            }

            #region Local functions

            void SetMaxStintTimes() {
                if (!RealtimeData.IsRace || !RealtimeData.IsPreSession || MaxDriverStintTime != -1)
                    return;

                MaxDriverStintTime = (int)DynLeaderboardsPlugin.PManager.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.Graphics.DriverStintTimeLeft") / 1000.0;
                MaxDriverTotalDriveTime = (int)DynLeaderboardsPlugin.PManager.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.Graphics.DriverStintTotalTimeLeft") / 1000.0;
                if (MaxDriverTotalDriveTime == 65535) { // This is max value, which means that the limit doesn't exist
                    MaxDriverTotalDriveTime = -1;
                }
            }

            void ClearMissingCars() {
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

            void SetOverallOrder() {
                // Sort cars in overall position order
                if (RealtimeData.IsRace) {
                    // In race use TotalSplinePosition (splinePosition + laps) which updates real time.
                    // RealtimeCarUpdate.Position only updates at the end of sector

                    int cmp(CarData a, CarData b) {
                        if (a == b || a.NewData == null || b.NewData == null)
                            return 0;

                        // Sort cars that have crossed the start line always in front of cars who haven't
                        if (a.HasCrossedStartLine && !b.HasCrossedStartLine) {
                            return -1;
                        } else if (b.HasCrossedStartLine && !a.HasCrossedStartLine) {
                            return 1;
                        }

                        // Always compare by laps first
                        var alaps = a.NewData.Laps;
                        var blaps = b.NewData.Laps;
                        if (alaps != blaps) {
                            return blaps.CompareTo(alaps);
                        }

                        // Keep order if one of the cars has offset lap update, could cause jumping otherwise
                        if (a.OffsetLapUpdate != 0 || b.OffsetLapUpdate != 0) {
                            return a.OverallPos.CompareTo(b.OverallPos);
                        }

                        // If car jumped to the pits we need to but it behind everyone on that same lap, but it's okay for the finished car to jump to the pits
                        if (a.JumpedToPits && !b.JumpedToPits && !a.IsFinished) {
                            return 1;
                        }
                        if (b.JumpedToPits && !a.JumpedToPits && !b.IsFinished) {
                            return -1;
                        }

                        if (a.IsFinalRealtimeCarUpdateAdded || b.IsFinalRealtimeCarUpdateAdded) {
                            // We cannot use NewData.Position to set results after finish because, if someone finished and leaves the server then the positions of the guys behind him would be wrong by one.
                            // Need to use FinishTime
                            if (!a.IsFinalRealtimeCarUpdateAdded || !b.IsFinalRealtimeCarUpdateAdded) {
                                // If one hasn't finished and their number of laps is same, that means that the car who has finished must be lap down.
                                // Thus it should be behind the one who hasn't finished.
                                var aFTime = a.FinishTime == null ? TimeSpan.MinValue.TotalSeconds : ((TimeSpan)a.FinishTime).TotalSeconds;
                                var bFTime = b.FinishTime == null ? TimeSpan.MinValue.TotalSeconds : ((TimeSpan)b.FinishTime).TotalSeconds;
                                return aFTime.CompareTo(bFTime);
                            } else {
                                // Both cars have finished
                                var aFTime = a.FinishTime == null ? TimeSpan.MaxValue.TotalSeconds : ((TimeSpan)a.FinishTime).TotalSeconds;
                                var bFTime = b.FinishTime == null ? TimeSpan.MaxValue.TotalSeconds : ((TimeSpan)b.FinishTime).TotalSeconds;
                                return aFTime.CompareTo(bFTime);
                            }
                        }

                        // Keep order, make sort stable, fixes jumping
                        if (a.TotalSplinePosition == b.TotalSplinePosition) {
                            return a.OverallPos.CompareTo(b.OverallPos);
                            ;
                        }
                        return b.TotalSplinePosition.CompareTo(a.TotalSplinePosition);
                    };

                    Cars.Sort(cmp);
                } else {
                    // In other sessions TotalSplinePosition doesn't make any sense, use RealtimeCarUpdate.Position
                    int cmp(CarData a, CarData b) {
                        if (a == b)
                            return 0;
                        var apos = a?.NewData?.Position ?? 1000;
                        var bpos = b?.NewData?.Position ?? 1000;
                        if (apos == bpos) { // Make sort stable, fixes jumping
                            return a.OverallPos.CompareTo(b.OverallPos);
                        }
                        return apos.CompareTo(bpos);
                    }

                    Cars.Sort(cmp);
                }
            }

            void SetStartionOrder() {
                Cars.Sort((a, b) => a.NewData.Position.CompareTo(b.NewData.Position)); // Spline position may give wrong results if cars are sitting on the grid, thus NewData.Position

                var classPositions = new CarClassArray<int>(0); // Keep track of what class position are we at the moment
                for (int i = 0; i < Cars.Count; i++) {
                    var thisCar = Cars[i];
                    var thisClass = thisCar.CarClass;
                    var classPos = ++classPositions[thisClass];
                    thisCar.SetStartingPositions(i + 1, classPos);
                }
                _startingPositionsSet = true;
            }

            void SetRelativeOrders() {
                foreach (var l in LeaderboardValues) {
                    switch (l.Settings.CurrentLeaderboard()) {
                        case Leaderboard.RelativeOverall:
                            l.SetRelativeOverallOrder((int)FocusedCarIdx, Cars);
                            break;

                        case Leaderboard.PartialRelativeOverall:
                            l.SetPartialRelativeOverallOrder((int)FocusedCarIdx, Cars);
                            break;

                        case Leaderboard.RelativeClass:
                            l.SetRelativeClassOrder((int)FocusedCarIdx, Cars, PosInClassCarsIdxs);
                            break;

                        case Leaderboard.PartialRelativeClass:
                            l.SetPartialRelativeClassOrder((int)FocusedCarIdx, Cars, PosInClassCarsIdxs);
                            break;

                        default:
                            break;
                    }
                }
            }

            /// <summary>
            /// Update car related data like positions and gaps
            /// </summary>
            void UpdateCarData() {
                Debug.Assert(FocusedCarIdx != null);
                Debug.Assert(Cars.Count != 0);

                // Clear old data
                _relativeSplinePositions.Clear();
                _classLeaderIdxs.Reset();
                _bestLapByClassCarIdxs.Reset();

                var leaderCar = Cars[0];
                var focusedCar = Cars[(int)FocusedCarIdx];
                var focusedClass = focusedCar.CarClass;

                if (leaderCar.NewData == null) {
                    // First RealtimeUpdate, cars do not yet have their RealtimeCarUpdates, wait until next message
                    return;
                }

                // We need to do 2 passes on Cars list, because we need to know best lap cars at the
                // moment we update CarData and only way we can do that is to go thorough all the cars
                for (int idxInCars = 0; idxInCars < Cars.Count; idxInCars++) {
                    var thisCar = Cars[idxInCars];
                    UpdateBestLapCarIdxs(thisCar, idxInCars);
                    UpdateRelativeSplinePosition(thisCar, idxInCars);
                    thisCar.OnRealtimeUpdateFirstPass(focusedCar.CarIndex);
                }

                var classPositions = new CarClassArray<int>(0);  // Keep track of what class position are we at the moment
                var lastSeenInClassCarIdxs = new CarClassArray<int?>(null);  // Keep track of the indexes of last cars seen in each class
                for (int idxInCars = 0; idxInCars < Cars.Count; idxInCars++) {
                    var thisCar = Cars[idxInCars];
                    var thisCarClassPos = ++classPositions[thisCar.CarClass];
                    SetPositionInClass(thisCar.CarClass, thisCarClassPos, idxInCars);

                    var carAheadInClassIdx = lastSeenInClassCarIdxs[thisCar.CarClass];
                    var overallBestLapCarIdx = _bestLapByClassCarIdxs[CarClass.Overall];
                    var classBestLapCarIdx = _bestLapByClassCarIdxs[thisCar.CarClass];

                    thisCar.OnRealtimeUpdateSecondPass(
                        realtimeData: RealtimeData,
                        leaderCar: leaderCar,
                        classLeaderCar: Cars[(int)_classLeaderIdxs[thisCar.CarClass]], // _classLeadeIdxs must contain thisClass, and Cars must contain that car
                        focusedCar: focusedCar,
                        carAhead: idxInCars != 0 ? Cars[idxInCars - 1] : null,
                        carAheadInClass: carAheadInClassIdx != null ? Cars[(int)carAheadInClassIdx] : null,
                        carAheadOnTrack: GetCarAheadOnTrack(thisCar),
                        overallBestLapCar: overallBestLapCarIdx != null ? Cars[(int)overallBestLapCarIdx] : null,
                        classBestLapCar: classBestLapCarIdx != null ? Cars[(int)classBestLapCarIdx] : null,
                        overallPos: idxInCars + 1,
                        classPos: thisCarClassPos,
                        sessionTimeLeft: SessionTimeRemaining
                    );
                    lastSeenInClassCarIdxs[thisCar.CarClass] = idxInCars;
                }
                ClearUnusedClassPositions(classPositions[focusedClass]);

                SetRelativeOnTrackOrders();

                #region Local functions

                void UpdateBestLapCarIdxs(CarData thisCar, int idxInCars) {
                    var thisCarBestLap = thisCar.NewData?.BestSessionLap?.Laptime;
                    if (thisCarBestLap != null) {
                        UpdateBestLap(thisCar.CarClass);
                        UpdateBestLap(CarClass.Overall);
                    }

                    void UpdateBestLap(CarClass cls) {
                        var currentIdx = _bestLapByClassCarIdxs[cls];
                        if (currentIdx == null || Cars[(int)currentIdx].NewData.BestSessionLap.Laptime >= thisCarBestLap) {
                            _bestLapByClassCarIdxs[cls] = idxInCars;
                        }
                    }
                }

                void UpdateRelativeSplinePosition(CarData thisCar, int idxInCars) {
                    var relSplinePos = thisCar.CalculateRelativeSplinePosition(focusedCar);
                    // Since we cannot remove cars after finish, don't add cars that have left to the relative
                    if (thisCar.MissedRealtimeUpdates < 10 && relSplinePos != null)
                        _relativeSplinePositions.Add(new CarSplinePos(idxInCars, (double)relSplinePos));
                }

                void SetPositionInClass(CarClass thisCarClass, int thisCarClassPos, int idxInCars) {
                    if (thisCarClassPos == classPositions.DefaultValue + 1) { // First time we see this class, must be the leader
                        _classLeaderIdxs[thisCarClass] = idxInCars;
                    }

                    if (PosInClassCarsIdxs != null && thisCarClass == focusedCar.CarClass) {
                        PosInClassCarsIdxs[thisCarClassPos - 1] = idxInCars;
                        if (idxInCars == FocusedCarIdx) {
                            FocusedCarPosInClassCarsIdxs = thisCarClassPos - 1;
                        }
                    }
                }

                void ClearUnusedClassPositions(int numCarsInFocusedCarClass) {
                    // If somebody left the session, need to reset following class positions
                    for (int i = numCarsInFocusedCarClass; i < DynLeaderboardsPlugin.Settings.GetMaxNumClassPos(); i++) {
                        if (PosInClassCarsIdxs[i] == null)
                            break; // All following must already be nulls
                        PosInClassCarsIdxs[i] = null;
                    }
                }

                void SetRelativeOnTrackOrders() {
                    if (FocusedCarIdx == null || _relativeSplinePositions == null || _relativeSplinePositions.Count == 0)
                        return;
                    _relativeSplinePositions.Sort((a, b) => a.SplinePos.CompareTo(b.SplinePos));

                    foreach (var l in LeaderboardValues) {
                        if (l.Settings.CurrentLeaderboard() == Leaderboard.RelativeOnTrack)
                            l.SetRelativeOnTrackOrder(_relativeSplinePositions, (int)FocusedCarIdx);
                        if (l.Settings.CurrentLeaderboard() == Leaderboard.RelativeOnTrackWoPit)
                            l.SetRelativeOnTrackWoPitOrder(_relativeSplinePositions, (int)FocusedCarIdx, Cars, RealtimeData.IsRace);
                    }
                }

                #endregion Local functions
            }

            #endregion Local functions
        }

        private CarData GetCarAheadOnTrack(CarData c) {
            // Closest car ahead is the one with smallest positive relative spline position.
            CarData closestCar = null;
            double relsplinepos = double.MaxValue;
            foreach (var car in Cars) {
                var pos = car.CalculateRelativeSplinePosition(c);
                if (pos != null && pos > 0 && pos < relsplinepos) {
                    closestCar = car;
                    relsplinepos = (double)pos;
                }
            }
            return closestCar;
        }

        private void OnEntryListUpdate(string sender, CarInfo carInfo) {
            // Add new cars if not already added, update car info of all the cars (adds new drivers if some were missing)
            var car = Cars.Find(x => x.CarIndex == carInfo.CarIndex);
            if (car == null) {
                Cars.Add(new CarData(carInfo, null));
            } else {
                car.OnEntryListUpdate(carInfo);
            }
        }

        private void OnRealtimeCarUpdate(string sender, RealtimeCarUpdate update) {
            // Update Realtime data of existing cars
            // If found new car, BroadcastClient itself requests new entry list
            if (RealtimeData == null)
                return;
            var car = Cars.Find(x => x.CarIndex == update.CarIndex);
            if (car == null)
                return;
            car.OnRealtimeCarUpdate(update, RealtimeData);
            _lastUpdateCarIds.Add(car.CarIndex);
        }

        private void OnTrackDataUpdate(string sender, TrackData update) {
            TrackData = update;
            TrackData.ReadDefBestLaps();
        }

        #endregion Broadcast client connection
    }
}