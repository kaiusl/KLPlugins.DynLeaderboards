using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ACSharedMemory.ACC.Reader;

using GameReaderCommon;

using KLPlugins.DynLeaderboards.Car;
using KLPlugins.DynLeaderboards.Helpers;
using KLPlugins.DynLeaderboards.ksBroadcastingNetwork;
using KLPlugins.DynLeaderboards.Realtime;
using KLPlugins.DynLeaderboards.Settings;
using KLPlugins.DynLeaderboards.Track;

using SimHub.Plugins;

namespace KLPlugins.DynLeaderboards {

    internal class CarSplinePos {
        // Index into Cars array
        public int CarIdx = -1;
        // Corresponding splinePosition
        public double SplinePos = 0;

        public CarSplinePos(int idx, double pos) {
            this.CarIdx = idx;
            this.SplinePos = pos;
        }
    }

    /// <summary>
    /// Storage and calculation of new properties
    /// </summary>
    internal class Values : IDisposable {
        public RealtimeData? RealtimeData { get; private set; }
        public TrackData? TrackData { get; private set; }
        public double MaxDriverStintTime { get; private set; } = -1;
        public double MaxDriverTotalDriveTime { get; private set; } = -1;

        // Idea with cars is to store one copy of data
        // We keep cars array sorted in overall position order
        // Other orderings are stored in different array containing indices into Cars list
        internal List<CarData> Cars { get; private set; }

        internal ACCUdpRemoteClient? BroadcastClient { get; private set; }
        internal int?[]? PosInClassCarsIdxs { get; private set; }
        internal int?[]? PosInCupCarsIdxs { get; private set; }
        internal int? FocusedCarPosInClassCarsIdxs { get; private set; }
        internal int? FocusedCarPosInCupCarsIdxs { get; private set; }
        internal int? FocusedCarIdx { get; private set; } = null;
        internal Statistics SessionEndTimeForBroadcastEventsTime = new();
        internal List<DynLeaderboardValues> LeaderboardValues { get; private set; } = new List<DynLeaderboardValues>();

        // Store relative spline positions for relative leaderboard,
        // need to store separately as we need to sort by spline pos at the end on update loop
        private readonly CarClassArray<int?> _bestLapByClassCarIdxs = new((_) => null);

        private readonly List<CarSplinePos> _relativeSplinePositions = new();
        private readonly CarClassArray<int?> _classLeaderIdxs = new((_) => null); // Indexes of class leaders in Cars list
        private readonly CarClassArray<CupCategoryArray<int?>> _cupLeaderIdxs = new((_) => new(_ => null)); // Indexes of cup leaders in Cars list
        private readonly List<ushort> _lastUpdateCarIds = new();
        private readonly ACCUdpRemoteClientConfig _broadcastConfig;
        private bool _startingPositionsSet = false;
        private readonly Statistics _broadcastEvt_realtimeData_sessiontime_diff = new();

        internal float SessionTimeRemaining = float.NaN;
        internal ACCRawData? RawData { get; private set; }

        internal Values() {
            this.Cars = new List<CarData>();
            var num = DynLeaderboardsPlugin.Settings.GetMaxNumClassPos();
            if (num > 0) {
                this.PosInClassCarsIdxs = new int?[100];
                this.PosInCupCarsIdxs = new int?[100];
            }

            this.ResetPos();
            this._broadcastConfig = new ACCUdpRemoteClientConfig("127.0.0.1", "KLDynLeaderboardsPlugin", DynLeaderboardsPlugin.Settings.BroadcastDataUpdateRateMs);
            foreach (var l in DynLeaderboardsPlugin.Settings.DynLeaderboardConfigs) {
                if (l.IsEnabled) {
                    this.LeaderboardValues.Add(new DynLeaderboardValues(l));
                }
            }
            this.SetDynamicCarGetter();
        }

        public CarData? GetCar(int i) {
            return this.Cars.ElementAtOrDefault(i);
        }

        public CarData? GetFocusedCar() {
            if (this.FocusedCarIdx == null || this.FocusedCarIdx == -1) {
                return null;
            }

            return this.Cars[(int)this.FocusedCarIdx];
        }

        public CarData? GetBestLapCar(CarClass cls) {
            var idx = this._bestLapByClassCarIdxs[cls];
            if (idx == null) {
                return null;
            }

            return this.Cars.ElementAtOrDefault((int)idx);
        }

        public CarData? GetFocusedClassBestLapCar() {
            var focusedClass = this.GetFocusedCar()?.CarClass;
            if (focusedClass == null) {
                return null;
            }

            return this.GetBestLapCar((CarClass)focusedClass);
        }

        internal void Reset() {
            if (this.BroadcastClient != null) {
                this.DisposeBroadcastClient();
            }
            this.RealtimeData = null;
            this.TrackData = null;
            this.Cars.Clear();
            this.ResetPos();
            this._lastUpdateCarIds.Clear();
            this._classLeaderIdxs.Reset();
            this._cupLeaderIdxs.Reset();
            this._bestLapByClassCarIdxs.Reset();
            this._relativeSplinePositions.Clear();
            this._startingPositionsSet = false;
            this.MaxDriverStintTime = -1;
            this.MaxDriverTotalDriveTime = -1;
            this.SessionEndTimeForBroadcastEventsTime.Reset();
            this._broadcastEvt_realtimeData_sessiontime_diff.Reset();
            this.SessionTimeRemaining = int.MaxValue;
        }

        private void ResetPos() {
            ResetIdxs(this.PosInClassCarsIdxs);
            ResetIdxs(this.PosInCupCarsIdxs);
            foreach (var l in this.LeaderboardValues) {
                l.ResetPos();
            }

            this._relativeSplinePositions.Clear();
            this.FocusedCarIdx = null;

            static void ResetIdxs(int?[]? arr) {
                if (arr != null) {
                    for (int i = 0; i < arr.Length; i++) {
                        arr[i] = null;
                    }
                }
            }
        }

        #region IDisposable Support

        ~Values() {
            this.Dispose(false);
            GC.SuppressFinalize(this);
        }

        private bool _isDisposed = false;

        protected virtual void Dispose(bool disposing) {
            if (!this._isDisposed) {
                if (disposing) {
                    DynLeaderboardsPlugin.LogInfo("Disposed");
                    this.DisposeBroadcastClient();
                }

                this._isDisposed = true;
            }
        }

        public void Dispose() {
            this.Dispose(true);
        }

        #endregion IDisposable Support

        internal void OnDataUpdate(PluginManager _, GameData data) {
            this.RawData = (ACCRawData)data.NewData.GetRawDataObject();
            this.SessionTimeRemaining = this.RawData.Graphics.SessionTimeLeft / 1000.0f;
        }

        internal void OnGameStateChanged(bool running, PluginManager _) {
            if (running) {
                if (this.BroadcastClient != null) {
                    DynLeaderboardsPlugin.LogWarn("Broadcast client wasn't 'null' at start of new event. Shouldn't be possible, there is a bug in disposing of Broadcast client from previous session.");
                    this.DisposeBroadcastClient();
                }
                this.ConnectToBroadcastClient();
            } else {
                this.Reset();
            }
        }

        internal CarData? GetCar(int i, int?[] idxs) {
            var idx = idxs.ElementAtOrDefault(i);
            if (idx == null) {
                return null;
            }

            return this.Cars.ElementAtOrDefault((int)idx);
        }

        internal void SetDynamicCarGetter() {
            foreach (var l in this.LeaderboardValues) {
                l.SetDynGetters(this);
            }
        }

        internal void AddNewLeaderboard(DynLeaderboardConfig s) {
            this.LeaderboardValues.Add(new DynLeaderboardValues(s));
            this.SetDynamicCarGetter();
        }

        #region Broadcast client connection

        internal void ConnectToBroadcastClient() {
            this.BroadcastClient = new ACCUdpRemoteClient(this._broadcastConfig);
            this.BroadcastClient.MessageHandler.OnEntrylistUpdate += this.OnEntryListUpdate;
            this.BroadcastClient.MessageHandler.OnRealtimeCarUpdate += this.OnRealtimeCarUpdate;
            this.BroadcastClient.MessageHandler.OnRealtimeUpdate += this.OnBroadcastRealtimeUpdate;
            this.BroadcastClient.MessageHandler.OnTrackDataUpdate += this.OnTrackDataUpdate;
            this.BroadcastClient.MessageHandler.OnBroadcastingEvent += this.OnBroadcastingEvent;
        }

        internal async void DisposeBroadcastClient() {
            if (this.BroadcastClient != null) {
                await this.BroadcastClient.ShutdownAsync();
                this.BroadcastClient.Dispose();
                this.BroadcastClient = null;
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

        private void OnBroadcastingEvent(string sender, in BroadcastingEvent evt) {
            if (this.RealtimeData == null) {
                return;
            }
            //Debug.Assert(evt != null);
            //Debug.Assert(RealtimeData != null);
            if (this.RealtimeData.NewData.SessionRunningTime == TimeSpan.Zero) {
                return;
            }

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
            var timeFromLastRealtimeUpdate = (DateTime.Now - this.RealtimeData.NewData.RecieveTime).TotalSeconds;
            var currentSessionRunningTime = this.RealtimeData.NewData.SessionRunningTime.TotalSeconds + timeFromLastRealtimeUpdate;

            // Store RealtimeData.SessionRunningTime and BroadcastEvent.MsgTime difference
            var sessiontime_diff = msgTime - currentSessionRunningTime;
            if (this.RealtimeData.NewData.SessionRemainingTime != TimeSpan.Zero && (this._broadcastEvt_realtimeData_sessiontime_diff.Stats == null || this._broadcastEvt_realtimeData_sessiontime_diff.Stats.Count < 100)) {
                this._broadcastEvt_realtimeData_sessiontime_diff.Add(sessiontime_diff);
            }

            // Check if this event was late
            var isLateEvent = false;
            var msgTimeDiffFromExpected = 0.0;
            if (this._broadcastEvt_realtimeData_sessiontime_diff.Stats != null && this._broadcastEvt_realtimeData_sessiontime_diff.Stats.Count > 5) {
                msgTimeDiffFromExpected = Math.Abs(sessiontime_diff - this._broadcastEvt_realtimeData_sessiontime_diff.Median);
                if (msgTimeDiffFromExpected > 0.1) {
                    isLateEvent = true;
                } else {
                    msgTimeDiffFromExpected = 0.0;
                }
            }

            // Store session end times for BroadcastEvents
            if (this.RealtimeData.OldData != null && this.RealtimeData.NewData.SessionRemainingTime != TimeSpan.Zero) {
                var endTime = msgTime + this.RealtimeData.NewData.SessionRemainingTime.TotalSeconds - timeFromLastRealtimeUpdate;
                this.SessionEndTimeForBroadcastEventsTime.Add(endTime);

                var sesstimeremainings = float.IsNaN(this.SessionTimeRemaining) ? float.MaxValue : this.SessionTimeRemaining;
            }

            var id = evt.CarId;
            var car = this.Cars.Find(x => x.CarIndex == id);
            if (evt.Type == BroadcastingCarEventType.LapCompleted
                && this.RealtimeData.IsRace
                && car != null
                && !car.SetFinishedOnNextUpdate // If broadcast event is late, we could have already set this
                && (this.Cars[0].CarIndex == car.CarIndex || this.Cars[0].SetFinishedOnNextUpdate)
            ) {
                if (this.SessionTimeRemaining == 0
                    || (this.SessionTimeRemaining == float.NaN
                        && !double.IsNaN(this.SessionEndTimeForBroadcastEventsTime.Median)
                        && this.SessionEndTimeForBroadcastEventsTime.Median <= msgTime
                        )
                ) {
                    // Check if the session was really over
                    var wasSessionReallyFinished = true;
                    var currentSessionRunningTimeAtMsgSent = currentSessionRunningTime - msgTimeDiffFromExpected;
                    var sessionFinishedTime = currentSessionRunningTimeAtMsgSent - this.RealtimeData.SessionTotalTime.TotalSeconds;
                    if (this.Cars[0].CarIndex == car.CarIndex && isLateEvent && sessionFinishedTime < 0) {
                        wasSessionReallyFinished = false;
                    }

                    if (wasSessionReallyFinished) {
                        var sesstimeremainings = float.IsNaN(this.SessionTimeRemaining) ? float.MaxValue : this.SessionTimeRemaining;
                        car.SetIsFinished(TimeSpan.FromSeconds(currentSessionRunningTimeAtMsgSent));
                    }
                }
            }
        }

        private void OnBroadcastRealtimeUpdate(string sender, RealtimeUpdate update) {
            if (this.Cars.Count == 0) {
                return;
            }

            if (this.RealtimeData == null) {
                this.RealtimeData = new RealtimeData(update);
                return;
            } else {
                this.RealtimeData.OnRealtimeUpdate(update);
            }

            if (this.RealtimeData.IsNewSession) {
                // Clear all data at the beginning of session
                // Technically we only need clear parts of the data, but this is simpler
                DynLeaderboardsPlugin.LogInfo("New session.");
                this.Cars.Clear();
                // BroadcastClient cannot be null at this point, otherwise we wouldn't have gotten here. 
                // This is a callback method called from BroadcastClient.
                Debug.Assert(this.BroadcastClient != null);
                this.BroadcastClient!.MessageHandler.RequestEntryList();
                this.ResetPos();
                this.SessionEndTimeForBroadcastEventsTime.Reset();
                this._broadcastEvt_realtimeData_sessiontime_diff.Reset();
                this._lastUpdateCarIds.Clear();
                this._relativeSplinePositions.Clear();
                this._startingPositionsSet = false;
                this.SessionTimeRemaining = int.MaxValue;
            }

            SetMaxStintTimes();
            ClearMissingCars();

            // TODO: Do we still need this?
            // We need to check if the car is finished before setting the overall order
            // If we don't and the car just finished, it would gain a lap until the next update,
            // this causes flickering in results at the moment anyone finished
            if (this.RealtimeData.IsRace && this.RealtimeData.IsPostSession) {
                foreach (var c in this.Cars) {
                    c.CheckIsFinished();
                }
            }

            if (!this._startingPositionsSet && this.RealtimeData.IsRace && this.Cars.Count != 0 && this.Cars.All(x => x.NewData != null)) {
                SetStartingOrder();
            }
            SetOverallOrder();

            this.FocusedCarIdx = this.Cars.FindIndex(x => x.CarIndex == update.FocusedCarIndex);
            if (this.FocusedCarIdx != null && this.FocusedCarIdx != -1 && !this.RealtimeData.IsNewSession && this.TrackData != null) {
                SetRelativeOrders();
                UpdateCarData((int)this.FocusedCarIdx, this.TrackData);
            }

            #region Local functions

            void SetMaxStintTimes() {
                if (!this.RealtimeData.IsRace || !this.RealtimeData.IsPreSession || this.MaxDriverStintTime != -1) {
                    return;
                }

                this.MaxDriverStintTime = (int)DynLeaderboardsPlugin.PManager.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.Graphics.DriverStintTimeLeft") / 1000.0;
                this.MaxDriverTotalDriveTime = (int)DynLeaderboardsPlugin.PManager.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.Graphics.DriverStintTotalTimeLeft") / 1000.0;
                if (this.MaxDriverTotalDriveTime == 65535) { // This is max value, which means that the limit doesn't exist
                    this.MaxDriverTotalDriveTime = -1;
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
                if (this._lastUpdateCarIds.Count != 0) {
                    foreach (var car in this.Cars) {
                        if (!this._lastUpdateCarIds.Contains(car.CarIndex)) {
                            car.MissedRealtimeUpdates++;
                        } else {
                            car.MissedRealtimeUpdates = 0;
                        }
                    }

                    // Also don't remove cars that have finished as we want to freeze the results after finish
                    var numRemovedCars = this.Cars.RemoveAll(x => x.MissedRealtimeUpdates > 10 && !x.IsFinished);
                }
                this._lastUpdateCarIds.Clear();
            }

            void SetOverallOrder() {
                // Sort cars in overall position order
                if (this.RealtimeData.IsRace) {
                    // In race use TotalSplinePosition (splinePosition + laps) which updates real time.
                    // RealtimeCarUpdate.Position only updates at the end of sector

                    int cmp(CarData a, CarData b) {
                        if (a == b || a.NewData == null || b.NewData == null) {
                            return 0;
                        }

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

                    this.Cars.Sort(cmp);
                } else {
                    // In other sessions TotalSplinePosition doesn't make any sense, use RealtimeCarUpdate.Position
                    int cmp(CarData a, CarData b) {
                        if (a == b) {
                            return 0;
                        }

                        var apos = a.NewData?.Position ?? 1000;
                        var bpos = b.NewData?.Position ?? 1000;
                        if (apos == bpos) { // Make sort stable, fixes jumping
                            return a.OverallPos.CompareTo(b.OverallPos);
                        }
                        return apos.CompareTo(bpos);
                    }

                    this.Cars.Sort(cmp);
                }
            }

            void SetStartingOrder() {
                // This method is called after we have checked that all cars have NewData
                this.Cars.Sort((a, b) => a.NewData!.Position.CompareTo(b.NewData!.Position)); // Spline position may give wrong results if cars are sitting on the grid, thus NewData.Position

                var classPositions = new CarClassArray<int>(0); // Keep track of what class position are we at the moment
                for (int i = 0; i < this.Cars.Count; i++) {
                    var thisCar = this.Cars[i];
                    var thisClass = thisCar.CarClass;
                    var classPos = ++classPositions[thisClass];
                    thisCar.SetStartingPositions(i + 1, classPos);
                }
                this._startingPositionsSet = true;
            }

            void SetRelativeOrders() {
                foreach (var l in this.LeaderboardValues) {
                    switch (l.Settings.CurrentLeaderboard()) {
                        case Leaderboard.RelativeOverall:
                            l.SetRelativeOverallOrder((int)this.FocusedCarIdx, this.Cars);
                            break;

                        case Leaderboard.PartialRelativeOverall:
                            l.SetPartialRelativeOverallOrder((int)this.FocusedCarIdx, this.Cars);
                            break;

                        case Leaderboard.RelativeClass:
                            if (this.PosInClassCarsIdxs != null) {
                                l.SetRelativeClassOrder((int)this.FocusedCarIdx, this.Cars, this.PosInClassCarsIdxs);
                            }
                            break;

                        case Leaderboard.PartialRelativeClass:
                            if (this.PosInClassCarsIdxs != null) {
                                l.SetPartialRelativeClassOrder((int)this.FocusedCarIdx, this.Cars, this.PosInClassCarsIdxs);
                            }
                            break;

                        case Leaderboard.RelativeCup:
                            if (this.PosInCupCarsIdxs != null) {
                                l.SetRelativeCupOrder((int)this.FocusedCarIdx, this.Cars, this.PosInCupCarsIdxs);
                            }
                            break;

                        case Leaderboard.PartialRelativeCup:
                            if (this.PosInCupCarsIdxs != null) {
                                l.SetPartialRelativeCupOrder((int)this.FocusedCarIdx, this.Cars, this.PosInCupCarsIdxs);
                            }
                            break;

                        default:
                            break;
                    }
                }
            }

            /// <summary>
            /// Update car related data like positions and gaps
            /// </summary>
            void UpdateCarData(int FocusedCarIdx, TrackData trackData) {
                Debug.Assert(this.Cars.Count != 0);

                // Clear old data
                this._relativeSplinePositions.Clear();
                this._classLeaderIdxs.Reset();
                this._cupLeaderIdxs.Reset();
                this._bestLapByClassCarIdxs.Reset();

                var leaderCar = this.Cars[0];
                // FocusedCarIdx is checked to be not null before
                var focusedCar = this.Cars[FocusedCarIdx];
                var focusedClass = focusedCar.CarClass;
                var focusedCup = focusedCar.TeamCupCategory;

                if (leaderCar.NewData == null) {
                    // First RealtimeUpdate, cars do not yet have their RealtimeCarUpdates, wait until next message
                    return;
                }

                // We need to do 2 passes on Cars list, because we need to know best lap cars at the
                // moment we update CarData and only way we can do that is to go thorough all the cars
                for (int idxInCars = 0; idxInCars < this.Cars.Count; idxInCars++) {
                    var thisCar = this.Cars[idxInCars];
                    UpdateBestLapCarIdxs(thisCar, idxInCars);
                    UpdateRelativeSplinePosition(thisCar, idxInCars);
                    thisCar.OnRealtimeUpdateFirstPass(focusedCar.CarIndex);
                }

                var classPositions = new CarClassArray<int>(0);  // Keep track of what class position are we at the moment
                var cupPositions = new CarClassArray<CupCategoryArray<int>>((_) => new(0));  // Keep track of what cup position are we at the moment 
                var lastSeenInClassCarIdxs = new CarClassArray<int?>((_) => null);  // Keep track of the indexes of last cars seen in each class
                var lastSeenInCupCarIdxs = new CarClassArray<CupCategoryArray<int?>>((_) => new((_) => null));  // Keep track of the indexes of last cars seen in each cup
                for (int idxInCars = 0; idxInCars < this.Cars.Count; idxInCars++) {
                    var thisCar = this.Cars[idxInCars];
                    var thisCarClassPos = ++classPositions[thisCar.CarClass];
                    var thisCarCupPos = ++(cupPositions[thisCar.CarClass][thisCar.TeamCupCategory]);
                    SetPositionInClassAndCup(thisCar.CarClass, thisCar.TeamCupCategory, thisCarClassPos, thisCarCupPos, idxInCars);

                    var carAheadInClassIdx = lastSeenInClassCarIdxs[thisCar.CarClass];
                    var carAheadInCupIdx = lastSeenInCupCarIdxs[thisCar.CarClass][thisCar.TeamCupCategory];
                    var overallBestLapCarIdx = this._bestLapByClassCarIdxs[CarClass.Overall];
                    var classBestLapCarIdx = this._bestLapByClassCarIdxs[thisCar.CarClass];

                    thisCar.OnRealtimeUpdateSecondPass(
                        trackData: trackData,
                        realtimeData: this.RealtimeData,
                        leaderCar: leaderCar,
                        // _classLeadeIdxs must contain thisClass, and Cars must contain that car. SetPositionInClass must set it.
                        classLeaderCar: this.Cars[(int)this._classLeaderIdxs[thisCar.CarClass]!],
                        // same reason as above
                        cupLeaderCar: this.Cars[(int)this._cupLeaderIdxs[thisCar.CarClass][thisCar.TeamCupCategory]!],
                        focusedCar: focusedCar,
                        carAhead: idxInCars != 0 ? this.Cars[idxInCars - 1] : null,
                        carAheadInClass: carAheadInClassIdx != null ? this.Cars[(int)carAheadInClassIdx] : null,
                        carAheadInCup: carAheadInCupIdx != null ? this.Cars[(int)carAheadInCupIdx] : null,
                        carAheadOnTrack: this.GetCarAheadOnTrack(thisCar),
                        overallBestLapCar: overallBestLapCarIdx != null ? this.Cars[(int)overallBestLapCarIdx] : null,
                        classBestLapCar: classBestLapCarIdx != null ? this.Cars[(int)classBestLapCarIdx] : null,
                        overallPos: idxInCars + 1,
                        classPos: thisCarClassPos,
                        cupPos: thisCarCupPos
                    );
                    lastSeenInClassCarIdxs[thisCar.CarClass] = idxInCars;
                    lastSeenInCupCarIdxs[thisCar.CarClass][thisCar.TeamCupCategory] = idxInCars;
                }
                if (this.PosInClassCarsIdxs != null) {
                    ClearUnusedClassPositions(classPositions[focusedClass], this.PosInClassCarsIdxs);
                }
                if (this.PosInCupCarsIdxs != null) {
                    ClearUnusedCupPositions(cupPositions[focusedClass][focusedCup], this.PosInCupCarsIdxs);
                }

                SetRelativeOnTrackOrders();

                #region Local functions

                void UpdateBestLapCarIdxs(CarData thisCar, int idxInCars) {
                    var thisCarBestLap = thisCar.NewData?.BestSessionLap.Laptime;
                    if (thisCarBestLap != null) {
                        UpdateBestLap(thisCar.CarClass);
                        UpdateBestLap(CarClass.Overall);
                    }

                    void UpdateBestLap(CarClass cls) {
                        var currentIdx = this._bestLapByClassCarIdxs[cls];
                        if (currentIdx == null || this.Cars[(int)currentIdx].NewData?.BestSessionLap.Laptime >= thisCarBestLap) {
                            this._bestLapByClassCarIdxs[cls] = idxInCars;
                        }
                    }
                }

                void UpdateRelativeSplinePosition(CarData thisCar, int idxInCars) {
                    var relSplinePos = thisCar.CalculateRelativeSplinePosition(focusedCar);
                    // Since we cannot remove cars after finish, don't add cars that have left to the relative
                    if (thisCar.MissedRealtimeUpdates < 10 && relSplinePos != null) {
                        this._relativeSplinePositions.Add(new CarSplinePos(idxInCars, (double)relSplinePos));
                    }
                }

                void SetPositionInClassAndCup(CarClass thisCarClass, TeamCupCategory thisCarCup, int thisCarClassPos, int thisCarCupPos, int idxInCars) {
                    if (thisCarClassPos == classPositions.DefaultValue(thisCarClass) + 1) { // First time we see this class, must be the leader
                        this._classLeaderIdxs[thisCarClass] = idxInCars;

                        if (thisCarCupPos == cupPositions[thisCarClass].DefaultValue(thisCarCup)) {
                            // First time we see this cup, must be the leader
                            this._cupLeaderIdxs[thisCarClass][thisCarCup] = idxInCars;
                        }
                    }

                    if (this.PosInClassCarsIdxs != null && thisCarClass == focusedCar.CarClass) {
                        this.PosInClassCarsIdxs[thisCarClassPos - 1] = idxInCars;

                        if (this.PosInCupCarsIdxs != null && thisCarCup == focusedCar.TeamCupCategory) {
                            this.PosInCupCarsIdxs[thisCarCupPos - 1] = idxInCars;
                        }
                        if (idxInCars == FocusedCarIdx) {
                            this.FocusedCarPosInClassCarsIdxs = thisCarClassPos - 1;
                            this.FocusedCarPosInCupCarsIdxs = thisCarCupPos - 1;
                        }
                    }
                }

                void ClearUnusedClassPositions(int numCarsInFocusedCarClass, int?[] PosInClassCarsIdxs) {
                    // If somebody left the session, need to reset following class positions
                    for (int i = numCarsInFocusedCarClass; i < DynLeaderboardsPlugin.Settings.GetMaxNumClassPos(); i++) {
                        if (PosInClassCarsIdxs[i] == null) {
                            break; // All following must already be nulls
                        }

                        PosInClassCarsIdxs[i] = null;
                    }
                }

                void ClearUnusedCupPositions(int numCarsInFocusedCarCup, int?[] PosInCupCarsIdxs) {
                    // If somebody left the session, need to reset following class positions
                    for (int i = numCarsInFocusedCarCup; i < DynLeaderboardsPlugin.Settings.GetMaxNumCupPos(); i++) {
                        if (PosInCupCarsIdxs[i] == null) {
                            break; // All following must already be nulls
                        }

                        PosInCupCarsIdxs[i] = null;
                    }
                }

                void SetRelativeOnTrackOrders() {
                    if (this._relativeSplinePositions == null || this._relativeSplinePositions.Count == 0) {
                        return;
                    }

                    this._relativeSplinePositions.Sort((a, b) => a.SplinePos.CompareTo(b.SplinePos));

                    foreach (var l in this.LeaderboardValues) {
                        if (l.Settings.CurrentLeaderboard() == Leaderboard.RelativeOnTrack) {
                            l.SetRelativeOnTrackOrder(this._relativeSplinePositions, FocusedCarIdx);
                        }

                        if (l.Settings.CurrentLeaderboard() == Leaderboard.RelativeOnTrackWoPit) {
                            l.SetRelativeOnTrackWoPitOrder(this._relativeSplinePositions, FocusedCarIdx, this.Cars, this.RealtimeData.IsRace);
                        }
                    }
                }

                #endregion Local functions
            }

            #endregion Local functions
        }

        private CarData? GetCarAheadOnTrack(CarData c) {
            // Closest car ahead is the one with smallest positive relative spline position.
            CarData? closestCar = null;
            double relsplinepos = double.MaxValue;
            foreach (var car in this.Cars) {
                var pos = car.CalculateRelativeSplinePosition(c);
                if (pos != null && pos > 0 && pos < relsplinepos) {
                    closestCar = car;
                    relsplinepos = (double)pos;
                }
            }
            return closestCar;
        }

        private void OnEntryListUpdate(string sender, in CarInfo carInfo) {
            // Add new cars if not already added, update car info of all the cars (adds new drivers if some were missing)
            var id = carInfo.Id;
            var car = this.Cars.Find(x => x.CarIndex == id);
            if (car == null) {
                this.Cars.Add(new CarData(in carInfo, null));
            } else {
                car.OnEntryListUpdate(in carInfo);
            }
        }

        private void OnRealtimeCarUpdate(string sender, RealtimeCarUpdate update) {
            // Update Realtime data of existing cars
            // If found new car, BroadcastClient itself requests new entry list
            if (this.RealtimeData == null) {
                return;
            }

            var car = this.Cars.Find(x => x.CarIndex == update.CarId);
            if (car == null) {
                return;
            }

            car.OnRealtimeCarUpdate(update, this.RealtimeData);
            this._lastUpdateCarIds.Add(car.CarIndex);
        }

        private void OnTrackDataUpdate(string sender, TrackData update) {
            if (this.TrackData == null || this.TrackData.Id != update.Id) {
                this.TrackData = update;
                this.TrackData.ReadDefBestLaps();
            }
        }

        #endregion Broadcast client connection
    }
}